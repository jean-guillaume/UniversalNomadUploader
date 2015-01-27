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

using Camera;
using Windows.Media.Devices;

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

        // Photo/Video capture
        private Camera.Camera m_camera;

        private RecordingMode m_CurrentMode;
        private MediaCapture m_AudioMediaCapture;
        private IRandomAccessStream m_audioStream;
        private DispatcherTimer m_timer;
        private TimeSpan m_elapsedTime;
        private AudioEncodingQuality m_encodingQuality = AudioEncodingQuality.Auto;
        private Byte[] m_PausedBuffer;
        private PageState m_CurrentState = PageState.Default;

        private enum PageState
        {
            Uploading,
            Renaming,
            Deleting,
            RecordingAudio,
            PreviewModePhoto,
            PreviewModeVideo,
            RecordingVideo,
            SetNewName,
            Default
        }

        private void DisableButtons(PageState state)
        {
            m_CurrentState = state;
            switch (state)
            {
                case PageState.Uploading:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case PageState.Renaming:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case PageState.Deleting:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case PageState.RecordingAudio:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case PageState.Default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    EnterPreviewMode.IsEnabled = true;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    StartStopRecord.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    TakePicture.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    StartStopRecord.Icon = new SymbolIcon(Symbol.Video);
                    break;
                case PageState.PreviewModePhoto:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    TakePicture.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
                case PageState.PreviewModeVideo:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    StartStopRecord.Icon = new SymbolIcon(Symbol.Play);
                    //StartStopRecord.InvalidateArrange();

                    break;
                case PageState.RecordingVideo:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    StartStopRecord.Icon = new SymbolIcon(Symbol.Stop);
                    TakePicture.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case PageState.SetNewName:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    EnterPreviewMode.IsEnabled = false;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    StartStopRecord.Icon = new SymbolIcon(Symbol.Video);
                    break;
                default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    EnterPreviewMode.IsEnabled = true;

                    itemListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    StartStopRecord.Icon = new SymbolIcon(Symbol.Video);
                    TakePicture.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
            }
        }

        void _CameraMediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            throw new NotImplementedException();
        }

        void _CameraMediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new NotImplementedException();
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
            Window.Current.Activated += Current_Activated;
        }

        void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                if (m_CurrentState == PageState.PreviewModePhoto || m_CurrentState == PageState.PreviewModeVideo)
                {
                    LeavePreviewMode_Click(null, null);
                }
            }

            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated)
            {

            }
        }

        async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (m_camera != null)
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
            Duration.DataContext = m_elapsedTime.Minutes + ":" + m_elapsedTime.Seconds + ":" + m_elapsedTime.Milliseconds;
            await InitAudioMediaCapture();
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
//            ((ListViewBase)SemanticView.ZoomedOutView).ItemsSource = groupedItemsViewSource.View.CollectionGroups;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void RebindItems()
        {
            var coll = await EvidenceUtil.GetEvidenceAsync();
            var res = coll.GroupBy(x => x.CreatedDate.Date.ToString("dd MMM yyyy")).OrderByDescending(x => Convert.ToDateTime(x.Key));
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
            itemListView.IsEnabled = false;
            DisableButtons(PageState.Uploading);
            try
            {
                foreach (Evidence item in itemListView.SelectedItems)
                {
                    //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
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
            }
            catch (Exception)
            {
                throw;
            }
            itemListView.SelectedItems.Clear();
            itemListView.IsEnabled = true;
            DisableButtons(PageState.Default);
        }

        private async Task HandleUploadAsync(UploadOperation upload, bool start, Evidence item)
        {
            try
            {
                if (start)
                {
                    // Start the upload and attach a progress handler.
                    await upload.StartAsync().AsTask(cts.Token);
                }
                else
                {
                    // The upload was already running when the application started, re-attach the progress handler.
                    await upload.AttachAsync().AsTask(cts.Token);
                }
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
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
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
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
                //ProgressBar pbar = ((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[2] as ProgressBar;
                //SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(item), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
                //sym.Foreground = new SolidColorBrush(Colors.Red);
                //sym.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //sym.Symbol = Symbol.Cancel;
                //pbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                //pbar.IsIndeterminate = false;
                EventLogUtil.InsertEvent(item.Name + " uploaded failed, reason: " + ex.Message + Environment.NewLine + Environment.NewLine + "Stack trace: " + Environment.NewLine + ex.StackTrace, LogType.Upload);
            }
        }

        private async void displayMessage(string message, string title)
        {
            MessageDialog msg = new MessageDialog(message, title);
            await msg.ShowAsync();
        }

        private void itemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.itemListView.SelectedItems.Count > 0)
            {
                this.BottomAppBar.IsSticky = true;
                this.BottomAppBar.IsOpen = true;
            }
            else
            {
                this.BottomAppBar.IsOpen = false;
                this.BottomAppBar.IsSticky = false;
            }
            Upload.Visibility = (itemListView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            Rename.Visibility = (itemListView.SelectedItems.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
            Delete.Visibility = (itemListView.SelectedItems.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons(PageState.Renaming);
            NewName.Text = (((Evidence)itemListView.SelectedItem).Name == null) ? "" : ((Evidence)itemListView.SelectedItem).Name;
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
                //The state must be 

                if (itemListView.SelectedItem == null)
                {
                    throw new NullReferenceException("No item selected");
                }
                Evidence evi = ((Evidence)itemListView.SelectedItem); //Can throw null exception
                evi.Name = NewName.Text;
                await EvidenceUtil.UpdateEvidenceNameAsync(evi);
            }
            CurrentEvidence = null;
            // HideNewName();
            RebindItems();
            NewName.Text = "";
            DisableButtons(PageState.Default);
        }

        private void CancelNameChange_Click(object sender, RoutedEventArgs e)
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            DisableButtons(PageState.Default);
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp3(m_encodingQuality);
            try
            {
                await InitAudioMediaCapture();
                m_audioStream = new InMemoryRandomAccessStream();
                await m_AudioMediaCapture.StartRecordToStreamAsync(encodingProfile, m_audioStream);
                UpdateRecordingControls(RecordingMode.Recording);
                m_timer.Start();
            }
            catch (Exception)
            {
                displayMessage("Please allow Nomad Uploader to access your microphone from the permissions charm.", "Microphone Access");
            }
        }

        private async void StpButton_Click(object sender, RoutedEventArgs e)
        {
            await m_AudioMediaCapture.StopRecordAsync();
            UpdateRecordingControls(RecordingMode.Stopped);
            m_timer.Stop();
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            await m_AudioMediaCapture.StopRecordAsync();
            m_timer.Stop();
            using (var dataReader = new DataReader(m_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)m_audioStream.Size);
                if (m_PausedBuffer == null)
                {
                    m_PausedBuffer = new byte[(int)m_audioStream.Size];
                    dataReader.ReadBytes(m_PausedBuffer);
                }
                else
                {
                    int currlength = m_PausedBuffer.Length;
                    byte[] temp = new byte[(int)m_audioStream.Size];
                    dataReader.ReadBytes(temp);
                    Array.Resize(ref m_PausedBuffer, (int)m_audioStream.Size + m_PausedBuffer.Length);
                    Array.Copy(temp, 0, m_PausedBuffer, currlength, temp.Length);
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
            using (var dataReader = new DataReader(m_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)m_audioStream.Size);
                byte[] buffer = new byte[(int)m_audioStream.Size];
                dataReader.ReadBytes(buffer);
                if (m_PausedBuffer != null && m_CurrentMode != RecordingMode.Paused)
                {
                    int currlength = m_PausedBuffer.Length;
                    Array.Resize(ref m_PausedBuffer, buffer.Length + m_PausedBuffer.Length);
                    Array.Copy(buffer, 0, m_PausedBuffer, currlength, buffer.Length);
                    await FileIO.WriteBytesAsync(_file, m_PausedBuffer);
                }
                else if (m_CurrentMode == RecordingMode.Paused)
                {
                    await FileIO.WriteBytesAsync(_file, m_PausedBuffer);
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
            //  ShowNewName();

            DisableButtons(PageState.SetNewName);
        }

        private async Task InitAudioMediaCapture()
        {
            m_AudioMediaCapture = new MediaCapture();
            var captureInitSettings = new MediaCaptureInitializationSettings();
            captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            await m_AudioMediaCapture.InitializeAsync(captureInitSettings);
            m_AudioMediaCapture.Failed += MediaCaptureOnFailed;
            m_AudioMediaCapture.RecordLimitationExceeded += MediaCaptureOnRecordLimitationExceeded;
        }

        private void UpdateRecordingControls(RecordingMode recordingMode)
        {
            m_CurrentMode = recordingMode;
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
            m_elapsedTime = new TimeSpan();
            m_timer = new DispatcherTimer();
            m_timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            m_timer.Tick += TimerOnTick;
        }

        private void TimerOnTick(object sender, object o)
        {
            m_elapsedTime = m_elapsedTime.Add(m_timer.Interval);
            Duration.DataContext = m_elapsedTime.Minutes + ":" + m_elapsedTime.Seconds + ":" + m_elapsedTime.Milliseconds;
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
            m_timer.Stop();
            try
            {
                await m_AudioMediaCapture.StopRecordAsync();
            }
            catch (Exception)
            {

            }
            ResetTimer();
            Duration.DataContext = m_elapsedTime.Minutes + ":" + m_elapsedTime.Seconds + ":" + m_elapsedTime.Milliseconds;
            UpdateRecordingControls(RecordingMode.Initializing);
            m_PausedBuffer = null;
            HideAudioControls();
        }

        private void ResetTimer()
        {
            InitTimer();
        }

        /*  private void ShowNewName()
          {
              CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
              previewButtons.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
              NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
          }

          private void HideNewName()
          {
              CaptureContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
              previewButtons.Visibility = Windows.UI.Xaml.Visibility.Visible;
              NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
          }*/

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
            MessageDialog diag = new MessageDialog("Are you sure you want to delete: " + ((Evidence)itemListView.SelectedItem).Name, "Confirm deletion!");
            diag.Commands.Add(new UICommand("Confirm", new UICommandInvokedHandler(this.ConfirmDelete)));
            diag.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler(this.ConfirmDelete)));
            await diag.ShowAsync();

        }

        private async void ConfirmDelete(IUICommand command)
        {
            if (command.Label == "Confirm")
            {
                await EvidenceUtil.DeleteAsync((Evidence)itemListView.SelectedItem);
                await (await StorageFile.GetFileFromPathAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + ((Evidence)itemListView.SelectedItem).FileName + "." + ((Evidence)itemListView.SelectedItem).Extension)).DeleteAsync();
                await EventLogUtil.InsertEventAsync(((Evidence)itemListView.SelectedItem).Name + " Deleted on " + DateTime.Now.ToString(), LogType.Delete);
                RebindItems();

            }

        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            SymbolIcon sym = (((VisualTreeHelper.GetChild((VisualTreeHelper.GetChild(itemListView.ContainerFromItem(itemListView.SelectedItem), 0)), 0) as Grid).Children[0] as StackPanel).Children[0] as Border).Child as SymbolIcon;
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
                // ShowNewName();
                DisableButtons(PageState.SetNewName);
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
    }
}
