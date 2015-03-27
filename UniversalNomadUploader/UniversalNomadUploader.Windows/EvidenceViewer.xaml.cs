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
using Windows.Storage.FileProperties;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace UniversalNomadUploader
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class EvidenceViewer : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private CancellationTokenSource cts;
        private FunctionnalEvidence CurrentEvidence = null;
        private StorageFile m_currentEviFile = null;
        private MimeTypes m_currentEviType = MimeTypes.Unknown;
        private DataManager m_dataManager;
        public enum RecordingMode
        {
            Initializing,
            Recording,
            Paused,
            Stopped
        };
        private RecordingMode CurrentMode;
        private DispatcherTimer _timer;
        private TimeSpan _elapsedTime;
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

        public EvidenceViewer()
        {
            cts = new CancellationTokenSource();
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            //this.Loaded += EvidenceViewer_Loaded;
            this.ContentStack.MaxHeight = Window.Current.Bounds.Height - 140.0;
            this.itemGridView.MaxHeight = Window.Current.Bounds.Height - 140.0;
            this.itemGridView.MinHeight = Window.Current.Bounds.Height - 140.0;
        }

        /*void EvidenceViewer_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values["Username"] != null)
            {
                Username.Text = roamingSettings.Values["Username"].ToString();
                
            }
        }*/

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
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Duration.DataContext = _elapsedTime.ToString(@"mm\:ss");
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
            m_dataManager = (DataManager)e.Parameter;

            if (m_dataManager == null)
            {
                m_dataManager = new DataManager("", "");
            }

            Password.Focus(FocusState.Pointer);

            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void RebindItems()
        {
            var evidenceGrouped = await m_dataManager.ReadAllEvidence();
            this.defaultViewModel["EvidenceItems"] = evidenceGrouped;
            BottomAppBar.IsOpen = evidenceGrouped.Count == 0;
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            Boolean failed = false;
            String failReason = null;
            itemGridView.IsEnabled = false;
            DisableButtons(PageState.Uploading);
            SyncProgress.IsIndeterminate = true;
            SyncProgress.Visibility = Visibility.Visible;
            foreach (FunctionnalEvidence item in itemGridView.SelectedItems)
            {
                if (await m_dataManager.UploadEvidence(item) != UploadStatus.OK)
                {
                    failed = true;
                    if (itemGridView.SelectedItems.Count > 1)
                    {
                        failReason = "One or more evidence failed to be uploaded.";
                    }
                    else if (itemGridView.SelectedItems.Count == 1)
                    {
                        failReason = "Evidence failed to be uploaded.";
                    }
                }
            }
            SyncProgress.IsIndeterminate = false;
            SyncProgress.Visibility = Visibility.Collapsed;
            RebindItems();
            itemGridView.IsEnabled = true;
            DisableButtons(PageState.Default);

            if (failed == true)
            {
                await displayMessage(failReason, "Upload failed");
            }
        }

        private async Task displayMessage(string message, string title)
        {
            MessageDialog msg = new MessageDialog(message, title);
            await msg.ShowAsync();
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.itemGridView.SelectedItems.Count == 1)
            {
                FileStatusTitle.Text = "File status:";
                FileStatus.Text = "";
                FileDetails.Text = (((FunctionnalEvidence)itemGridView.SelectedItem).Name == null) ? "" : ((FunctionnalEvidence)itemGridView.SelectedItem).Name;
                FileStatus.Text = ((FunctionnalEvidence)itemGridView.SelectedItem).FileStatus;
            }
            else if (this.itemGridView.SelectedItems.Count > 1)
            {
                Rename.Style = (Style)(App.Current as App).Resources["EditButtonStyle"];
                Rename.Content = "Edit";
                Rename.IsEnabled = true;
                FileDetails.Visibility = Visibility.Visible;
                FileDetailsRename.Visibility = Visibility.Collapsed;
                FileDetailsRename.Text = "";
                FileStatusTitle.Text = "Info:";
                FileStatus.Text = this.itemGridView.SelectedItems.Count.ToString() + " selected items";
            }
            else
            {
                FileStatusTitle.Text = "File status:";
                FileStatus.Text = "";
                Rename.Style = (Style)(App.Current as App).Resources["EditButtonStyle"];
                Rename.Content = "Edit";
                Rename.IsEnabled = true;
                FileDetails.Visibility = Visibility.Visible;
                FileDetailsRename.Visibility = Visibility.Collapsed;
                FileDetailsRename.Text = "";
            }
            Upload.Visibility = (itemGridView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            Delete.Visibility = (itemGridView.SelectedItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

            if (FileInfoGrid.Height == 0 && RecorderGrid.Height == 0 && itemGridView.SelectedItems.Count > 0)
            {
                expandInfoAnimation.Begin();
            }
            else if (itemGridView.SelectedItems.Count == 0)
            {
                if (FileInfoGrid.Height == 100.0)
                    hideSingleInfoAnimation.Begin();
                else
                    reduceInfoAnimation.Begin();
            }
            else if (FileInfoGrid.Height != 100.0 && FileInfoGrid.Height > 0 && itemGridView.SelectedItems.Count > 1)
            {
                reduceSingleInfoAnimation.Begin();
            }
            else if (FileInfoGrid.Height > 0 && itemGridView.SelectedItems.Count == 1)
            {
                expandSingleInfoAnimation.Begin();
            }
            if (RecorderGrid.Height > 0)
            {
                if (itemGridView.SelectedItems.Count == 1)
                    expandInfoAnimation.Begin();
                else if (itemGridView.SelectedItems.Count > 1)
                    showSingleInfoAnimation.Begin();

                reduceRecorderAnimation.Begin();
            }
            FileInfoGrid.RowDefinitions[0].Height = (itemGridView.SelectedItems.Count <= 1) ? new GridLength(1, GridUnitType.Star) : new GridLength(0.0);

        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (Rename.Content.ToString() == "Edit" && itemGridView.SelectedItems.Count > 0)
            {
                Rename.Style = (Style)(App.Current as App).Resources["SaveButtonStyle"];
                Rename.Content = "Save";
                FileDetails.Visibility = Visibility.Collapsed;
                FileDetailsRename.Visibility = Visibility.Visible;
                FileDetailsRename.Text = (((FunctionnalEvidence)itemGridView.SelectedItem).Name == null) ? "" : ((FunctionnalEvidence)itemGridView.SelectedItem).Name;
                FileDetailsRename.Focus(FocusState.Pointer);
                FileDetailsRename.Select(FileDetailsRename.Text.Length, 0);
            }
            else if (itemGridView.SelectedItems.Count > 0)
            {
                Boolean failed = false;
                String failReason = null;
                FunctionnalEvidence evi = ((FunctionnalEvidence)itemGridView.SelectedItem);
                evi.Name = FileDetailsRename.Text;

                try
                {
                    await m_dataManager.UpdateEvidence(evi);
                }
                catch (SQLite.SQLiteException ex)
                {
                    failed = true;
                    failReason = ex.Message;
                }

                if (failed == true)
                {
                    await displayMessage("Evidence not updated:" + failReason, "SQL error");
                }

                Rename.Style = (Style)(App.Current as App).Resources["EditButtonStyle"];
                Rename.Content = "Edit";
                FileDetails.Visibility = Visibility.Visible;
                FileDetailsRename.Visibility = Visibility.Collapsed;
                FileDetailsRename.Text = "";
                SearchTerm.Text = "";
                RebindItems();
            }
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            EvidenceStatus evidenceStatus = EvidenceStatus.OK;
            if (m_currentEviFile == null && m_currentEviType == MimeTypes.Audio)
            {
                String fileName = Guid.NewGuid().ToString();
                m_currentEviFile = await m_dataManager.SaveAudioRecord(fileName);
            }

            Boolean failed = false;
            String failReason = null;

            if (m_currentEviFile != null)
            {
                try
                {
                    evidenceStatus = await m_dataManager.AddEvidence(m_currentEviFile, NewName.Text, m_currentEviType);
                }
                catch (SQLite.SQLiteException ex)
                {
                    failed = true;
                    failReason = ex.Message;
                }

                if (failed == true)
                {
                    await displayMessage("Evidence not saved:" + failReason, "SQL error");
                }
            }

            if (failed == false)
            {
                if (evidenceStatus != EvidenceStatus.OK)
                {
                    switch (evidenceStatus)
                    {
                        case EvidenceStatus.BadEvidenceName:
                            failReason = "The evidence must have a name";
                            break;
                        case EvidenceStatus.BadFileName:
                            failReason = "The evidence has failed to be saved";
                            break;
                        case EvidenceStatus.MaximumSizeFileExceeded:
                            failReason = "The file can't be saved because he is exceeding the maximum size.";
                            break;
                        default:
                            failReason = "Unknown error";
                            break;
                    }

                    MessageDialog msgDialog = new MessageDialog(failReason, "Warning");
                    await msgDialog.ShowAsync();
                }
                else if (m_currentEviType == MimeTypes.Movie)
                {
                    await CreateThumbnail();
                }
            }

            m_currentEviFile = null;
            m_currentEviType = MimeTypes.Unknown;
            CurrentEvidence = null;
            HideNewName();
            RebindItems();
            NewName.Text = "";
            DisableButtons(PageState.Default);
        }

        private async Task CreateThumbnail()
        {
            ThumbnailMode thumbnailMode;

            switch (m_currentEviType)
            {
                case MimeTypes.Movie:
                    thumbnailMode = ThumbnailMode.VideosView;
                    break;

                case MimeTypes.Audio:
                    thumbnailMode = ThumbnailMode.MusicView;
                    break;

                case MimeTypes.Picture:
                    thumbnailMode = ThumbnailMode.PicturesView;
                    break;

                default:
                    thumbnailMode = ThumbnailMode.SingleItem;
                    break;
            }

            StorageFolder thumbs = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(GlobalVariables.thumbnailFolderName, CreationCollisionOption.OpenIfExists);
            StorageFile thumb = await thumbs.CreateFileAsync(m_currentEviFile.DisplayName + ".jpg", CreationCollisionOption.ReplaceExisting);

            using (var stream = await m_currentEviFile.GetThumbnailAsync(thumbnailMode))
            {
                stream.AsStream().CopyTo(await thumb.OpenStreamForWriteAsync());
            }
        }

        private void CancelNameChange_Click(object sender, RoutedEventArgs e)
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            NewName.Text = "";
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
                    SearchButton.IsEnabled = false;
                    BottomAppBar.IsOpen = true;
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
                    SearchButton.IsEnabled = false;
                    BottomAppBar.IsOpen = false;
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
                    SearchButton.IsEnabled = false;
                    BottomAppBar.IsOpen = true;
                    break;
                case PageState.RecordingAudio:
                    Delete.IsEnabled = false;
                    Upload.IsEnabled = false;
                    Rename.IsEnabled = false;
                    Import.IsEnabled = false;
                    CaptureAudio.IsEnabled = false;
                    CapturePhoto.IsEnabled = false;
                    CaptureVideo.IsEnabled = false;
                    SearchButton.IsEnabled = false;
                    backButton.IsEnabled = false;
                    BottomAppBar.IsOpen = true;
                    break;
                case PageState.Default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    SearchButton.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    BottomAppBar.IsOpen = true;
                    backButton.IsEnabled = true;
                    break;
                default:
                    Delete.IsEnabled = true;
                    Upload.IsEnabled = true;
                    Rename.IsEnabled = true;
                    Import.IsEnabled = true;
                    BottomAppBar.IsOpen = true;
                    SearchButton.IsEnabled = true;
                    CaptureAudio.IsEnabled = true;
                    CapturePhoto.IsEnabled = true;
                    CaptureVideo.IsEnabled = true;
                    backButton.IsEnabled = true;
                    break;
            }
        }

        private void CaptureAudio_Click(object sender, RoutedEventArgs e)
        {
            if (RecorderGrid.Height == 0)
            {
                ShowAudioControls();
                BottomAppBar.IsOpen = false;
            }
            else
            {
                HideAudioControls();
            }

        }


        private async void RecordAudioButton_Click(object sender, RoutedEventArgs e)
        {
            String filename = Guid.NewGuid().ToString();
            Boolean captureFailed = false;
            String failReason = null;

            try
            {
                await m_dataManager.StartAudioRecord();
                UpdateRecordingControls(RecordingMode.Recording);
                _timer.Start();
            }
            catch (Exception ex)
            {
                failReason = ex.Message;
                captureFailed = true;
            }

            if (captureFailed == true)
            {
                await displayMessage(failReason, "Failed to start an audio record");
            }
            else
            {
                m_currentEviType = MimeTypes.Audio;
            }
        }

        private async void StpAudioButton_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.StopAudioRecord();

            UpdateRecordingControls(RecordingMode.Stopped);
            _timer.Stop();
            _elapsedTime = new TimeSpan();
        }

        private async void PauseAudioButton_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.PauseAudioRecord();
            _timer.Stop();
            UpdateRecordingControls(RecordingMode.Paused);
        }

        private void SvButton_Click(object sender, RoutedEventArgs e)
        {
            HideAudioControls();
            InitTimer();
            ShowNewName();
        }

        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            String filename = Guid.NewGuid().ToString();
            StorageFile newPhoto = await m_dataManager.TakePicture(filename);

            if (newPhoto != null)
            {
                m_currentEviFile = newPhoto;
                m_currentEviType = MimeTypes.Picture;
                ShowNewName();
            }
        }

        private async void CaptureVideo_Click(object sender, RoutedEventArgs e)
        {
            String filename = Guid.NewGuid().ToString();
            StorageFile newVideo = await m_dataManager.StartVideoRecord(filename);

            if (newVideo != null)
            {
                m_currentEviFile = newVideo;
                m_currentEviType = MimeTypes.Movie;
                ShowNewName();
            }
        }

        private async void CancelAudioButton_Click(object sender, RoutedEventArgs e)
        {
            String exceptionMessage = "";

            _timer.Stop();
            try
            {
                await m_dataManager.StopAudioRecord();
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            if (exceptionMessage.Length > 0)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    var warningMessage = new MessageDialog(String.Format("The audio capture failed to stop: {0}", exceptionMessage), "Capture Failed");
                    await warningMessage.ShowAsync();
                });
            }

            m_currentEviFile = null;
            m_currentEviType = MimeTypes.Unknown;
            ResetTimer();
            Duration.DataContext = _elapsedTime.ToString(@"mm\:ss");
            UpdateRecordingControls(RecordingMode.Initializing);
            HideAudioControls();
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
            Duration.DataContext = _elapsedTime.ToString(@"mm\:ss");
        }

        private async void TimerOnTick(object sender, object o)
        {
            _elapsedTime = _elapsedTime.Add(_timer.Interval);
            Duration.DataContext = _elapsedTime.ToString(@"mm\:ss");

            if (_elapsedTime.Minutes == GlobalVariables.maxRecordTimeMinute)
            {
                if (m_currentEviType == MimeTypes.Audio)
                {
                    StpAudioButton_Click(null, null);
                }

                await displayMessage("The record is stopped because it reached the maximum length authorized", "Maximum length reached");
            }
        }

        private void ResetTimer()
        {
            InitTimer();
        }



        private void ShowNewName()
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            NewName.Focus(FocusState.Keyboard);
            DisableButtons(PageState.Renaming);
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
            if (FileInfoGrid.Height > 0)
            {
                HideInfoGrid();
            }
            expandRecorderAnimation.Begin();
        }

        private void HideInfoGrid()
        {
            if (FileInfoGrid.Height == 100.0)
            {
                hideSingleInfoAnimation.Begin();
            }
            else
            {
                reduceInfoAnimation.Begin();
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
                foreach (FunctionnalEvidence item in itemGridView.SelectedItems)
                {
                    String failReason = null;
                    Boolean failed = false;

                    try
                    {
                        await m_dataManager.DeleteEvidence(item);
                    }
                    catch(SQLite.SQLiteException ex)
                    {
                        failed = true;
                        failReason = ex.Message;
                    }

                    if(failed == true)
                    {
                        await displayMessage("Evidence not deleted, reason: " + failReason, "SQL server Error");
                        break;
                    }

                    if (item.Type == MimeTypes.Movie)
                    {
                        Boolean exceptionCatched = false;

                        try
                        {
                            StorageFolder thumbnailFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(GlobalVariables.thumbnailFolderName);
                            StorageFile eviThumbnailFile = await thumbnailFolder.GetFileAsync(item.FileName + ".jpg");
                            await eviThumbnailFile.DeleteAsync();
                        }
                        catch (Exception)
                        {
                            exceptionCatched = true;
                        }

                        if (exceptionCatched == true)
                        {
                            await displayMessage(String.Format("Failed to delete the evidence: {0}", item.Name), "Failed to delete on file system");                           
                            RebindItems();
                        }
                    }
                    await SQLEventLogUtil.InsertEventAsync(item.Name + " Deleted on " + DateTime.Now.ToString(), LogType.Delete);
                }
                RebindItems();
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.FileTypeFilter.Clear();
            foreach (KeyValuePair<String, MimeTypes> extension in GlobalVariables.ValidExtensions())
            {
                filePicker.FileTypeFilter.Add(extension.Key);
            }

            StorageFile importedFile = await filePicker.PickSingleFileAsync();
            m_currentEviFile = await importedFile.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, importedFile.DisplayName + importedFile.FileType, NameCollisionOption.ReplaceExisting);

            if (m_currentEviFile != null)
            {
                m_currentEviType = GlobalVariables.GetMimeTypeFromExtension(m_currentEviFile.FileType);
            }

            ShowNewName();
        }

        private async void logon_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress();
            if (String.IsNullOrWhiteSpace(Username.Text))
            {
                MessageDialog msg = new MessageDialog("Please enter a Username", "Required");
                await msg.ShowAsync();
                HideProgress();
                logon.IsEnabled = true;
                return;
            }
            if (String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageDialog msg = new MessageDialog("Please enter a Password", "Required");
                await msg.ShowAsync();
                HideProgress();
                logon.IsEnabled = true;
                return;
            }

            m_dataManager = new DataManager(Username.Text, Password.Password);

            switch ((await m_dataManager.ConnectToServer()))
            {
                case connectionStatus.Success:
                    this.Frame.Navigate(typeof(EvidenceViewer), m_dataManager);
                    return;
                case connectionStatus.BadPassword:
                    MessageDialog msg = new MessageDialog("Incorrect Password", "Required");
                    await msg.ShowAsync();
                    break;
                case connectionStatus.BadUsername:
                    MessageDialog msg0 = new MessageDialog("Incorrect Password", "Required");
                    await msg0.ShowAsync();
                    break;
                case connectionStatus.SqlError:
                    MessageDialog msg1 = new MessageDialog("Failed to register into the database", "Database error");
                    await msg1.ShowAsync();
                    break;
                case connectionStatus.AuthenticationFailed:
                    MessageDialog msg2 = new MessageDialog("Incorrect user or password", "Authentication failure");
                    await msg2.ShowAsync();
                    break;
                default:
                    MessageDialog msg3 = new MessageDialog("Unattented result", "Unknown error");
                    await msg3.ShowAsync();
                    break;
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
            Rename.Style = (Style)(App.Current as App).Resources["EditButtonStyle"];
            Rename.Content = "Edit";
            FileDetails.Visibility = Visibility.Visible;
            FileDetailsRename.Visibility = Visibility.Collapsed;
            FileDetailsRename.Text = "";
        }

        private void SearchTerm_TextChanged(object sender, TextChangedEventArgs e)
        {
            selectedItems = new object[itemGridView.SelectedItems.Count];
            itemGridView.SelectedItems.CopyTo(selectedItems, 0);
            if (SearchTerm.Text.Length >= 3)
            {
                var searchRes = (itemGridView.Items.OfType<FunctionnalEvidence>()).Where(x => x.Name.ToUpper().Contains(SearchTerm.Text.ToUpper()));
                foreach (var item in itemGridView.Items)
                {
                    if (!searchRes.Contains(item))
                    {
                        if ((itemGridView.ContainerFromItem(item) as GridViewItem) != null)
                        {
                            (itemGridView.ContainerFromItem(item) as GridViewItem).Opacity = 0.10;
                        }
                    }
                    else
                    {
                        if ((itemGridView.ContainerFromItem(item) as GridViewItem) != null)
                        {
                            (itemGridView.ContainerFromItem(item) as GridViewItem).Opacity = 1.0;
                        }
                    }
                }
            }
            else
            {
                foreach (var item in itemGridView.Items)
                {
                    if ((itemGridView.ContainerFromItem(item) as GridViewItem) != null)
                    {
                        (itemGridView.ContainerFromItem(item) as GridViewItem).Opacity = 1.0;
                    }
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchStack.Width == 200)
            {
                hideSearchBox.Begin();
                SearchTerm.Text = "";
            }
            else if (SearchStack.Width == 0)
            {
                showSearchBox.Begin();
                SearchTerm.Focus(FocusState.Pointer);
            }
        }

        private void NewName_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveName.IsEnabled = !String.IsNullOrWhiteSpace(NewName.Text);
        }

        private void FileDetailsRename_TextChanged(object sender, TextChangedEventArgs e)
        {
            Rename.IsEnabled = !String.IsNullOrWhiteSpace(FileDetailsRename.Text);
        }

        private void NewName_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                NewName.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void NameGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                NewName.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }
    }
}