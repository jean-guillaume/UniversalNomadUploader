using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Video);
            await Preview.Source.StartPreviewAsync();
        }

        private void UIState(PageState _state)
        {
            switch(_state)
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
                    break;
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            int captureResult = 0;

            captureResult = await m_dataManager.captureEvidence.TakePicture(Guid.NewGuid().ToString());

            if( captureResult == -1)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Photo);
                await Preview.Source.StartPreviewAsync();
                await m_dataManager.captureEvidence.TakePicture(Guid.NewGuid().ToString());
            }

            UIState(PageState.SavingName);
        }

        private async void StartRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.VideoRecording);
            int captureResult = 0;
            captureResult = await m_dataManager.captureEvidence.StartVideoRecord(Guid.NewGuid().ToString());

            if (captureResult == -1)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.captureEvidence.Initialize(CaptureType.Video);
                await Preview.Source.StartPreviewAsync();
                await m_dataManager.captureEvidence.StartVideoRecord(Guid.NewGuid().ToString());
            }
        }

        private void StopRecord_Click(object sender, RoutedEventArgs e)
        {
            m_dataManager.captureEvidence.StopVideoRecord();

            UIState(PageState.SavingName);
        }

        private void StartRecordAudio_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            await Preview.Source.StopPreviewAsync();
            this.Frame.Navigate(typeof(EvidenceViewer));
        }

        private void SaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }
        
    }
}
