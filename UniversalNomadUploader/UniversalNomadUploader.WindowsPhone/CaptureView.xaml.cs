using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.SQLModels;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
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
        SimpleOrientationSensor m_simpleorientation;
        StorageFile m_file = null;
        MimeTypes m_mimeTypeEvi;
        Double m_currentAngle = 0;

        private enum PageState
        {
            Default,
            AudioRecording,
            VideoRecording,
            SavingName,
            NoCamera
        }

        public CaptureView()
        {
            this.InitializeComponent();
            m_dataManager = new DataManager("","");
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            m_simpleorientation = SimpleOrientationSensor.GetDefault();

            // Assign an event handler for the sensor orientation-changed event
            if (m_simpleorientation != null)
            {
                m_simpleorientation.OrientationChanged += new TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs>(OrientationChanged);
            }
        }

        private async void OrientationChanged(object sender, SimpleOrientationSensorOrientationChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Double nextAngle = m_dataManager.getOrientationAngle(e.Orientation);
                Storyboard sbRotatingButton = (Storyboard)FindName("RotatingBtnAnimation");

                DoubleAnimation rotateAnimation = (DoubleAnimation)FindName("RotatingTakePicture");
                rotateAnimation.From = m_currentAngle;
                rotateAnimation.To = nextAngle;
                rotateAnimation = (DoubleAnimation)FindName("RotatingRecordVideo");
                rotateAnimation.From = m_currentAngle;
                rotateAnimation.To = nextAngle;
                rotateAnimation = (DoubleAnimation)FindName("RotatingRecordAudio");
                rotateAnimation.From = m_currentAngle;
                rotateAnimation.To = nextAngle;

                m_currentAngle = nextAngle;
                sbRotatingButton.Begin();
            });
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            UIState(PageState.Default);

            Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Photo);
            await Preview.Source.StartPreviewAsync();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            UIState(PageState.Default);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;

            if (Preview.Source != null)
            {
                await Preview.Source.StopPreviewAsync();
                await m_dataManager.StopVideoRecord();
            }

            m_dataManager.DisposeCamera();
        }

        async void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                UIState(PageState.Default);
                await Preview.Source.StopPreviewAsync();
                await m_dataManager.StopVideoRecord();
                await m_dataManager.StopAudioRecord();
            }

            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated)
            {
                Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Photo);
            }
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

        private void UIState(PageState _state)
        {
            switch (_state)
            {
                case PageState.Default:
                    SaveName.IsEnabled = false;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    RecordVideo.IsEnabled = true;
                    RecordVideo.IsEnabled = true;                    

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.AudioRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordVideo.IsEnabled = false;

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.VideoRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordVideo.IsEnabled = false;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.SavingName:
                    SaveName.IsEnabled = true;
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    
                    NewName.Text = "";
                    NewName.Focus(FocusState.Pointer);
                    break;

                case PageState.NoCamera:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    NoCameraMsg.Text += "No camera detected.\n";
                    NoCameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            String fileName = Guid.NewGuid().ToString();
            m_file = await m_dataManager.TakePicture(fileName);

            if (m_file == null)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Photo);
                await Preview.Source.StartPreviewAsync();
                m_file = await m_dataManager.TakePicture(fileName);
            }

            m_mimeTypeEvi = MimeTypes.Picture;

            UIState(PageState.SavingName);
        }

        int time = 0;
        DispatcherTimer m_dispatcherTimer;
        private async void StartVideoRecord_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.VideoRecording);

            m_dispatcherTimer = new DispatcherTimer();
            m_dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            m_dispatcherTimer.Tick += EachSeconds;
            
            await Preview.Source.StopPreviewAsync();
            Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Video);
            await Preview.Source.StartPreviewAsync();
            m_file = await m_dataManager.StartVideoRecord(Guid.NewGuid().ToString());
            m_dispatcherTimer.Start();
        }

        private void EachSeconds(object sender, object o)
        {            
            Timer.Text = time++.ToString();
        }

        private async void StopRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.StopVideoRecord();

            m_dispatcherTimer.Stop();
            time = 0;

            m_mimeTypeEvi = MimeTypes.Movie;

            UIState(PageState.SavingName);
        }

        private async void StartAudioRecord_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.AudioRecording);
            await m_dataManager.StartAudioRecord();
        }

        private async void PauseRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            PauseRecordAudio.IsEnabled = false;
            RestartRecordAudio.IsEnabled = true;
            await m_dataManager.PauseAudioRecord();            
        }

        private async void RestartRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            PauseRecordAudio.IsEnabled = true;
            RestartRecordAudio.IsEnabled = false;
            await m_dataManager.StartAudioRecord();
        }

        private async void StopRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.StopAudioRecord();            

            m_mimeTypeEvi = MimeTypes.Audio;

            UIState(PageState.SavingName);
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);

            if( m_mimeTypeEvi == MimeTypes.Audio )
            {
                String fileName = Guid.NewGuid().ToString();
                m_file = await m_dataManager.SaveAudioRecord(fileName);
            }

            EvidenceStatus evidenceStatus = await m_dataManager.AddEvidence(m_file, NewName.Text, m_mimeTypeEvi);
            if ( evidenceStatus != EvidenceStatus.OK )
            {
                String message = "";
                switch (evidenceStatus)
                {
                    case EvidenceStatus.BadEvidenceName:
                        message = "The evidence must have a name";
                        break;
                    case EvidenceStatus.BadFileName:
                        message = "The evidence has failed to be saved";
                        break;
                    case EvidenceStatus.MaximumSizeFileExceeded:
                        message = "The file exceed the maximum size";
                        break;
                    default:
                        message = "Unknown error";
                        break;
                }

                MessageDialog msgDialog = new MessageDialog(message, "Required");
                await msgDialog.ShowAsync();
                UIState(PageState.SavingName);
                return;
            }

            if (m_mimeTypeEvi == MimeTypes.Movie || m_mimeTypeEvi == MimeTypes.Picture)
            {
                StorageFolder VideoThumbs = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(GlobalVariables.thumbnailFolderName, CreationCollisionOption.OpenIfExists);
                StorageFile VideoThumb = await VideoThumbs.CreateFileAsync(m_file.DisplayName + ".jpg", CreationCollisionOption.ReplaceExisting);

                using (var stream = await m_file.GetThumbnailAsync(ThumbnailMode.VideosView))
                {
                    stream.AsStream().CopyTo(await VideoThumb.OpenStreamForWriteAsync());
                }

                await m_file.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, m_file.DisplayName + m_file.FileType, NameCollisionOption.ReplaceExisting);
            }

            m_file = null; // put in the initial state
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }       
    }
}
