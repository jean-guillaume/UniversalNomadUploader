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

        public async Task<MediaCapture> Initialize(CaptureType _captureType = CaptureType.Photo)
        {
            MediaCapture mediaCapture = null;

            switch (_captureType)
            {
                case (CaptureType.Audio):
                    m_Microphone.Initialize();
                    break;

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

        #region Camera

        public async Task<StorageFile> TakePicture(String _filename)
        {
            try
            {
                return await m_Camera.takePicture(_filename);
            }
            catch (Camera.MediaTypeException)
            {
                return null;
            }
        }

        public async Task<StorageFile> StartVideoRecord(String _filename)
        {
            try
            {
                return await m_Camera.startRecording(_filename);
            }
            catch (Camera.MediaTypeException)
            {
                return null;
            }
        }

        public async void StopVideoRecord()
        {
            await m_Camera.stopVideoRecording();
        }        

        public async void StartPreview()
        {
            await m_Camera.startPreview();
        }

        public async void StopPreview()
        {
            await m_Camera.stopPreview();
        }

        #endregion 

        #region Microphone

        public async Task<StorageFile> StartAudioRecord(String _filename)
        {
            return await m_Microphone.StartRecord(_filename);
        }

        public async void StopAudioRecord()
        {
            await m_Microphone.StopRecord();
        }

        #endregion 
    }
}
