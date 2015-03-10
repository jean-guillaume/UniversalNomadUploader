using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Storage;

namespace UniversalNomadUploader
{
    public enum CaptureType { Audio, Photo, Video };

    public class CaptureEvidence
    {
        private Camera m_Camera;
        private Microphone m_Microphone;

        public CaptureEvidence()
        {
            m_Camera = new Camera();
            m_Microphone = new Microphone();
        }

        /// <summary>
        /// Initialize the recording device for capture a Photo/Video/Audio record
        /// </summary>
        /// <param name="_captureType">Type of record</param>
        /// <returns>Access to the device handler, should be only used to initialize a UI CaptureElement for a Photo/Video record</returns>
        public async Task<MediaCapture> Initialize(CaptureType _captureType = CaptureType.Photo)
        {
            MediaCapture mediaCapture = null;

            switch (_captureType)
            {
                case (CaptureType.Video):
                    mediaCapture = await m_Camera.Initialize(CaptureUse.Video);
                    break;

                case (CaptureType.Photo):
                    mediaCapture = await m_Camera.Initialize(CaptureUse.Photo);
                    break;

                default: break;
            }           
            

            return mediaCapture;
        }

        /// <summary>
        /// Take a picture
        /// </summary>
        /// <param name="_filename">Name of the file where the picture will be saved</param>
        /// <returns>StorageFile object containing the picture. Return null if not initialized or is currently recording</returns>
        public async Task<StorageFile> TakePicture(String _filename)
        {
            try
            {
                return await m_Camera.takePicture(_filename);
            }
            catch (Camera.MediaTypeException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Start the recording of a video
        /// </summary>
        /// <param name="_fileName">Name of the file where the video will be saved.</param>
        /// <returns>StorageFile object containing the picture. Return null if not initialized or is currently recording</returns>
        public async Task<StorageFile> StartVideoRecord(String _filename)
        {
            try
            {
                return await m_Camera.startRecording(_filename);
            }
            catch (Camera.MediaTypeException ex)
            {
                throw ex;
            }
        }

        public async Task StopVideoRecord()
        {
            await m_Camera.stopVideoRecording();
        }        

        public async Task StartPreview()
        {
            await m_Camera.startPreview();
        }

        public async Task StopPreview()
        {
            await m_Camera.stopPreview();
        }

        public Double getOrientationAngle(SimpleOrientation _orientation)
        {
            return m_Camera.OrientationAngle(_orientation);
        }

        public void DisposeCamera()
        {
            m_Camera.Dispose();
        }                
        
        public async Task StartAudioRecord()
        {
            await m_Microphone.StartRecord();
        }

        public async Task PauseAudioRecord()
        {
            await m_Microphone.PauseRecord();
        }

        public async Task StopAudioRecord()
        {
            await m_Microphone.StopRecord();
        }

        public async Task<StorageFile> SaveAudioRecord(String _fileName)
        {
            return await m_Microphone.SaveRecord(_fileName);
        }
    }
}
