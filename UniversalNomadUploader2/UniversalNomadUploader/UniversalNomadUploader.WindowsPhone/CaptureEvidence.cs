using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private StorageFile record;

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

        public async Task<int> TakePicture(String _filename)
        {
            try
            {
                record = await m_Camera.takePicture(_filename);
            }
            catch (Camera.MediaTypeException)
            {
                return -1;
            }

            return 1;
        }

        public async Task<int> StartVideoRecord(String _filename)
        {
            try
            {
                record = await m_Camera.startRecording(_filename);
            }
            catch (Camera.MediaTypeException)
            {
                return -1;
            }

            return 1;
        }

        public async void StopVideoRecord()
        {
            await m_Camera.stopVideoRecording();
        }

        public async void StartAudioRecord(String _filename)
        {
            await m_Microphone.StartRecord(_filename);
        }

        public void StopAudioRecord()
        {
            m_Microphone.StopRecord();
        }

        public async void StartPreview()
        {
            await m_Camera.startPreview();
        }

        public async void StopPreview()
        {
            await m_Camera.stopPreview();
        }
    }
}
