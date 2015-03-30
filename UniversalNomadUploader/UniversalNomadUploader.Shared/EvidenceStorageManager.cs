using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.DataModels.SQLModels;
using UniversalNomadUploader.SQLUtils;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using UniversalNomadUploader.Common;
using System.Collections;
using UniversalNomadUploader.DataModels.APIModels;

namespace UniversalNomadUploader
{
    /// <summary>
    /// This class handle all the modification or reading of the evidences. In this class there is no direct relation with way the data are stored (database, raw files, xml).
    /// </summary>
    public class EvidenceStorageManager
    {
        public EvidenceStorageManager() { }

        public static void InitDB()
        {
            SQLTableUtil.CreateTables();
            SQLServerUtil.SetServers();
        }

        public async Task<EvidenceStatus> AddEvidence(String _fileName, String _extension, DateTime _createdDate, int _serverID, int _userID, String _name, MimeTypes _mimeType, Double _size)
        {
            if (String.IsNullOrEmpty(_fileName) == true)
            {
                return EvidenceStatus.BadFileName;
            }

            if (String.IsNullOrEmpty(_name) == true)
            {
                return EvidenceStatus.BadEvidenceName;
            }

            int maximumUploadSize = GlobalVariables.LoggedInUser.MaximumUploadSize * 1024 * 1024; //*1024 * 1024 to convert in MiB
            int maximumUploadRecordSize = 600 * 1024 * 1024; //TODO in next version must be defined by the user via the website

            if (_size > maximumUploadSize && maximumUploadSize > 0)
            {
                if (_size > maximumUploadRecordSize)
                {
                    if (_mimeType != MimeTypes.Audio && _mimeType != MimeTypes.Movie)
                    {
                        return EvidenceStatus.MaximumSizeFileExceeded;
                    }
                }
            }

            FunctionnalEvidence evi = new FunctionnalEvidence(_fileName, _extension, _createdDate, _serverID, _userID, _name, _mimeType, _size);
            await SQLEvidenceUtil.InsertEvidenceAsync(evi); //Can return SQLiteException

            return EvidenceStatus.OK;
        }

        public async Task<IList> readAllEvidence()
        {
            return await SQLEvidenceUtil.GetEvidenceAsync();
        }

        public async Task<IList> readAllEvidencePhone()
        {
            return await SQLEvidenceUtil.GetEvidenceAsyncPhone();
        }

        public async Task UpdateEvidence(FunctionnalEvidence _evi)
        {
            await SQLEvidenceUtil.UpdateEvidenceNameAsync(_evi);
        }

        public async Task DeleteEvidence(FunctionnalEvidence _evi)
        {
            await SQLEvidenceUtil.DeleteAsync(_evi);
        }
    }
}
