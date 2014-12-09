using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UniversalNomadUploader.APIUtils;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.Exceptions;
using UniversalNomadUploader.SQLUtils;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking.BackgroundTransfer;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace UniversalNomadUploader
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class EvidenceView : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private CancellationTokenSource cts;
        private Evidence CurrentEvidence = null;
        public enum RecordingMode
        {
            Initializing,
            Recording,
            Paused,
            Stopped
        };
        private RecordingMode CurrentMode;
        private MediaCapture _AudioMediaCapture;
        private IRandomAccessStream _audioStream;
        private DispatcherTimer _timer;
        private TimeSpan _elapsedTime;
        private AudioEncodingQuality _encodingQuality = AudioEncodingQuality.Auto;
        private Byte[] _PausedBuffer;

        private String CurrentVideoName;
        MediaCaptureInitializationSettings _captureInitSettings;
        List<Windows.Devices.Enumeration.DeviceInformation> _deviceList;
        MediaEncodingProfile _profile;
        MediaCapture _CameraMediaCapture;
        bool _recording = false;
        bool _previewing = false;
        private PageState _CurrentPageState = PageState.Default;

        private enum PageState
        {
            Uploading,
            Renaming,
            Deleting,
            RecordingAudio,
            RecordingVideo,
            CapturingPhoto,
            Default
        }

        private async void EnumerateCameras()
        {
            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.Enumeration.DeviceClass.VideoCapture);

            _deviceList = new List<Windows.Devices.Enumeration.DeviceInformation>();

            // Add the devices to deviceList
            if (devices.Count > 0)
            {

                for (var i = 0; i < devices.Count; i++)
                {
                    _deviceList.Add(devices[i]);
                }

                InitCaptureSettings();
                InitCameraMediaCapture();
            }
            else
            {
            }
        }

        private async void InitCaptureSettings()
        {
            _captureInitSettings = null;
            _captureInitSettings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
            _captureInitSettings.AudioDeviceId = (await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.AudioCapture)).FirstOrDefault().Id;
            _captureInitSettings.VideoDeviceId = "";
            _captureInitSettings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.AudioAndVideo;
            _captureInitSettings.PhotoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.VideoPreview;
            //_captureInitSettings. = true;
            if (_deviceList.Count > 0)
                _captureInitSettings.VideoDeviceId = _deviceList[0].Id;
        }

        // Create and initialze the MediaCapture object.
        public async void InitCameraMediaCapture()
        {
            _CameraMediaCapture = null;
            _CameraMediaCapture = new Windows.Media.Capture.MediaCapture();

            // Set the MediaCapture to a variable in App.xaml.cs to handle suspension.
            (App.Current as App).MediaCapture = _CameraMediaCapture;

            _CameraMediaCapture.Failed += _CameraMediaCapture_Failed;
            _CameraMediaCapture.RecordLimitationExceeded += _CameraMediaCapture_RecordLimitationExceeded;
            await _CameraMediaCapture.InitializeAsync(_captureInitSettings);

            CreateProfile();
        }

        void _CameraMediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            throw new NotImplementedException();
        }

        void _CameraMediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new NotImplementedException();
        }

        // Create a profile.
        private void CreateProfile()
        {
            _profile = Windows.Media.MediaProperties.MediaEncodingProfile.CreateMp4(Windows.Media.MediaProperties.VideoEncodingQuality.Auto);
        }
        // Start the video capture.
        private async void StartMediaCaptureSession()
        {
            try
            {
                CurrentVideoName = Guid.NewGuid().ToString();
                StorageFile CurrentVideoFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(CurrentVideoName + ".mp4", CreationCollisionOption.ReplaceExisting);
                await _CameraMediaCapture.StartRecordToStorageFileAsync(_profile, CurrentVideoFile);
                _recording = true;
                (App.Current as App).IsRecording = true;
            }
            catch (Exception er)
            {
                
                throw;
            }
        }

        // Stop the video capture.
        private async void StopMediaCaptureSession()
        {
            try
            {
                StorageFile CurrentVideoFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(CurrentVideoName + ".mp4");
                Evidence evi = new Evidence();
                evi.FileName = CurrentVideoName;
                evi.CreatedDate = DateTime.Now;
                evi.ServerID = (int)GlobalVariables.SelectedServer;
                evi.Type = MimeTypes.Movie;
                evi.Size = Convert.ToDouble((await CurrentVideoFile.GetBasicPropertiesAsync()).Size);
                await _CameraMediaCapture.StopRecordAsync();
                _recording = false;
                (App.Current as App).IsRecording = false;
                if (CurrentVideoFile != null)
                {
                    evi.Extension = CurrentVideoFile.FileType.Replace(".", "");
                    
                    evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                    evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                    CurrentEvidence = evi;
                    CaptureContainer.Visibility = Visibility.Collapsed;
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                    ShowNewName();
                }
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
                CurrentVideoFile = null;
                CurrentVideoName = "";
                await (App.Current as App).CleanupCaptureResources();
            }
            catch (Exception er)
            {
                displayMessage(er.Message, "error");
            }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get
            {
                return this.navigationHelper;
            }
        }

        public EvidenceView()
        {
            cts = new CancellationTokenSource();
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.Loaded += EvidenceView_Loaded;
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

        }

        async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (_CameraMediaCapture != null)
            {
                await (App.Current as App).CleanupCaptureResources();
                e.Handled = true;
            }
        }



        void EvidenceView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values["Username"] != null)
            {
                Username.Text = roamingSettings.Values["Username"].ToString();
                Password.Focus(FocusState.Pointer);
            }
            (App.Current as App).PreviewElement = Preview;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Duration.DataContext = _elapsedTime.Minutes + ":" + _elapsedTime.Seconds + ":" + _elapsedTime.Milliseconds;
            await InitAudioMediaCapture();
            UpdateRecordingControls(RecordingMode.Initializing);
            InitTimer();
            EnumerateCameras();
            RebindItems();
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void RebindItems()
        {
            var coll = await EvidenceUtil.GetEvidenceAsync();
            var res = coll.GroupBy(x => x.CreatedDate.Date.ToString("dd/MM/yyy")).OrderByDescending(x => Convert.ToDateTime(x.Key));
            this.DefaultViewModel["Groups"] = res;
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVariables.IsOffline || !GlobalVariables.HasInternetAccess() || await AuthenticationUtil.VerifySessionAsync())
            {
                expandLoginAnimation.Begin();
                GlobalVariables.IsOffline = !GlobalVariables.HasInternetAccess();
                if (GlobalVariables.IsOffline)
                {
                    NewLoginReason.Text = "Your are currently offline please sign in to upload";
                }
                else
                {
                    NewLoginReason.Text = "Your session has expired please sign in again";
                }
            }
            else
            {
                SetupUploads();
            }
        }

        private async void SetupUploads()
        {
            itemGridView.IsEnabled = false;
            DisableButtons(PageState.Uploading);
            foreach (Evidence item in itemGridView.SelectedItems)
            {
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(item.FileName + "." + item.Extension);
                BackgroundUploader uploader = new BackgroundUploader();
                uploader.SetRequestHeader("FileName", (item.Name == null) ? "" : item.Name);
                uploader.SetRequestHeader("ContentType", file.ContentType);
                uploader.SetRequestHeader("Extension", file.FileType.Replace(".", ""));
                uploader.SetRequestHeader("X-SessionID", GlobalVariables.LoggedInUser.SessionID.ToString());
                UploadOperation upload = uploader.CreateUpload(new Uri(((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + ServerUtil.getServerWSUrl() + "/User/MobileUploadEvidence"), file);
                //pbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //pbar.IsIndeterminate = true;
                await HandleUploadAsync(upload, true, item);
            }
            itemGridView.SelectedItems.Clear();
            itemGridView.IsEnabled = true;
            DisableButtons(PageState.Default);
        }

        private async Task HandleUploadAsync(UploadOperation upload, bool start, Evidence item)
        {
            try
            {
                Progress<UploadOperation> progressCallback = new Progress<UploadOperation>(UploadProgress);
                if (start)
                {
                    // Start the upload and attach a progress handler.
                    await upload.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    // The upload was already running when the application started, re-attach the progress handler.
                    await upload.AttachAsync().AsTask(cts.Token, progressCallback);
                }
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                //sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //sym.Symbol = Symbol.Accept;
                //sym.Foreground = new SolidColorBrush(Colors.Green);
                //pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //pbar.IsIndeterminate = false;
                item.HasTryUploaded = true;
                item.UploadedDate = DateTime.Now;
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                await EventLogUtil.InsertEventAsync(item.Name + " uploaded successfully", LogType.Upload);
            }
            catch (TaskCanceledException)
            {
                item.HasTryUploaded = true;
                item.UploadError = "Upload was cancelled (Task cancellation)";
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                //sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //sym.Foreground = new SolidColorBrush(Colors.Red);
                //sym.Symbol = Symbol.Cancel;
                //pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //pbar.IsIndeterminate = false;
                EventLogUtil.InsertEvent(item.Name + " uploaded cancelled", LogType.Upload);
            }
            catch (Exception ex)
            {
                item.HasTryUploaded = true;
                item.UploadError = ex.Message;
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                //sym.Foreground = new SolidColorBrush(Colors.Red);
                //sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //sym.Symbol = Symbol.Cancel;
                //pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //pbar.IsIndeterminate = false;
                EventLogUtil.InsertEvent(item.Name + " uploaded failed, reason: " + ex.Message + Environment.NewLine + Environment.NewLine + "Stack trace: " + Environment.NewLine + ex.StackTrace, LogType.Upload);
            }
        }

        private void UploadProgress(UploadOperation obj)
        {

        }

        private async void displayMessage(string message, string title)
        {
            MessageDialog msg = new MessageDialog(message, title);
            await msg.ShowAsync();
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.itemGridView.SelectedItems.Count > 0)
            {
                this.BottomAppBar.IsSticky = true;
                this.BottomAppBar.IsOpen = true;
            }
            else
            {
                this.BottomAppBar.IsOpen = false;
                this.BottomAppBar.IsSticky = false;
            }
            Upload.Visibility = (itemGridView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            Rename.Visibility = (itemGridView.SelectedItems.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
            Delete.Visibility = (itemGridView.SelectedItems.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
            Info.Visibility = (itemGridView.SelectedItems.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons(PageState.Renaming);
            NewName.Text = (((Evidence)itemGridView.SelectedItem).Name == null) ? "" : ((Evidence)itemGridView.SelectedItem).Name;
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentEvidence != null)
            {
                CurrentEvidence.Name = NewName.Text;
                await EvidenceUtil.UpdateEvidenceNameAsync(CurrentEvidence);
            }
            else
            {
                Evidence evi = ((Evidence)itemGridView.SelectedItem);
                evi.Name = NewName.Text;
                await EvidenceUtil.UpdateEvidenceNameAsync(evi);
            }
            CurrentEvidence = null;
            HideNewName();
            RebindItems();
            NewName.Text = "";
            DisableButtons(PageState.Default);
        }

        private void CancelNameChange_Click(object sender, RoutedEventArgs e)
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            DisableButtons(PageState.Default);
        }

        private void DisableButtons(PageState state)
        {

            switch (state)
            {
                case PageState.Uploading:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                case PageState.Renaming:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                case PageState.Deleting:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                case PageState.RecordingAudio:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                case PageState.Default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Info.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    break;
                case PageState.CapturingPhoto:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                case PageState.RecordingVideo:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Info.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    break;
                default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Info.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    break;
            }
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp3(_encodingQuality);
            try
            {
                await InitAudioMediaCapture();
                _audioStream = new InMemoryRandomAccessStream();
                await _AudioMediaCapture.StartRecordToStreamAsync(encodingProfile, _audioStream);
                UpdateRecordingControls(RecordingMode.Recording);
                _timer.Start();
            }
            catch (Exception)
            {
                displayMessage("Please allow Nomad Uploader to access your microphone from the permissions charm.", "Microphone Access");
            }
        }

        private async void StpButton_Click(object sender, RoutedEventArgs e)
        {
            await _AudioMediaCapture.StopRecordAsync();
            UpdateRecordingControls(RecordingMode.Stopped);
            _timer.Stop();
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            await _AudioMediaCapture.StopRecordAsync();
            _timer.Stop();
            using (var dataReader = new DataReader(_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)_audioStream.Size);
                if (_PausedBuffer == null)
                {
                    _PausedBuffer = new byte[(int)_audioStream.Size];
                    dataReader.ReadBytes(_PausedBuffer);
                }
                else
                {
                    int currlength = _PausedBuffer.Length;
                    byte[] temp = new byte[(int)_audioStream.Size];
                    dataReader.ReadBytes(temp);
                    Array.Resize(ref _PausedBuffer, (int)_audioStream.Size + _PausedBuffer.Length);
                    Array.Copy(temp, 0, _PausedBuffer, currlength, temp.Length);
                }

                UpdateRecordingControls(RecordingMode.Paused);
            }
        }

        private async void SvButton_Click(object sender, RoutedEventArgs e)
        {
            Evidence evi = new Evidence();
            evi.FileName = Guid.NewGuid().ToString();
            evi.Extension = "mp3";
            evi.CreatedDate = DateTime.Now;
            evi.ServerID = (int)GlobalVariables.SelectedServer;
            evi.Type = MimeTypes.Audio;
            StorageFile _file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(evi.FileName + ".mp3", CreationCollisionOption.OpenIfExists);
            using (var dataReader = new DataReader(_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)_audioStream.Size);
                byte[] buffer = new byte[(int)_audioStream.Size];
                dataReader.ReadBytes(buffer);
                if (_PausedBuffer != null && CurrentMode != RecordingMode.Paused)
                {
                    int currlength = _PausedBuffer.Length;
                    Array.Resize(ref _PausedBuffer, buffer.Length + _PausedBuffer.Length);
                    Array.Copy(buffer, 0, _PausedBuffer, currlength, buffer.Length);
                    await FileIO.WriteBytesAsync(_file, _PausedBuffer);
                }
                else if (CurrentMode == RecordingMode.Paused)
                {
                    await FileIO.WriteBytesAsync(_file, _PausedBuffer);
                }
                else
                {
                    await FileIO.WriteBytesAsync(_file, buffer);
                }
                UpdateRecordingControls(RecordingMode.Initializing);
            }
            evi.Size = Convert.ToDouble((await _file.GetBasicPropertiesAsync()).Size);
            evi.UserID = GlobalVariables.LoggedInUser.LocalID;
            evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
            CurrentEvidence = evi;
            HideAudioControls();
            ShowNewName();
        }


        private async Task InitAudioMediaCapture()
        {
            _AudioMediaCapture = new MediaCapture();
            var captureInitSettings = new MediaCaptureInitializationSettings();
            captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            await _AudioMediaCapture.InitializeAsync(captureInitSettings);
            _AudioMediaCapture.Failed += MediaCaptureOnFailed;
            _AudioMediaCapture.RecordLimitationExceeded += MediaCaptureOnRecordLimitationExceeded;
        }

        private void UpdateRecordingControls(RecordingMode recordingMode)
        {
            CurrentMode = recordingMode;
            switch (recordingMode)
            {
                case RecordingMode.Initializing:
                    RecordButton.IsEnabled = true;
                    StpButton.IsEnabled = false;
                    SvButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    break;
                case RecordingMode.Recording:
                    RecordButton.IsEnabled = false;
                    StpButton.IsEnabled = true;
                    SvButton.IsEnabled = false;
                    PauseButton.IsEnabled = true;
                    break;
                case RecordingMode.Stopped:
                    RecordButton.IsEnabled = true;
                    StpButton.IsEnabled = false;
                    SvButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    break;
                case RecordingMode.Paused:
                    RecordButton.IsEnabled = true;
                    StpButton.IsEnabled = false;
                    SvButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("recordingMode");
            }
        }

        private void InitTimer()
        {
            _elapsedTime = new TimeSpan();
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Tick += TimerOnTick;
        }

        private void TimerOnTick(object sender, object o)
        {
            _elapsedTime = _elapsedTime.Add(_timer.Interval);
            Duration.DataContext = _elapsedTime.Minutes + ":" + _elapsedTime.Seconds + ":" + _elapsedTime.Milliseconds;
        }

        private async void MediaCaptureOnRecordLimitationExceeded(MediaCapture sender)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await sender.StopRecordAsync();
                var warningMessage = new MessageDialog("The recording has stopped because you exceeded the maximum recording length.", "Recording Stopped");
                await warningMessage.ShowAsync();
            });
        }

        private async void MediaCaptureOnFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var warningMessage = new MessageDialog(String.Format("The audio capture failed: {0}", errorEventArgs.Message), "Capture Failed");
                await warningMessage.ShowAsync();
            });
        }

        private async void CancelAudioButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            try
            {
                await _AudioMediaCapture.StopRecordAsync();
            }
            catch (Exception)
            {

            }
            ResetTimer();
            Duration.DataContext = _elapsedTime.Minutes + ":" + _elapsedTime.Seconds + ":" + _elapsedTime.Milliseconds;
            UpdateRecordingControls(RecordingMode.Initializing);
            _PausedBuffer = null;
            HideAudioControls();
        }


        private void ResetTimer()
        {
            InitTimer();
        }

        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            EnumerateCameras();
            Preview.Source = _CameraMediaCapture;
            await _CameraMediaCapture.StartPreviewAsync();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            CaptureContainer.Visibility = Visibility.Visible;
            Appbar.IsOpen = false;
            Appbar.IsSticky = false;
            _CurrentPageState = PageState.CapturingPhoto;
            //DisableButtons(PageState.CapturingPhoto);
        }

        private async void CaptureVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnumerateCameras();
                Preview.Source = _CameraMediaCapture;
                await _CameraMediaCapture.StartPreviewAsync();
                CaptureContainer.Visibility = Visibility.Visible;
                Appbar.IsOpen = false;
                Appbar.IsSticky = false;
                _CurrentPageState = PageState.RecordingVideo;
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                //DisableButtons(PageState.RecordingVideo);
            }
            catch (Exception er)
            {
                Appbar.IsSticky = false;
            }
        }

        private void ShowNewName()
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void HideNewName()
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void HideAudioControls()
        {
            reduceRecorderAnimation.Begin();
        }

        private void ShowAudioControls()
        {
            expandRecorderAnimation.Begin();
        }


        private void CaptureAudio_Click(object sender, RoutedEventArgs e)
        {
            if (RecorderGrid.Height == 0)
            {
                ShowAudioControls();
            }
            else
            {
                HideAudioControls();
            }

        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog diag = new MessageDialog("Are you sure you want to delete: " + ((Evidence)itemGridView.SelectedItem).Name, "Confirm deletion!");
            diag.Commands.Add(new UICommand("Confirm", new UICommandInvokedHandler(this.ConfirmDelete)));
            diag.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler(this.ConfirmDelete)));
            await diag.ShowAsync();

        }

        private async void ConfirmDelete(IUICommand command)
        {
            if (command.Label == "Confirm")
            {
                await EvidenceUtil.DeleteAsync((Evidence)itemGridView.SelectedItem);
                await (await StorageFile.GetFileFromPathAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + ((Evidence)itemGridView.SelectedItem).FileName + "." + ((Evidence)itemGridView.SelectedItem).Extension)).DeleteAsync();
                await EventLogUtil.InsertEventAsync(((Evidence)itemGridView.SelectedItem).Name + " Deleted on " + DateTime.Now.ToString(), LogType.Delete);
                RebindItems();

            }

        }

        private void SymbolIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {


        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(itemGridView.SelectedItem), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
            FlyoutBase.ShowAttachedFlyout(sym);
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            foreach (KeyValuePair<String, MimeTypes> extension in GlobalVariables.ValidExtensions())
            {
                openPicker.FileTypeFilter.Add(extension.Key);
            }
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                Evidence evi = new Evidence();
                evi.FileName = Guid.NewGuid().ToString();
                evi.Extension = file.FileType.Replace(".", "");
                evi.CreatedDate = DateTime.Now;
                evi.Type = GlobalVariables.GetMimeTypeFromExtension(file.FileType);
                evi.ServerID = (int)GlobalVariables.SelectedServer;
                await file.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, evi.FileName + file.FileType, NameCollisionOption.ReplaceExisting);
                evi.Size = Convert.ToDouble((await file.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                CurrentEvidence = evi;
                ShowNewName();
            }
        }

        private async void logon_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress();
            if (String.IsNullOrWhiteSpace(Username.Text))
            {
                MessageDialog msg = new MessageDialog("Please enter a Username", "Required");
                await msg.ShowAsync();
                HideProgress();
                return;
            }
            if (String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageDialog msg = new MessageDialog("Please enter a Password", "Required");
                await msg.ShowAsync();
                HideProgress();
                return;
            }

            Boolean HasAuthed = false;
            if (GlobalVariables.HasInternetAccess())
            {
                if (Username.Text.ToUpper() != GlobalVariables.LoggedInUser.Username)
                {
                    MessageDialog msg = new MessageDialog("You can only log in as the user that you are logged in as offline", "Username");
                    await msg.ShowAsync();
                    HideProgress();
                    return;
                }
                Guid Session = await AuthenticationUtil.Authenticate(Username.Text.ToUpper(), Password.Password, GlobalVariables.SelectedServer);
                if (Session != Guid.Empty)
                {
                    GlobalVariables.IsOffline = false;
                    GlobalVariables.LoggedInUser.SessionID = Session;
                    await UniversalNomadUploader.SQLUtils.UserUtil.UpdateUser(GlobalVariables.LoggedInUser);
                    HasAuthed = true;
                    HideProgress();
                    reduceLoginAnimation.Begin();
                    SetupUploads();
                    return;
                }
            }
            else
            {
                MessageDialog msg = new MessageDialog("Unable to to connect to the server, please check your internet connection", "Connection!");
                await msg.ShowAsync();
                HideProgress();
                return;
            }
            if (!HasAuthed)
            {
                MessageDialog msg = new MessageDialog("Incorrect Username or Password", "Access denied");
                await msg.ShowAsync();
                HideProgress();
                return;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            reduceLoginAnimation.Begin();
            HideProgress();
        }


        private void ShowProgress()
        {
            SyncProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;
            SyncProgress.IsIndeterminate = true;
        }

        private void HideProgress()
        {
            SyncProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SyncProgress.IsIndeterminate = false;
        }

        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            Username.Text = Username.Text.ToUpper();
            Username.SelectionStart = Username.Text.Length;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            String NewFileName = Guid.NewGuid().ToString();
            StorageFile newFile;
            Evidence evi = new Evidence();
            evi.FileName = NewFileName;
            evi.CreatedDate = DateTime.Now;
            evi.ServerID = (int)GlobalVariables.SelectedServer;

            if (_CurrentPageState == PageState.CapturingPhoto)
            {
                newFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(NewFileName + ".jpg", CreationCollisionOption.ReplaceExisting);
                evi.Type = MimeTypes.Picture;
                await _CameraMediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), newFile);
            }
            else
            {
                StartMediaCaptureSession();
                StopRecording.Visibility = Windows.UI.Xaml.Visibility.Visible;
                StartCapture.IsEnabled = false;
                return;
            }

            if (newFile != null)
            {
                evi.Extension = newFile.FileType.Replace(".", "");
                evi.Size = Convert.ToDouble((await newFile.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                CurrentEvidence = evi;
                CaptureContainer.Visibility = Visibility.Collapsed;
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                ShowNewName();
            }
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
            await (App.Current as App).CleanupCaptureResources();
        }

        private void StopRecording_Click(object sender, RoutedEventArgs e)
        {
            StopMediaCaptureSession();
            StopRecording.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

    }
}
