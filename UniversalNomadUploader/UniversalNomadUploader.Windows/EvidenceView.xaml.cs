using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking.BackgroundTransfer;
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
        private MediaCapture _mediaCapture;
        private IRandomAccessStream _audioStream;
        private DispatcherTimer _timer;
        private TimeSpan _elapsedTime;
        private AudioEncodingQuality _encodingQuality = AudioEncodingQuality.Auto;
        private Byte[] _PausedBuffer;
        private object[] selectedItems;

        private enum PageState
        {
            Uploading,
            Renaming,
            Deleting,
            RecordingAudio,
            Default
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
            this.itemGridView.MaxHeight = Window.Current.Bounds.Height - 140.0;
        }

        void EvidenceView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values["Username"] != null)
            {
                Username.Text = roamingSettings.Values["Username"].ToString();
                Password.Focus(FocusState.Pointer);
            }
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
                ProgressBar pbar = GetUploadingItemProgressbar(item);
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
                ProgressBar pbar = GetUploadingItemProgressbar(item);
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
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
                ProgressBar pbar = GetUploadingItemProgressbar(item);
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
                EventLogUtil.InsertEvent(item.Name + " uploaded cancelled", LogType.Upload);
            }
            catch (Exception ex)
            {
                item.HasTryUploaded = true;
                item.UploadError = ex.Message;
                EvidenceUtil.UpdateEvidenceSyncStatus(item);
                ProgressBar pbar = GetUploadingItemProgressbar(item);
                pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pbar.IsIndeterminate = false;
                EventLogUtil.InsertEvent(item.Name + " uploaded failed, reason: " + ex.Message + Environment.NewLine + Environment.NewLine + "Stack trace: " + Environment.NewLine + ex.StackTrace, LogType.Upload);
            }
        }

        private ProgressBar GetUploadingItemProgressbar(Evidence item)
        {
            return VisualTreeHelper.GetChild(((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemGridView.ContainerFromItem(item), 0) as GridViewItemPresenter), 0) as Grid).Children[0] as StackPanel).Children[1] as Grid, 0) as ProgressBar;
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

            if (this.itemGridView.SelectedItems.Count == 1)
            {
                FileDetails.Text = ((Evidence)itemGridView.SelectedItem).Name;
                if (((Evidence)itemGridView.SelectedItem).HasTryUploaded)
                {
                    if (((Evidence)itemGridView.SelectedItem).UploadError != null && ((Evidence)itemGridView.SelectedItem).UploadError != "")
                    {
                        FileStatus.Text = ((Evidence)itemGridView.SelectedItem).UploadError.ToString();
                    }
                    else
                    {
                        FileStatus.Text = ((Evidence)itemGridView.SelectedItem).UploadedDate.ToString();
                    }
                }
            }
            Upload.Visibility = (itemGridView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            Delete.Visibility = (itemGridView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            if (FileInfoGrid.Height == 0 && itemGridView.SelectedItems.Count > 0)
            {
                expandInfoAnimation.Begin();
            }
            else if (itemGridView.SelectedItems.Count == 0)
            {
                if (FileInfoGrid.Height == 75.0)
                    hideSingleInfoAnimation.Begin();
                else
                    reduceInfoAnimation.Begin();
            }
            else if (FileInfoGrid.Height != 75.0 && FileInfoGrid.Height > 0 && itemGridView.SelectedItems.Count > 1)
            {
                reduceSingleInfoAnimation.Begin();
            }
            else if (FileInfoGrid.Height > 0 && itemGridView.SelectedItems.Count == 1)
            {
                expandSingleInfoAnimation.Begin();
            }
            FileInfoGrid.RowDefinitions[0].Height = (itemGridView.SelectedItems.Count <= 1) ? new GridLength(1, GridUnitType.Star) : new GridLength(0.0);
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
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    backButton.IsEnabled = false;
                    break;
                case PageState.Renaming:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    backButton.IsEnabled = false;
                    break;
                case PageState.Deleting:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    backButton.IsEnabled = false;
                    break;
                case PageState.RecordingAudio:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    backButton.IsEnabled = false;
                    break;
                case PageState.Default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    backButton.IsEnabled = true;
                    break;
                default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    backButton.IsEnabled = true;
                    break;
            }
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
            _PausedBuffer = null;
            HideAudioControls();
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
                evi.Type = MimeTypes.Picture;
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
            StorageFolder VideoThumbs = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("_VidThumbs", CreationCollisionOption.OpenIfExists);
            if (newVideo != null)
            {
                Evidence evi = new Evidence();
                evi.FileName = Guid.NewGuid().ToString();
                evi.Extension = newVideo.FileType.Replace(".", "");
                evi.CreatedDate = DateTime.Now;
                evi.ServerID = (int)GlobalVariables.SelectedServer;
                evi.Type = MimeTypes.Movie;
                await newVideo.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, evi.FileName + newVideo.FileType, NameCollisionOption.ReplaceExisting);
                evi.Size = Convert.ToDouble((await newVideo.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                StorageFile VideoThumb = await VideoThumbs.CreateFileAsync(evi.FileName + ".jpg", CreationCollisionOption.ReplaceExisting);
                (await newVideo.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView)).AsStream().CopyTo(await VideoThumb.OpenStreamForWriteAsync());
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
            if (itemGridView.SelectedItems.Count > 0)
            {
                ShowInfoGrid();
            }
        }

        private void ShowInfoGrid()
        {
            if (itemGridView.SelectedItems.Count == 1)
            {
                expandInfoAnimation.Begin();
            }
            else
            {
                showSingleInfoAnimation.Begin();
            }
        }

        private void ShowAudioControls()
        {
            expandRecorderAnimation.Begin();
            if (FileInfoGrid.Height > 0)
            {
                HideInfoGrid();
            }
        }

        private void HideInfoGrid()
        {
            if (FileInfoGrid.Height == 75.0)
            {
                hideSingleInfoAnimation.Begin();
            }
            else
            {
                reduceInfoAnimation.Begin();
            }
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
            MessageDialog diag = new MessageDialog("Are you sure you want to delete the selected file(s)?", "Confirm deletion!");
            diag.Commands.Add(new UICommand("Confirm", new UICommandInvokedHandler(this.ConfirmDelete)));
            diag.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler(this.ConfirmDelete)));
            await diag.ShowAsync();
        }

        private async void ConfirmDelete(IUICommand command)
        {
            if (command.Label == "Confirm")
            {
                foreach (var item in itemGridView.SelectedItems)
                {
                    Evidence evi = (Evidence)item;
                    await EvidenceUtil.DeleteAsync(evi);
                    await (await StorageFile.GetFileFromPathAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + evi.FileName + "." + evi.Extension)).DeleteAsync();
                    await EventLogUtil.InsertEventAsync(evi.Name + " Deleted on " + DateTime.Now.ToString(), LogType.Delete);
                }
                RebindItems();
            }
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

        private void InfoCancel_Click(object sender, RoutedEventArgs e)
        {
            itemGridView.SelectedItems.Clear();
        }

        private void SearchTerm_TextChanged(object sender, TextChangedEventArgs e)
        {
            selectedItems = new object[itemGridView.SelectedItems.Count];
            itemGridView.SelectedItems.CopyTo(selectedItems, 0);
            if (SearchTerm.Text.Length >= 3)
            {
                var searchRes = (itemGridView.Items.OfType<Evidence>()).Where(x => x.Name.Contains(SearchTerm.Text));
                foreach (var item in itemGridView.Items)
                {
                    if (!searchRes.Contains(item))
                    {
                        (itemGridView.ContainerFromItem(item) as GridViewItem).Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                foreach (var item in itemGridView.Items)
                {
                    (itemGridView.ContainerFromItem(item) as GridViewItem).Visibility = Visibility.Visible;
                }
            }
        }

    }
}
