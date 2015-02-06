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

namespace UniversalNomadUploader
{
    public sealed partial class EvidenceView : Page
    {
        private async void EnterPreviewMode_Click(object sender, RoutedEventArgs e)
        {
            m_camera = new Camera.Camera();
            Preview.Source = await m_camera.Initialize(CaptureUse.Photo);
            await m_camera.startPreview();

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            Appbar.IsOpen = false;
            Appbar.IsSticky = false;
            DisableButtons(PageState.PreviewModePhoto);
        }

        private async void LeavePreviewMode_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons(PageState.Default);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            await m_camera.stopPreview();
            freeResources();
        }

        //Permits to free the resources of the camera and the Preview. 
        private void freeResources()
        {
            if (m_camera != null)
            {
                Preview.Source = null;
                m_camera.Dispose();
                m_camera = null;
            }
        }

        //Parameter : the use you want (photo or video)
        private async Task RecordPictureOrVideo(CaptureUse _CurrentUse)
        {
            StorageFile testingRecord = null;
            String NewFileName = Guid.NewGuid().ToString();
            Evidence evi = new Evidence();
            evi.FileName = NewFileName;
            evi.CreatedDate = DateTime.Now;
            evi.ServerID = (int)GlobalVariables.SelectedServer;


            //Photo Capturing
            if (_CurrentUse == CaptureUse.Photo)
            {
                bool mediaTypeExceptionCatched = false;

                try
                {
                    testingRecord = await m_camera.takePicture(NewFileName);
                }
                catch (Camera.Camera.MediaTypeException)
                {
                    mediaTypeExceptionCatched = true;
                }

                if (mediaTypeExceptionCatched == true)
                {
                    if (Preview.Source != null)
                    {
                        await m_camera.stopPreview();
                    }
                    Preview.Source = await m_camera.Initialize(CaptureUse.Photo);
                    await m_camera.startPreview();

                    testingRecord = await m_camera.startRecording(NewFileName);
                }
                evi.Type = MimeTypes.Picture;                

                //  ShowNewName();
                LeavePreviewMode_Click(null, null);
                DisableButtons(PageState.SetNewName);
            }
            //Video recording
            else
            {
               
                bool mediaTypeExceptionCatched = false;                
                evi.Type = MimeTypes.Movie;

                if (m_CurrentState != PageState.RecordingVideo)
                {
                    try
                    {
                        testingRecord = await m_camera.startRecording(NewFileName);
                        DisableButtons(PageState.RecordingVideo);
                    }
                    catch (Camera.Camera.MediaTypeException)
                    {
                        mediaTypeExceptionCatched = true;
                    }

                    if (mediaTypeExceptionCatched == true)
                    {
                        if (Preview.Source != null)
                        {
                            await m_camera.stopPreview();
                        }
                        Preview.Source = await m_camera.Initialize(CaptureUse.Video);
                        await m_camera.startPreview();

                        testingRecord = await m_camera.startRecording(NewFileName);
                        DisableButtons(PageState.RecordingVideo);
                    }
                }
                else if (m_CurrentState == PageState.RecordingVideo)
                {
                    await m_camera.stopVideoRecording();

                    LeavePreviewMode_Click(null, null);
                    DisableButtons(PageState.SetNewName);
                }

            }

            if (testingRecord != null)
            {
                evi.Extension = testingRecord.FileType.Replace(".", "");
                evi.Size = Convert.ToDouble((await testingRecord.GetBasicPropertiesAsync()).Size);
                evi.UserID = GlobalVariables.LoggedInUser.LocalID;
                evi.LocalID = await EvidenceUtil.InsertEvidenceAsync(evi);
                CurrentEvidence = evi;
                CaptureContainer.Visibility = Visibility.Collapsed;
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            }
        }

        //Record video
        private async void StartStopRecord_Click(object sender, RoutedEventArgs e)
        {
            if (m_CurrentState == PageState.PreviewModePhoto || Preview.Source == null)
            {
                if (Preview.Source != null)
                {
                    await m_camera.stopPreview();
                }
                Preview.Source = await m_camera.Initialize(CaptureUse.Video);
                await m_camera.startPreview();
                DisableButtons(PageState.PreviewModeVideo);
            }
            else
            {
                await RecordPictureOrVideo(CaptureUse.Video);
            }
        }

        //Record picture
        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            await RecordPictureOrVideo(CaptureUse.Photo);
        }

        private async void StopRecord_Click(object sender, RoutedEventArgs e)
        {
            await m_camera.stopVideoRecording();
            DisableButtons(PageState.SetNewName);
            await Preview.Source.StopPreviewAsync();
        }

        private async void CancelRecordingMode_Click(object sender, RoutedEventArgs e)
        {
            await Preview.Source.StopPreviewAsync();
            DisableButtons(PageState.Default);

        }
    }
}
