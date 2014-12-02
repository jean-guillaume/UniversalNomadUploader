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
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
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
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
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

        private void New_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(EvidenceCapture));
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            itemGridView.IsEnabled = false;
            Upload.IsEnabled = false;
            New.IsEnabled = false;
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
            New.IsEnabled = true;
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
            Evidence evi = ((Evidence)itemGridView.SelectedItem);
            evi.Name = NewName.Text;
            await EvidenceUtil.UpdateEvidenceNameAsync(evi);
            RebindItems();
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void CancelNameChange_Click(object sender, RoutedEventArgs e)
        {
            NameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
    }
}
