using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace UniversalNomadUploader
{
    class Camera : IDisposable
    {
        private MediaCapture m_mediaCapture;                    //Handle the camera
        private ImageEncodingProperties m_imgEncodingProp;      //Properties of the photo
        private MediaEncodingProfile m_videoEncodingProp;       //Properties of the video
        private CaptureUse m_captureUse;                        //Dertemines if we gonna take a photo or a video
        private enum State { UnInitialized, Instantiated, Initialized, Previewing, Recording, PreviewingAndRecording };
        private State m_CurrentState = State.UnInitialized;

        public Camera()
        {
            m_CurrentState = State.Instantiated;
        }

        /**
         *Initialize encoding for the photo and the video
         *Return: useful for Preview.Source = await camera.Initialize(CaptureUse.Video);
         */
        public async Task<MediaCapture> Initialize(CaptureUse _use = CaptureUse.Photo)
        {
            if (m_CurrentState != State.Instantiated && m_CurrentState != State.Initialized)
            {
                if (m_CurrentState == State.Previewing)
                {
                    await stopPreview();
                }
                else
                {
                    throw new Exception("Camera.Initialize: Previewing still running, stop it before initalizing.");
                }
            }

            // Create MediaCapture and init
            m_mediaCapture = new MediaCapture();            
            await m_mediaCapture.InitializeAsync();
            m_mediaCapture.SetPreviewRotation(VideoRotation.Clockwise90Degrees);
            m_mediaCapture.VideoDeviceController.PrimaryUse = _use;            

            m_imgEncodingProp = ImageEncodingProperties.CreateJpeg();
            m_videoEncodingProp = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

            m_captureUse = _use;
            m_CurrentState = State.Initialized;
            
            return m_mediaCapture;
        }

        public async Task<StorageFile> takePicture(String _fileName)
        {
            StorageFile storageFile = null;

            if (m_CurrentState != State.Initialized && m_CurrentState != State.Previewing)
            {
                return null;
            }

            if (m_captureUse != CaptureUse.Photo)
            {
                throw new MediaTypeException("Wrong media type. Camera must be initialized with CaptureUse.Photo");
            }

            if (_fileName == "")
            {
                _fileName = "default";
            }

            storageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(_fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);
            await m_mediaCapture.CapturePhotoToStorageFileAsync(m_imgEncodingProp, storageFile);

            return storageFile;
        }

        public async Task<StorageFile> startRecording(String _fileName)
        {
            StorageFile storageFile = null;

            if (m_CurrentState != State.Initialized && m_CurrentState != State.Previewing)
            {
                return null;
            }

            if (m_captureUse != CaptureUse.Video)
            {
                throw new MediaTypeException("Wrong media type. Camera must be initialized with CaptureUse.Video");
            }

            if (_fileName == "")
            {
                _fileName = "default";
            }

            storageFile = await KnownFolders.VideosLibrary.CreateFileAsync(_fileName + ".mp4", CreationCollisionOption.GenerateUniqueName);
            await m_mediaCapture.StartRecordToStorageFileAsync(m_videoEncodingProp, storageFile);

            if (m_CurrentState == State.Previewing)
            {
                m_CurrentState = State.PreviewingAndRecording;
            }
            else
            {
                m_CurrentState = State.Recording;
            }


            return storageFile;
        }

        public async Task stopVideoRecording()
        {
            if (m_CurrentState != State.Recording && m_CurrentState != State.PreviewingAndRecording)
            {
                return;
            }

            await m_mediaCapture.StopRecordAsync();
            
            if (m_CurrentState == State.PreviewingAndRecording)
            {
                m_CurrentState = State.Previewing;
            }
            else
            {
                m_CurrentState = State.Initialized;
            }
        }

        public async Task startPreview()
        {
            if (m_CurrentState != State.Initialized && m_CurrentState != State.Recording)
            {
                return;
            }

            await m_mediaCapture.StartPreviewAsync();

            if (m_CurrentState == State.Recording)
            {
                m_CurrentState = State.PreviewingAndRecording;
            }
            else
            {
                m_CurrentState = State.Previewing;
            }
        }

        public async Task stopPreview()
        {
            if (m_CurrentState != State.Previewing && m_CurrentState != State.PreviewingAndRecording)
            {
                return;
            }

            await m_mediaCapture.StopPreviewAsync();


            if (m_CurrentState == State.PreviewingAndRecording)
            {
                m_CurrentState = State.Recording;
            }
            else
            {
                m_CurrentState = State.Initialized;
            }
        }

        public Double OrientationAngle(SimpleOrientation _orientation)
        {
            switch (_orientation)
            {
                case SimpleOrientation.NotRotated:
                    return 0;

                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;

                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;

                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return -90;

                case SimpleOrientation.Faceup:
                    break;
                case SimpleOrientation.Facedown:
                    break;
                default:
                    break;
            }

            return 0;
        }

        public void Dispose()
        {
            if (m_mediaCapture != null)
            {
                m_mediaCapture.Dispose();
                m_mediaCapture = null;
            }
        }

        public class MediaTypeException : Exception
        {
            public MediaTypeException(string message)
                : base(message)
            {
            }
        }
    }
}
