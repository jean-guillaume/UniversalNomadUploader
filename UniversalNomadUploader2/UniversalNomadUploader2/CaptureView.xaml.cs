using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.SQLModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace UniversalNomadUploader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CaptureView : Page
    {
        DataManager m_dataManager = null;
        DBManager db;

        String m_fileNameEvi;
        String m_extensionEvi;
        int m_serverIDEvi;
        MimeTypes m_mimeTypeEvi;

        private enum PageState
        {
            Default,
            VideoRecording,
            SavingName
        }

        public CaptureView()
        {
            this.InitializeComponent();
            m_dataManager = new DataManager();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            UIState(PageState.Default);
            Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Photo);
            await Preview.Source.StartPreviewAsync();
            db = (DBManager)e.Parameter;
        }

        async void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                UIState(PageState.Default);
                await Preview.Source.StopPreviewAsync();
            }

            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated)
            {
                Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Photo);
            }
        }

       /* async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (m_camera != null)
            {
                await (App.Current as App).CleanupCaptureResources();
                e.Handled = true;
            }
        }*/

       /* private async void LeavePreviewMode_Click(object sender, RoutedEventArgs e)
        {            
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            await m_camera.stopPreview();
            freeResources();
        }*/

        private void UIState(PageState _state)
        {
            switch (_state)
            {
                case PageState.Default:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.IsEnabled = true;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.VideoRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.IsEnabled = false;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.SavingName:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    Appbar.IsEnabled = false;

                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NewName.Text = "";
                    break;
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            StorageFile storageFileResult = null;
            String fileName = Guid.NewGuid().ToString();

            storageFileResult = await m_dataManager.captureEvidence.TakePicture(fileName);

            if (storageFileResult == null)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Photo);
                await Preview.Source.StartPreviewAsync();
                storageFileResult = await m_dataManager.captureEvidence.TakePicture(fileName);
            }

            m_extensionEvi = "jpg";
            m_fileNameEvi = fileName;
            m_serverIDEvi = 0;
            m_mimeTypeEvi = MimeTypes.Picture;

            UIState(PageState.SavingName);
        }

        private async void StartRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.VideoRecording);
            StorageFile storageFileResult = null;
            String filename = Guid.NewGuid().ToString();
            storageFileResult = await m_dataManager.captureEvidence.StartVideoRecord(filename);

            if (storageFileResult == null)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Video);
                await Preview.Source.StartPreviewAsync();
                storageFileResult = await m_dataManager.captureEvidence.StartVideoRecord(Guid.NewGuid().ToString());
            }

            m_fileNameEvi = filename;
        }

        private void StopRecord_Click(object sender, RoutedEventArgs e)
        {
            m_dataManager.captureEvidence.StopVideoRecord();

            m_extensionEvi = "mp4";
            m_serverIDEvi = 0;
            m_mimeTypeEvi = MimeTypes.Movie;

            UIState(PageState.SavingName);
        }

        private void StartRecordAudio_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            await Preview.Source.StopPreviewAsync();
            this.Frame.Navigate(typeof(EvidenceViewer), db);
        }

        private void SaveName_Click(object sender, RoutedEventArgs e)
        {
            db.AddEvidence(m_fileNameEvi, m_extensionEvi, DateTime.Now, m_serverIDEvi, NewName.Text, m_mimeTypeEvi);
            UIState(PageState.Default);
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }

        private void registerEvidence()
        {

        }

    }
}
