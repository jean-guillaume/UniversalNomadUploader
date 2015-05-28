using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace UniversalNomadUploader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EvidenceViewer : Page
    {
        DataManager m_dataManager = null;
        ObservableDictionary defaultViewModel = new ObservableDictionary();
        FunctionnalEvidence m_selectedEvi = null;
        StorageFile m_importedFile = null;
        bool m_cancel = false;
        CoreApplicationView m_importView = CoreApplication.GetCurrentView();
        enum PageState { Default, SingleItemSelected, MultipleItemsSelected, Processing, UpdateName }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public EvidenceViewer()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            m_dataManager = (DataManager)e.Parameter;
            if (m_dataManager == null)
            {
                m_dataManager = new DataManager("", "");
            }
            refreshEvidences();
        }

        private void UIState(PageState _pageState)
        {
            switch (_pageState)
            {
                case PageState.Default:
                    ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    ImportBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    RecordPageBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    DeleteBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RenameBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UpdateNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUploadBtn.IsEnabled = true;
                    break;

                case PageState.SingleItemSelected:
                    ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    ImportBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordPageBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    DeleteBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    RenameBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    UploadBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    UpdateNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.MultipleItemsSelected:
                    ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    ImportBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordPageBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    DeleteBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    RenameBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UploadBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    UpdateNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.Processing:
                    ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CancelUploadBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    ImportBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordPageBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    DeleteBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RenameBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UpdateNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.UpdateName:
                    ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    ImportBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordPageBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    DeleteBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UploadBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RenameBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    UpdateNameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NewName.Text = "";
                    NewName.Focus(FocusState.Pointer);
                    break;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {

            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.FileTypeFilter.Clear();
            foreach (KeyValuePair<String, MimeTypes> extension in GlobalVariables.ValidExtensions())
            {
                filePicker.FileTypeFilter.Add(extension.Key);
            }

            filePicker.PickSingleFileAndContinue();
            m_importView.Activated += viewActivated;
        }

        /// <summary>
        /// Handle the returned files from file picker
        /// This method is triggered by ContinuationManager based on ActivationKind
        /// Any change to the file imported (name, extension) should be here
        /// </summary>
        /// <param name="args">File open picker continuation activation argment. It cantains the list of files user selected with file open picker </param>
        private async void viewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            FileOpenPickerContinuationEventArgs arg = args1 as FileOpenPickerContinuationEventArgs;

            if (arg != null)
            {
                if (arg.Files.Count == 0) return;
                m_importView.Activated -= viewActivated;
                StorageFile file = arg.Files[0];
                if (file != null)
                {
                    m_importedFile = await file.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, Guid.NewGuid().ToString() + file.FileType, NameCollisionOption.ReplaceExisting);
                }

                UIState(PageState.UpdateName);
            }
        }

        private void EnterPreviewMode_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CaptureView));
        }

        private void UploadSelectedEvi_Click(object sender, RoutedEventArgs e)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                UIState(PageState.Processing);
                List<FunctionnalEvidence> eviList = itemListView.SelectedItems.Cast<FunctionnalEvidence>().ToList();

                Boolean failed = false;
                String failReason = null;

                //can be interrupted by a cancel
                foreach (FunctionnalEvidence evi in eviList)
                {  
                    try
                    {
                        await m_dataManager.UploadEvidence(evi);
                    }
                    catch (SQLite.SQLiteException ex)
                    {
                        failed = true;
                        failReason = ex.Message;
                    } 

                    if (m_cancel == true)
                    {
                        m_cancel = false;
                        m_dataManager.CancelProcessing();
                        break;
                    }
                }

                if (failed == true)
                {
                    String message = null;

                    if (itemListView.SelectedItems.Count > 1)
                    {
                        message = "One or more evidence failed to be uploaded:";
                    }
                    else if (itemListView.SelectedItems.Count == 1)
                    {
                        message = "Evidence failed to be uploaded:";
                    }

                    await DisplayMessage(message + failReason, "SQL error");
                }
                refreshEvidences();
                UIState(PageState.Default);
            });
        }

        private void CancelUpload_Click(object sender, RoutedEventArgs e)
        {
            m_cancel = true;
            CancelUploadBtn.IsEnabled = false;
        }

        private void zoomOut_Click(object sender, RoutedEventArgs e)
        {
            SemanticView.IsZoomedInViewActive = false;

            refreshEvidences();
        }
        
        private void RenameBtn_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            if (menuFlyoutItem == null) { return; }
            var evi = menuFlyoutItem.DataContext as FunctionnalEvidence;

            m_selectedEvi = evi;

            UIState(PageState.UpdateName);
        }

        private async void UpdateName_Click(object sender, RoutedEventArgs e)
        {
            if (m_importedFile != null)
            {
                ThumbnailMode thumbnailMode;
                Boolean failed = false;
                String failReason = null;

                try
                {
                    await m_dataManager.AddEvidence(m_importedFile, NewName.Text, GlobalVariables.GetMimeTypeFromExtension(m_importedFile.FileType));
                }
                catch (SQLite.SQLiteException ex)
                {
                    failed = true;
                    failReason = ex.Message;
                }

                if (failed == true)
                {
                    await DisplayMessage("Evidence not saved:" + failReason, "SQL error");
                    UIState(PageState.Default);
                    refreshEvidences();
                    return;
                }

                switch (GlobalVariables.GetMimeTypeFromExtension(m_importedFile.FileType))
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

                StorageFile accessFile = await m_importedFile.CopyAsync(Windows.Storage.KnownFolders.CameraRoll, m_importedFile.DisplayName + m_importedFile.FileType, NameCollisionOption.ReplaceExisting);
                StorageFolder VideoThumbs = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(GlobalVariables.thumbnailFolderName, CreationCollisionOption.OpenIfExists);
                StorageFile VideoThumb = await VideoThumbs.CreateFileAsync(accessFile.Name, CreationCollisionOption.ReplaceExisting);

                using (var stream = await accessFile.GetThumbnailAsync(thumbnailMode))
                {
                    stream.AsStream().CopyTo(await VideoThumb.OpenStreamForWriteAsync());
                }

                m_importedFile = null;
            }
            else
            {
                m_selectedEvi.Name = NewName.Text;

                Boolean failed = false;
                String failReason = null;

                try
                {
                    await m_dataManager.UpdateEvidence(m_selectedEvi);
                }
                catch (SQLite.SQLiteException ex)
                {
                    failed = true;
                    failReason = ex.Message;
                }

                if (failed == true)
                {
                    await DisplayMessage("Evidence not updated:" + failReason, "SQL error");
                }
            }
            UIState(PageState.Default);
            refreshEvidences();
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }

        private void Evidence_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private async void DeleteEvis_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                UIState(PageState.Processing);
                List<FunctionnalEvidence> eviList = itemListView.SelectedItems.Cast<FunctionnalEvidence>().ToList();

                foreach (FunctionnalEvidence evi in itemListView.SelectedItems)
                {
                    Boolean failed = false;
                    String failReason = null;

                    try
                    {
                        await m_dataManager.DeleteEvidence(evi);
                    }
                    catch (SQLite.SQLiteException ex)
                    {
                        failed = true;
                        failReason = ex.Message;
                    }

                    if (failed == true)
                    {
                        await DisplayMessage("Evidence not deleted:" + failReason, "SQL error");
                        break;
                    }

                    if (m_cancel == true)
                    {
                        m_cancel = false;
                        break;
                    }
                }

                refreshEvidences();
                UIState(PageState.Default);
            });
        }
                
        private async void refreshEvidences()
        {
            var evidenceGrouped = await m_dataManager.ReadAllEvidence();
            this.DefaultViewModel["EvidenceItems"] = evidenceGrouped;
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (e.Handled == true)
            {
                return;
            }

            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }

        private void itemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (itemListView.SelectedItems.Count == 0)
            {
                UIState(PageState.Default);
            }
            else if (itemListView.SelectedItems.Count == 1)
            {
                UIState(PageState.SingleItemSelected);
            }
            else
            {
                UIState(PageState.MultipleItemsSelected);
            }
        }

        private async Task DisplayMessage(string message, string title)
        {
            MessageDialog msg = new MessageDialog(message, title);
            await msg.ShowAsync();
        }
    }
}
