using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UniversalNomadUploader
{
    class Microphone
    {
        MediaCapture m_AudioMediaCapture;
        IRandomAccessStream m_audioStream;
        Byte[] m_PausedBuffer;
        Boolean m_isStopped = true;

        public Microphone()
        {
            m_AudioMediaCapture = new MediaCapture();
        }

        /// <summary>
        /// Initialize the capture device for audio recording
        /// </summary>
        /// <returns> MediaCapture object, permits to have direct access to the device</returns>
        private async Task<MediaCapture> Initialize()
        {
            var settings = new MediaCaptureInitializationSettings();
            settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            settings.MediaCategory = MediaCategory.Other;
            settings.AudioProcessing = Windows.Media.AudioProcessing.Default;

            try
            {
                await m_AudioMediaCapture.InitializeAsync(settings);
            }
            catch (Exception)
            {
                return null;
            }
            m_AudioMediaCapture.RecordLimitationExceeded += AudioMediaCapture_RecordLimitationExceeded;
            m_AudioMediaCapture.Failed += AudioMediaCapture_Failed;

            return m_AudioMediaCapture;
        }

        void AudioMediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            throw new Exception("The recording has stopped because you exceeded the maximum recording length.");
        }

        void AudioMediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {

            throw new Exception("The audio capture failed: {0}\n" + errorEventArgs.Message);
        }

        /// <summary>
        /// Start audio recording 
        /// </summary>      
        /// <returns></returns>
        public async Task StartRecord()
        {
            m_isStopped = false;

            await Initialize();

            try
            {
                m_audioStream = new InMemoryRandomAccessStream();

                MediaEncodingProfile recordProfile = null;
                if (GlobalVariables.IsWindowsPhone())
                {
                    //TODO UPDATE 26/03/2015: on next update of WINDOWS PHONE check if MediaEncodingProfile.CreateMp3 works again
                    recordProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
                }
                else
                {
                    recordProfile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto);
                }

                await m_AudioMediaCapture.StartRecordToStreamAsync(recordProfile, m_audioStream);
            }
            catch (Exception ex)
            {
                if (m_audioStream != null)
                {
                    m_audioStream.Dispose();
                    m_audioStream = null;
                }

                throw ex;
            }
        }

        /// <summary>
        /// Pause the record
        /// </summary>
        public async Task PauseRecord()
        {
            //TODO UPDATE 26/03/2015 On windows phone 8.1 the pause/resume doesn't works, have to wait next update of windows phone            
            if (GlobalVariables.IsWindowsPhone())
            {
                return;
            }

            await m_AudioMediaCapture.StopRecordAsync();
            m_isStopped = true;

            using (var dataReader = new DataReader(m_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)m_audioStream.Size);
                if (m_PausedBuffer == null)
                {
                    m_PausedBuffer = new byte[(int)m_audioStream.Size];
                    dataReader.ReadBytes(m_PausedBuffer);
                }
                else
                {
                    int currlength = m_PausedBuffer.Length;
                    byte[] temp = new byte[(int)m_audioStream.Size];
                    dataReader.ReadBytes(temp);
                    Array.Resize(ref m_PausedBuffer, (int)m_audioStream.Size + m_PausedBuffer.Length);
                    Array.Copy(temp, 0, m_PausedBuffer, currlength, temp.Length);
                }
            }
        }

        /// <summary>
        /// Stop the record
        /// </summary>
        public async Task StopRecord()
        {
            if (m_isStopped == false)
            {
                await m_AudioMediaCapture.StopRecordAsync();
                m_isStopped = true;
            }
        }

        /// <summary>
        /// Save audio record
        /// </summary>
        /// <param name="_fileName">name of the file containing the record</param>
        /// <returns>the file containing the record </returns>
        public async Task<StorageFile> SaveRecord(String _fileName)
        {
            if (m_isStopped == false)
            {
                await this.StopRecord();
                m_isStopped = false;
            }

            String extension = null;

            if (GlobalVariables.IsWindowsPhone())
            {
                //TODO UPDATE 26/03/2015 on next update if CreateMp3 works suppress condition
                extension = ".wav";
            }
            else
            {
                extension = ".mp3";
            }

            StorageFile storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_fileName + extension, CreationCollisionOption.GenerateUniqueName);

            using (var dataReader = new DataReader(m_audioStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)m_audioStream.Size);
                byte[] buffer = new byte[(int)m_audioStream.Size];
                dataReader.ReadBytes(buffer);
                if (m_PausedBuffer != null && buffer.Length > 0)
                {
                    int currlength = m_PausedBuffer.Length;
                    Array.Resize(ref m_PausedBuffer, buffer.Length + m_PausedBuffer.Length);
                    Array.Copy(buffer, 0, m_PausedBuffer, currlength, buffer.Length);
                    await FileIO.WriteBytesAsync(storageFile, m_PausedBuffer);
                }
                else if (m_PausedBuffer != null && buffer.Length == 0)
                {
                    await FileIO.WriteBytesAsync(storageFile, m_PausedBuffer);
                }
                else
                {
                    await FileIO.WriteBytesAsync(storageFile, buffer);
                }
            }

            /*m_PausedBuffer = null;
            m_isStopped = false;*/
            return storageFile;
        }

        /// <summary>
        /// free all ressources of the Microphone object
        /// </summary>
        public void Dispose()
        {
            if (m_AudioMediaCapture != null)
            {
                m_AudioMediaCapture.Dispose();
                m_audioStream.Dispose();
                m_audioStream = null;
                m_AudioMediaCapture = null;
            }
        }
    }
}
