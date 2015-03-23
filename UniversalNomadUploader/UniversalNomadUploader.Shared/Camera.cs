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

        /// <summary>
        /// Initialize the camera, this method have to be called before any action
        /// </summary>
        /// <param name="_use">Define the purpose of the Camera (Video or Photo)</param>
        /// <returns>Return a MedicaCapture object. It should be use to initialize a UI CaptureElement. If failed return null</returns>
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

            try
            {
                await m_mediaCapture.InitializeAsync();
            }
            catch (Exception)
            {
                return null;
            }

            m_mediaCapture.SetPreviewRotation(VideoRotation.Clockwise90Degrees);
            m_mediaCapture.VideoDeviceController.PrimaryUse = _use;

            m_imgEncodingProp = ImageEncodingProperties.CreateJpeg();
            m_videoEncodingProp = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Ntsc);
                        
            m_captureUse = _use;
            m_CurrentState = State.Initialized;

            m_mediaCapture.Failed += CameraMediaCapture_Failed;
            m_mediaCapture.RecordLimitationExceeded += CameraMediaCapture_RecordLimitationExceeded;

            return m_mediaCapture;
        }

        void CameraMediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            throw new Exception("The recording has stopped because you exceeded the maximum recording length.");
        }

        void CameraMediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new Exception("The camera capture failed: {0}\n" + errorEventArgs.Message);
        }

        /// <summary>
        /// Take a picture
        /// </summary>
        /// <param name="_fileName"> Name of the file where the picture will be saved</param>
        /// <returns> StorageFile object containing the picture. Return null if not initialized or is currently recording</returns>
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

        /// <summary>
        /// Start the recording of a video
        /// </summary>
        /// <param name="_fileName">Name of the file where the video will be saved.</param>
        /// <returns>StorageFile object containing the picture. Return null if not initialized or is currently recording</returns>
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

        /// <summary>
        /// Stop the actual record. If the camera is not recording the method do nothing.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Start the preview of the camera
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Stop the preview of the camera
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Give the rotation angle of the phone in degrees
        /// </summary>
        /// <param name="_orientation"> Orientation object</param>
        /// <returns>Orientation angle in degrees</returns>
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

        /// <summary>
        /// free all ressources of the Camera object
        /// </summary>
        public void Dispose()
        {
            if (m_mediaCapture != null)
            {
                m_mediaCapture.Dispose();
                m_mediaCapture = null;
            }
        }

        /// <summary>
        /// Exception thrown when a Camera object initialized as CaptureUse.Photo try to capture a video and vice versa
        /// </summary>
        public class MediaTypeException : Exception
        {
            public MediaTypeException(string message)
                : base(message)
            {
            }
        }
    }
}
