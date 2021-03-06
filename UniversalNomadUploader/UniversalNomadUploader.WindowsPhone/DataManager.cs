﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.APIModels;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using Windows.Devices.Sensors;
using Windows.Globalization.Collation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace UniversalNomadUploader
{
    /// <summary>
    /// This class is the link between the back-end of the application and the UI classes. It should stay the only access point to the back-end classes. The exceptions to this rule are:
    ///-Log handler classes
    ///-Functional Models classes
    ///-The video preview classes (Windows Phone 8.1)
    ///-UI oriented classes (ex: the converters in XAML)
    ///Because this class is related to the UI, if the UI change the class could have to be modified (Especially on different OS like Windows 8.1 and Windows Phone 8.1).
    /// </summary>
    public class DataManager
    {
        private EvidenceStorageManager m_ESM;
        private ServerManager m_ServerManager;
        private CaptureDeviceManager m_CaptureEvidence;

        public DataManager(String _username, String _password)
        {
            m_CaptureEvidence = new CaptureDeviceManager();
            m_ESM = new EvidenceStorageManager();
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

            return (await this.m_ESM.AddEvidence(_file.DisplayName, _file.FileType.Replace(".", ""), DateTime.Now.Date, (int)GlobalVariables.SelectedServer, GlobalVariables.LoggedInUser.UserID, _name, _mimeType, size));
        }

        public async Task<IList> ReadAllEvidence()
        {
            return await m_ESM.readAllEvidencePhone();
        }

        public async Task UpdateEvidence(FunctionnalEvidence _evi)
        {
            await m_ESM.UpdateEvidence(_evi);
        }

        public async Task DeleteEvidence(FunctionnalEvidence _evi)
        {
            await m_ESM.DeleteEvidence(_evi);

            StorageFile eviFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_evi.FileName + "." + _evi.Extension);
            await eviFile.DeleteAsync();

            if (_evi.Type == MimeTypes.Movie || _evi.Type == MimeTypes.Picture)
            {
                StorageFolder thumbnailFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("_VidThumbs");
                StorageFile eviThumbnailFile = await thumbnailFolder.GetFileAsync(_evi.FileName + ".jpg");
                await eviThumbnailFile.DeleteAsync();
            }
        }

        public async Task<MediaCapture> InitializeMediaCapture(CaptureType _type)
        {
            return await m_CaptureEvidence.Initialize(_type);
        }

        /// <summary>
        /// Take a picture via Camera class
        /// </summary>
        /// <param name="_fileName">Name of the file which will contains the evidence</param>
        /// <returns></returns>
        public async Task<StorageFile> TakePicture(String _fileName)
        {
            return await m_CaptureEvidence.TakePicture(_fileName);
        }

        public async Task<StorageFile> StartVideoRecord(String _fileName)
        {
            return await m_CaptureEvidence.StartVideoRecord(_fileName);
        }

        public async Task StopVideoRecord()
        {
            await m_CaptureEvidence.StopVideoRecord();
        }

        public Double getOrientationAngle(SimpleOrientation _orientation)
        {
            return m_CaptureEvidence.getOrientationAngle(_orientation);
        }

        public void DisposeCamera()
        {
            m_CaptureEvidence.DisposeCamera();
        }

        /// <summary>
        /// Start an audio record
        /// </summary>
        public async Task StartAudioRecord()
        {
            await m_CaptureEvidence.StartAudioRecord();
        }

        public async Task PauseAudioRecord()
        {
            await m_CaptureEvidence.PauseAudioRecord();
        }

        /// <summary>
        /// Stop an audio record
        /// </summary>
        public async Task StopAudioRecord()
        {
            await m_CaptureEvidence.StopAudioRecord();
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
        /// Upload an evidence to the server
        /// </summary>
        /// <param name="_evi">Evidence to upload</param>
        /// <returns></returns>
        public async Task<UploadStatus> UploadEvidence(FunctionnalEvidence _evi)
        {
            if (GlobalVariables.IsOffline || !GlobalVariables.HasInternetAccess() || await m_ServerManager.VerifySessionAsync())
            {
                return UploadStatus.NoInternetConnection;
            }

            return await m_ServerManager.UploadEvidence(_evi);
        }

        public async Task<connectionStatus> ConnectToServer()
        {
            return (await m_ServerManager.Connect());
        }

        public void CancelProcessing()
        {
            m_ServerManager.CancelProcessing();
        }
    }

    //TODO move this class in his own file
    public class EvidenceGrouped
    {
        private String key;

        public EvidenceGrouped(String _key)
        {
            key = _key;
        }

        public String Key
        {
            get { return key; }
        }
    }
}


