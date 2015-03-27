using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.UI;
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
        DispatcherTimer m_dispatcherTimer;
        TimeSpan m_elapsedTime;

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
            m_dataManager = new DataManager("", "");
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
            await SwitchCaptureMode(MimeTypes.Picture);
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
                await SwitchCaptureMode(MimeTypes.Picture);
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
                    initTimer();

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Timer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    MaxTime.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.AudioRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordVideo.IsEnabled = false;

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Timer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    MaxTime.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;

                case PageState.VideoRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    RecordVideo.IsEnabled = false;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Timer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    MaxTime.Visibility = Windows.UI.Xaml.Visibility.Visible;
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

                    CameraMsg.Text += "No camera detected.\n";
                    CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
            }
        }

        private void initTimer()
        {
            m_elapsedTime = new TimeSpan();
            m_dispatcherTimer = new DispatcherTimer();
            m_dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            m_dispatcherTimer.Tick += EachSeconds;
            Timer.Text = m_elapsedTime.ToString(@"mm\:ss");
        }

        private async void EachSeconds(object sender, object o)
        {
            m_elapsedTime = m_elapsedTime.Add(m_dispatcherTimer.Interval);
            Timer.Text = m_elapsedTime.ToString(@"mm\:ss");

            if (m_elapsedTime.Minutes == GlobalVariables.maxRecordTimeMinute)
            {
                if (m_mimeTypeEvi == MimeTypes.Audio)
                {
                    StopRecordAudio_Click(null, null);
                }
                else if (m_mimeTypeEvi == MimeTypes.Movie)
                {
                    StopRecordVideo_Click(null, null);
                }

                await DisplayMessage("The record is stopped because it reached the maximum length authorized", "Maximum length reached");
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            if (m_mimeTypeEvi != MimeTypes.Picture)
            {
                CameraMsg.Text = "Switching to Photo mode...";
                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;

                await SwitchCaptureMode(MimeTypes.Picture);
                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                return;
            }

            Boolean doSwitch = false;
            String failReason = null;
            String fileName = Guid.NewGuid().ToString();

            try
            {
                m_file = await m_dataManager.TakePicture(fileName);
            }
            catch (Camera.MediaTypeException ex)
            {
                doSwitch = true;
                failReason = ex.Message;
                m_file = null;
            }
            catch (Exception ex)
            {
                failReason = ex.Message;
                m_file = null;
            }

            if (m_file == null)
            {
                await DisplayMessage(failReason, "Failed to capture a photo");
            }
            else
            {
                UIState(PageState.SavingName);
            }

            if (doSwitch == true)
            {
                await SwitchCaptureMode(MimeTypes.Picture);
            }

            m_mimeTypeEvi = MimeTypes.Picture;

        }

        private async void StartVideoRecord_Click(object sender, RoutedEventArgs e)
        {
            if (m_mimeTypeEvi != MimeTypes.Movie)
            {
                CameraMsg.Text = "Switching to Video mode...";
                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;

                await SwitchCaptureMode(MimeTypes.Movie);

                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }

            UIState(PageState.VideoRecording);
            Boolean doSwitch = false;
            String failReason = null;

            try
            {
                m_dispatcherTimer.Start();
                m_file = await m_dataManager.StartVideoRecord(Guid.NewGuid().ToString());
            }
            catch (Camera.MediaTypeException ex)
            {
                doSwitch = true;
                failReason = ex.Message;
                m_file = null;
            }
            catch (Exception ex)
            {
                failReason = ex.Message;
                m_file = null;
            }

            if (m_file == null)
            {
                await DisplayMessage(failReason, "Failed to start a video record");
            }

            if (doSwitch == true)
            {
                await SwitchCaptureMode(MimeTypes.Movie);
            }

            m_mimeTypeEvi = MimeTypes.Movie;
        }

        private async void StopRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.StopVideoRecord();

            m_dispatcherTimer.Stop();

            UIState(PageState.SavingName);
        }

        private async void StartAudioRecord_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.AudioRecording);

            Boolean captureFailed = false;
            String failReason = null;

            try
            {
                m_dispatcherTimer.Start();
                await m_dataManager.StartAudioRecord();
            }
            catch (Exception ex)
            {
                captureFailed = true;
                failReason = ex.Message;
            }

            if (captureFailed == true)
            {
                await DisplayMessage(failReason, "Failed to start an audio record");
            }
            else
            {
                m_mimeTypeEvi = MimeTypes.Audio;
            }
        }

        private async void PauseRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            PauseRecordAudio.IsEnabled = false;
            RestartRecordAudio.IsEnabled = true;
            await m_dataManager.PauseAudioRecord();
            m_dispatcherTimer.Stop();
        }

        private async void RestartRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            PauseRecordAudio.IsEnabled = true;
            RestartRecordAudio.IsEnabled = false;
            await m_dataManager.StartAudioRecord();
            m_dispatcherTimer.Start();
        }

        private async void StopRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            await m_dataManager.StopAudioRecord();
            m_dispatcherTimer.Stop();

            UIState(PageState.SavingName);
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);

            if (m_mimeTypeEvi == MimeTypes.Audio)
            {
                String fileName = Guid.NewGuid().ToString();
                m_file = await m_dataManager.SaveAudioRecord(fileName);
            }

            EvidenceStatus evidenceStatus = await m_dataManager.AddEvidence(m_file, NewName.Text, m_mimeTypeEvi);
            if (evidenceStatus != EvidenceStatus.OK)
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

                await DisplayMessage(message, "Required");
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

            m_file = null; // put in the initial state of the variable
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }

        private async Task SwitchCaptureMode(MimeTypes _captureMode)
        {
            if (_captureMode == MimeTypes.Picture)
            {
                if (Preview.Source != null)
                {
                    CameraMsg.Text = "Switching to Photo capture mode...";
                    CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    await Preview.Source.StopPreviewAsync();
                }
                Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Photo);
                TakePictureSymbolIcon.Foreground = new SolidColorBrush(Colors.GreenYellow);
                RecordVideoSymbolIcon.Foreground = new SolidColorBrush(Colors.White);
                m_mimeTypeEvi = _captureMode;
                await Preview.Source.StartPreviewAsync();
                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (_captureMode == MimeTypes.Movie)
            {
                if (Preview.Source != null)
                {
                    await Preview.Source.StopPreviewAsync();
                    CameraMsg.Text = "Switching to Video capture mode...";
                    CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                Preview.Source = await m_dataManager.InitializeMediaCapture(CaptureType.Video);
                TakePictureSymbolIcon.Foreground = new SolidColorBrush(Colors.White);
                RecordVideoSymbolIcon.Foreground = new SolidColorBrush(Colors.GreenYellow);
                m_mimeTypeEvi = _captureMode;
                await Preview.Source.StartPreviewAsync();
                CameraMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async Task DisplayMessage(String _message, String _title)
        {
            MessageDialog msg = new MessageDialog(_message, _title);
            await msg.ShowAsync();
        }
    }
}
