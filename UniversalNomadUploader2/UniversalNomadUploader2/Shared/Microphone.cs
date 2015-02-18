using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UniversalNomadUploader
{
    class Microphone
    {
        MediaCapture m_AudioMediaCapture;

        public Microphone() { }
        
        public async void Initialize()
        {
            m_AudioMediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings();
            settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            settings.MediaCategory = MediaCategory.Other;
            settings.AudioProcessing = Windows.Media.AudioProcessing.Default;

            await m_AudioMediaCapture.InitializeAsync(settings);
            
            /*_mediaCaptureManager.RecordLimitationExceeded += new RecordLimitationExceededEventHandler(RecordLimitationExceeded);
            _mediaCaptureManager.Failed += new MediaCaptureFailedEventHandler(Failed);
        */}

        public async Task<StorageFile> StartRecord (string _fileName)
        {
            StorageFile storageFile = null;

            try
            {
                storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_fileName + ".m4a", CreationCollisionOption.GenerateUniqueName);
                MediaEncodingProfile recordProfile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Auto);
                await m_AudioMediaCapture.StartRecordToStorageFileAsync(recordProfile, storageFile);
            }
            catch (Exception)
            {
                throw new Exception("Please allow Nomad Uploader to access your microphone from the permissions charm.");
            }

            return storageFile;
        }

        public async void StopRecord()
        {
            await m_AudioMediaCapture.StopRecordAsync();
        }
    }
}
