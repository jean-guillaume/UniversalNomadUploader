using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.SQLUtils;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
            Stopped,
        };
        private RecordingMode CurrentMode;
        private MediaCapture _mediaCapture;
        private IRandomAccessStream _audioStream;
        private DispatcherTimer _timer;
        private TimeSpan _elapsedTime;
        private AudioEncodingQuality _encodingQuality = AudioEncodingQuality.Auto;
        private Byte[] _PausedBuffer;


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
            await InitMediaCapture();
            UpdateRecordingControls(RecordingMode.Initializing);
            InitTimer();
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
            var res = coll.GroupBy(x => x.CreatedDate.Date.ToString("dd/MM/yyy"));
            this.DefaultViewModel["Groups"] = res;
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            itemGridView.IsEnabled = false;
            Upload.IsEnabled = false;
            CaptureAudio.IsEnabled = false;
            CaptureVideo.IsEnabled = false;
            CapturePhoto.IsEnabled = false;
            backButton.IsEnabled = false;
            foreach (Evidence item in itemGridView.SelectedItems)
            {
                ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(item.FileName + "." + item.Extension);
                BackgroundUploader uploader = new BackgroundUploader();
                uploader.SetRequestHeader("FileName", (item.Name == null) ? "" : item.Name);
                uploader.SetRequestHeader("ContentType", file.ContentType);
                uploader.SetRequestHeader("Extension", file.FileType.Replace(".", ""));
                uploader.SetRequestHeader("X-SessionID", GlobalVariables.LoggedInUser.SessionID.ToString());
                UploadOperation upload = uploader.CreateUpload(new Uri(((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + ServerUtil.getServerWSUrl() + "/User/MobileUploadEvidence"), file);
                pbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                pbar.IsIndeterminate = true;
                await HandleUploadAsync(upload, true, item);
            }
            itemGridView.SelectedItems.Clear();
            itemGridView.IsEnabled = true;
            Upload.IsEnabled = true;
            CaptureAudio.IsEnabled = true;
            CaptureVideo.IsEnabled = true;
            CapturePhoto.IsEnabled = true;
            backButton.IsEnabled = true;
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
                ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                ((((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon).Visibility = Windows.UI.Xaml.Visibility.Visible;
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
                item.HasUploaded = true;
                item.UploadedDate = DateTime.Now;
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
            }
            catch (TaskCanceledException)
            {
                item.HasUploaded = false;
                item.UploadError = "Upload was cancelled (Task cancellation)";
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                sym.Symbol = Symbol.Cancel;
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
            }
            catch (Exception ex)
            {
                item.HasUploaded = false;
                item.UploadError = ex.Message;
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                sym.Foreground = new SolidColorBrush(Colors.Red);
                sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                sym.Symbol = Symbol.Cancel;
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
            }
        }

        private void UploadProgress(UploadOperation obj)
        {

            //UploadGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //UploadProgressBar.IsIndeterminate = false;
            //UploadProgressBar.Value = obj.Progress.BytesSent / obj.Progress.TotalBytesToSend * 100.00;
            //UploadProgressBar.Maximum = 100;
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
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            NewName.Text = (((Evidence)itemGridView.SelectedItem).Name == null) ? "" : ((Evidence)itemGridView.SelectedItem).Name;
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentEvidence != null)
            {
                CurrentEvidence.Name = NewName.Text;
                await EvidenceUtil.UpdateEvidenceNameAsync(CurrentEvidence);
                CurrentEvidence = null;
                HideNewName();
            }
            else
            {
                Evidence evi = ((Evidence)itemGridView.SelectedItem);
                evi.Name = NewName.Text;
                await EvidenceUtil.UpdateEvidenceNameAsync(evi);
                RebindItems();
                NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            
        }

        private void CancelNameChange_Click(object sender, RoutedEventArgs e)
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp3(_encodingQuality);
            try
            {
                await InitMediaCapture();
                _audioStream = new InMemoryRandomAccessStream();
                await _mediaCapture.StartRecordToStreamAsync(encodingProfile, _audioStream);
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
            await _mediaCapture.StopRecordAsync();
            UpdateRecordingControls(RecordingMode.Stopped);
            _timer.Stop();
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            await _mediaCapture.StopRecordAsync();
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


        private async Task InitMediaCapture()
        {
            _mediaCapture = new MediaCapture();
            var captureInitSettings = new MediaCaptureInitializationSettings();
            captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            await _mediaCapture.InitializeAsync(captureInitSettings);
            _mediaCapture.Failed += MediaCaptureOnFailed;
            _mediaCapture.RecordLimitationExceeded += MediaCaptureOnRecordLimitationExceeded;
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
                await _mediaCapture.StopRecordAsync();
            }
            catch (Exception)
            {

            }
            ResetTimer();
            Duration.DataContext = _elapsedTime.Minutes + ":" + _elapsedTime.Seconds + ":" + _elapsedTime.Milliseconds;
            UpdateRecordingControls(RecordingMode.Initializing);
        }


        private void ResetTimer()
        {
            InitTimer();
        }

        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI camera = new CameraCaptureUI();
            StorageFile newPhoto = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (newPhoto != null)
            {
                Evidence evi = new Evidence();
                evi.FileName = Guid.NewGuid().ToString();
                evi.Extension = newPhoto.FileType.Replace(".", "");
                evi.CreatedDate = DateTime.Now;
                evi.ServerID = (int)GlobalVariables.SelectedServer;
                await newPhoto.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, evi.FileName + newPhoto.FileType, NameCollisionOption.ReplaceExisting);
                evi.Size = Convert.ToDouble((await newPhoto.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                CurrentEvidence = evi;
                ShowNewName();
            }
        }

        private async void CaptureVideo_Click(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI video = new CameraCaptureUI();
            StorageFile newVideo = await video.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (newVideo != null)
            {
                Evidence evi = new Evidence();
                evi.FileName = Guid.NewGuid().ToString();
                evi.Extension = newVideo.FileType.Replace(".", "");
                evi.CreatedDate = DateTime.Now;
                evi.ServerID = (int)GlobalVariables.SelectedServer;
                await newVideo.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, evi.FileName + newVideo.FileType, NameCollisionOption.ReplaceExisting);
                evi.Size = Convert.ToDouble((await newVideo.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                CurrentEvidence = evi;
                ShowNewName();
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

       

    }
}
