using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.SQLModels;
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
            SavingName
        }

        public CaptureView()
        {
            this.InitializeComponent();
            m_dataManager = new DataManager();
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
            Preview.Source = await m_dataManager.InitializeCamera(CaptureType.Photo);
            await Preview.Source.StartPreviewAsync();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            UIState(PageState.Default);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
            await Preview.Source.StopPreviewAsync();
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
                Preview.Source = await m_dataManager.InitializeCamera(CaptureType.Photo);
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


        /*private async void LeavePreviewMode_Click(object sender, RoutedEventArgs e)
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
                    //Appbar.IsEnabled = true;

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.AudioRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    //Appbar.IsEnabled = false;

                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.VideoRecording:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    //Appbar.IsEnabled = false;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;

                case PageState.SavingName:
                    Appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    //Appbar.IsEnabled = false;

                    VideoRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    AudioRecordBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    SavingNameGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    NewName.Text = "";
                    NewName.Focus(FocusState.Pointer);
                    break;
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            /* for (int i = 0; i < 30; i++)
             {*/
            String fileName = Guid.NewGuid().ToString();
            m_file = await m_dataManager.TakePicture(fileName);

            if (m_file == null)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.InitializeCamera(CaptureType.Photo);
                await Preview.Source.StartPreviewAsync();
                m_file = await m_dataManager.TakePicture(fileName);
            }

            m_mimeTypeEvi = MimeTypes.Picture;

            UIState(PageState.SavingName);
            /*
            //////////////////////////////
            NewName.Text = "b";
            //////////////////////////
            if (await m_dataManager.AddEvidence(m_file, NewName.Text, m_mimeTypeEvi) == -1)
            {
                String message = "";
                if (String.IsNullOrEmpty(NewName.Text) == true)
                {
                    message = "The evidence must have a name";
                }
                else
                {
                    message = "The evidence has failed to be saved";
                }

                MessageDialog msgDialog = new MessageDialog(message, "Required");
                await msgDialog.ShowAsync();
                UIState(PageState.SavingName);
                return;
            }
            m_file = null; // put in the initial value for the next test on null
            UIState(PageState.Default);
        }*/
        }

        private async void StartVideoRecord_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.VideoRecording);

            String fileName = Guid.NewGuid().ToString();
            m_file = await m_dataManager.StartVideoRecord(fileName);

            if (m_file == null)
            {
                await Preview.Source.StopPreviewAsync();
                Preview.Source = await m_dataManager.InitializeCamera(CaptureType.Video);
                await Preview.Source.StartPreviewAsync();
                m_file = await m_dataManager.StartVideoRecord(Guid.NewGuid().ToString());
            }
        }

        private void StopRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            m_dataManager.StopVideoRecord();

            m_mimeTypeEvi = MimeTypes.Movie;

            UIState(PageState.SavingName);
        }

        private async void StartRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.AudioRecording);

            String filename = Guid.NewGuid().ToString();
            m_file = await m_dataManager.StartAudioRecord(filename);

            if (m_file == null)
            {
                await m_dataManager.InializeMicrophone();
                m_file = await m_dataManager.StartAudioRecord(Guid.NewGuid().ToString());
            }
        }

        private void StopRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            m_dataManager.StopAudioRecord();

            m_mimeTypeEvi = MimeTypes.Audio;

            UIState(PageState.SavingName);
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            if (await m_dataManager.AddEvidence(m_file, NewName.Text, m_mimeTypeEvi) == -1)
            {
                String message = "";
                if (String.IsNullOrEmpty(NewName.Text) == true)
                {
                    message = "The evidence must have a name";
                }
                else
                {
                    message = "The evidence has failed to be saved";
                }

                MessageDialog msgDialog = new MessageDialog(message, "Required");
                await msgDialog.ShowAsync();
                UIState(PageState.SavingName);
                return;
            }

            if (m_mimeTypeEvi == MimeTypes.Movie || m_mimeTypeEvi == MimeTypes.Picture)
            {

                StorageFolder VideoThumbs = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("_VidThumbs", CreationCollisionOption.OpenIfExists);
                StorageFile VideoThumb = await VideoThumbs.CreateFileAsync(m_file.DisplayName + ".jpg", CreationCollisionOption.ReplaceExisting);

                using (var stream = await m_file.GetThumbnailAsync(ThumbnailMode.VideosView))
                {
                    stream.AsStream().CopyTo(await VideoThumb.OpenStreamForWriteAsync());
                }

                await m_file.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, m_file.DisplayName + m_file.FileType, NameCollisionOption.ReplaceExisting);
            }

            m_file = null; // put in the initial state
            UIState(PageState.Default);
        }

        private void CancelSaveName_Click(object sender, RoutedEventArgs e)
        {
            UIState(PageState.Default);
        }
    }
}
