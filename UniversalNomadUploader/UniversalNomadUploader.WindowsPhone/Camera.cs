using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Camera
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
                throw new Exception("Camera.Initialize: Previewing still running, stop him before initalizing.");
            }

            // Create MediaCapture and init
            m_mediaCapture = new MediaCapture();
            await m_mediaCapture.InitializeAsync();
            m_mediaCapture.VideoDeviceController.PrimaryUse = _use;
            m_mediaCapture.SetRecordRotation(VideoRotation.None);

            m_imgEncodingProp = ImageEncodingProperties.CreateJpeg();
            m_imgEncodingProp.Width = 640;
            m_imgEncodingProp.Height = 480;

            m_videoEncodingProp = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);

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

            try
            {
                storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);
                await m_mediaCapture.CapturePhotoToStorageFileAsync(m_imgEncodingProp, storageFile);

            }
            catch (Exception)
            {
                throw;
            }

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

            try
            {
                storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_fileName + ".mp4", CreationCollisionOption.GenerateUniqueName);
                await m_mediaCapture.StartRecordToStorageFileAsync(m_videoEncodingProp, storageFile);
            }
            catch (Exception)
            {
                throw;
            }

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
