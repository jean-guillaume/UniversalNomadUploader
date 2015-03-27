using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.APIUtils;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using Windows.Devices.Sensors;
using Windows.Globalization.Collation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Data;

namespace UniversalNomadUploader
{
    public class DataManager
    {
        private DBManager m_DBManager;
        private ServerManager m_ServerManager;
        private CaptureDeviceManager m_CaptureEvidence;

        public DataManager(String _username, String _password)
        {
            m_CaptureEvidence = new CaptureDeviceManager();
            m_DBManager = new DBManager();
            m_ServerManager = new ServerManager(_username, _password);
        }

        /// <summary>
        /// Add an evidence in the database via DBManager
        /// </summary>
        /// <param name="_file">File containing the evidence</param>
        /// <param name="_name">Name of the evidence</param>
        /// <param name="_mimeType">Type of the evidence (Photo, Video, PDF, etc ...)</param>
        /// <returns>Status: if <0 the operation is a failure</returns>
        public async Task<EvidenceStatus> AddEvidence(StorageFile _file, String _name, MimeTypes _mimeType)
        {
            Double size = Convert.ToDouble((await _file.GetBasicPropertiesAsync()).Size);
            return (await this.m_DBManager.AddEvidence(_file.DisplayName, _file.FileType.Replace(".", ""),
                                                        DateTime.Now.Date, (int)GlobalVariables.SelectedServer,
                                                        GlobalVariables.LoggedInUser.UserID, _name, _mimeType, size));
        }

        public async Task<IList> ReadAllEvidence()
        {
            return await m_DBManager.readAllEvidence();
        }

        public async Task UpdateEvidence(FunctionnalEvidence _evi)
        {
            await m_DBManager.UpdateEvidence(_evi);
        }

        public async Task DeleteEvidence(FunctionnalEvidence _evi)
        {
            await m_DBManager.DeleteEvidence(_evi);

            StorageFile eviFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_evi.FileName + "." + _evi.Extension);
            await eviFile.DeleteAsync();
        }

        /// <summary>
        /// Take a picture via Camera class
        /// </summary>
        /// <param name="_fileName">Name of the file which will contains the evidence</param>
        /// <returns></returns>
        public async Task<StorageFile> TakePicture(String _fileName)
        {
            CameraCaptureUI camera = new CameraCaptureUI();
            StorageFile newPhoto = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (newPhoto != null)
            {
                await newPhoto.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, _fileName + newPhoto.FileType, NameCollisionOption.ReplaceExisting);
            }
            return newPhoto;
        }

        public async Task<StorageFile> StartVideoRecord(String _fileName)
        {
            CameraCaptureUI video = new CameraCaptureUI();
            video.VideoSettings.MaxDurationInSeconds = GlobalVariables.maxRecordTimeMinute * 60; // max time in seconds
            StorageFile newVideo = await video.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (newVideo != null)
            {
                await newVideo.MoveAsync(Windows.Storage.ApplicationData.Current.LocalFolder, _fileName + newVideo.FileType, NameCollisionOption.ReplaceExisting);
            }

            return newVideo;
        }

        /// <summary>
        /// Start an audio record
        /// </summary>
        public async Task StartAudioRecord()
        {
            await m_CaptureEvidence.StartAudioRecord();
        }

        /// <summary>
        /// Stop an audio record
        /// </summary>
        public async Task StopAudioRecord()
        {
            await m_CaptureEvidence.StopAudioRecord();
        }

        public async Task PauseAudioRecord()
        {
            await m_CaptureEvidence.PauseAudioRecord();
        }

        /// <summary>
        /// Save the audio record in a file
        /// </summary>
        /// <param name="_fileName">name of the file containing the record</param>
        /// <returns>File containing the record</returns>
        public async Task<StorageFile> SaveAudioRecord(String _fileName)
        {
            return await m_CaptureEvidence.SaveAudioRecord(_fileName);
        }

        /// <summary>
        /// Log to the server
        /// </summary>
        /// <returns>Status code</returns>
        public async Task<connectionStatus> ConnectToServer()
        {
            return (await m_ServerManager.Connect());
        }

        /// <summary>
        /// Upload an evidence to the server
        /// </summary>
        /// <param name="_evi">Evidence to upload</param>
        /// <returns></returns>
        public async Task<UploadStatus> UploadEvidence(FunctionnalEvidence _evi)
        {
            if (GlobalVariables.IsOffline || !GlobalVariables.HasInternetAccess() || await APIAuthenticationUtil.VerifySessionAsync())
            {
                throw new Exception("No internet connection");
            }

            return await m_ServerManager.UploadEvidence(_evi);
        }

        public void CancelProcessing()
        {
            m_ServerManager.CancelProcessing();
        }
    }   
}


