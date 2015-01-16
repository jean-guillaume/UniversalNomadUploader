using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Camera
{

    //TODO : handling error case and verify all the parameters
    class Camera : IDisposable
    {
        private MediaCapture m_mediaCapture;                    //Handle the camera
        private ImageEncodingProperties m_imgEncodingProp;      //Properties of the photo
        private MediaEncodingProfile m_videoEncodingProp;       //Properties of the video
        private CaptureUse m_captureUse;                        //Dertemines if we gonna take a photo or a video

        public Camera()
        {

        }

        /**
         *Initialize encoding for the photo and the video
         *Return: useful for Preview.Source = await camera.Initialize(CaptureUse.Video);
         */
        public async Task<MediaCapture> Initialize(CaptureUse _use = CaptureUse.Photo)
        {
            // Create MediaCapture and init
            m_mediaCapture = new MediaCapture();
            await m_mediaCapture.InitializeAsync();
            m_mediaCapture.VideoDeviceController.PrimaryUse = _use;

            // Create photo encoding properties as JPEG and set the size that should be used for photo capturing
            m_imgEncodingProp = ImageEncodingProperties.CreateJpeg();
            m_imgEncodingProp.Width = 640;
            m_imgEncodingProp.Height = 480;

            // Create video encoding profile as MP4 
            m_videoEncodingProp = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga);
            // Lots of properties for audio and video could be set here...

            m_captureUse = _use;

            return m_mediaCapture;
        }

        public async Task<StorageFile> startRecording(String _fileName)
        {
            StorageFile storageFile = null;

            try
            {
                if (m_captureUse == CaptureUse.Photo)
                {

                    // Create new unique file in the pictures library and capture photo into it
                    storageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(_fileName + ".jpg", CreationCollisionOption.GenerateUniqueName);
                    await m_mediaCapture.CapturePhotoToStorageFileAsync(m_imgEncodingProp, storageFile);
                }
                else if (m_captureUse == CaptureUse.Video)
                {
                    // Create new unique file in the videos library and record video! 
                    storageFile = await KnownFolders.VideosLibrary.CreateFileAsync(_fileName + ".mp4", CreationCollisionOption.GenerateUniqueName);
                    await m_mediaCapture.StartRecordToStorageFileAsync(m_videoEncodingProp, storageFile);
                }
            }
            //If the authorization to write is not given
            catch (UnauthorizedAccessException ex)
            {
                string a = ex.Message;
            }
            catch(Exception ex)
            {
                string a = ex.Message;
            }

            return storageFile;
        }

        public async Task stopVideoRecording()
        {
            await m_mediaCapture.StopRecordAsync();
        }

        public async Task startPreview()
        {
            await m_mediaCapture.StartPreviewAsync();
        }

        public async Task stopPreview()
        {
            await m_mediaCapture.StopPreviewAsync();
        }

        public void Dispose()
        {
            if (m_mediaCapture != null)
            {
                m_mediaCapture.Dispose();
                m_mediaCapture = null;
            }
        }
    }
}
