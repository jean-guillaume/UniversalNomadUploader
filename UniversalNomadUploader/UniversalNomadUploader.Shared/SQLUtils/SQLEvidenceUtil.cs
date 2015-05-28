using System;
using System.Collections.Generic;
using System.Text;
using UniversalNomadUploader.DataModels.FunctionalModels;
using System.Threading.Tasks;
using System.Linq;
using UniversalNomadUploader.Common;
using SQLite;
using UniversalNomadUploader.DataModels.SQLModels;
using Windows.Globalization.Collation;
using System.Collections;

namespace UniversalNomadUploader.SQLUtils
{
    public class SQLEvidenceUtil
    {
        /// <summary>
        /// For windows Phone
        /// </summary>
        /// <returns></returns>
        public async static Task<IList> GetEvidenceAsyncPhone()
        {
            return await Task.Run(
                () => GetEvidencePhone()
              );
        }

        public static IList GetEvidencePhone()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return EvidenceConverter.ToFunctionalEvidence(db.Table<SQLEvidence>().Where(usr => usr.UserID == GlobalVariables.LoggedInUser.UserID)).ToAlphaGroups(x => x.Name);
            }
        }

        /// <summary>
        /// For windows desktop
        /// </summary>
        /// <returns></returns>
        public async static Task<IList> GetEvidenceAsync()
        {
            return await Task.Run(
                () => GetEvidence()
              );
        }

        public static IList GetEvidence()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return EvidenceConverter.ToFunctionalEvidence(db.Table<SQLEvidence>().Where(usr => usr.UserID == GlobalVariables.LoggedInUser.UserID))
                    .GroupBy(x => x.CreatedDate.Date.ToString("dd MMM yyyy")).OrderByDescending(x => Convert.ToDateTime(x.Key)).ToList();
            }
        }

        public async static Task<int> InsertEvidenceAsync(FunctionnalEvidence evi)
        {
            return await Task.FromResult<int>(InsertEvidence(evi));
        }

        public static int InsertEvidence(FunctionnalEvidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return db.Insert(new DataModels.SQLModels.SQLEvidence(evi) { TriedUpload = false });
            }
        }

        public async static Task UpdateEvidenceSyncStatusAsync(FunctionnalEvidence evi)
        {
            await Task.Run(() => UpdateEvidenceSyncStatus(evi));
        }

        public static void UpdateEvidenceSyncStatus(FunctionnalEvidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.SQLEvidence dbEvi = db.Table<DataModels.SQLModels.SQLEvidence>().Where(ev => ev.FileName == evi.FileName).SingleOrDefault();
                if (dbEvi != null)
                {
                    dbEvi.TriedUpload = evi.HasTryUploaded;
                    if (evi.UploadedDate != null && evi.UploadedDate != DateTime.MinValue)
                    {
                        dbEvi.UploadedDate = evi.UploadedDate;
                        dbEvi.UploadError = "";
                    }
                    else if (evi.UploadError != null)
                        dbEvi.UploadError = evi.UploadError;
                    db.Update(dbEvi);
                }
            }
        }

        public async static Task UpdateEvidenceNameAsync(FunctionnalEvidence evi)
        {
            await Task.Run(() => UpdateEvidenceName(evi));
        }

        public static void UpdateEvidenceName(FunctionnalEvidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.SQLEvidence dbEvi = db.Table<DataModels.SQLModels.SQLEvidence>().Where(ev => ev.FileName == evi.FileName).SingleOrDefault();
                if (dbEvi != null)
                {
                    dbEvi.Name = evi.Name;
                    db.Update(dbEvi);
                }
            }
        }

        public static String GetFriendlyName(String LoacalFileName)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.SQLEvidence dbEvi = db.Table<DataModels.SQLModels.SQLEvidence>().Where(ev => ev.FileName == LoacalFileName).SingleOrDefault();
                if (dbEvi != null)
                {
                    return dbEvi.Name;
                }
                return "";
            }
        }

        public async static Task DeleteAsync(FunctionnalEvidence evidence)
        {
            await Task.Run(() => Delete(evidence));
        }

        public static void Delete(FunctionnalEvidence evidence)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.SQLEvidence dbEvi = db.Table<DataModels.SQLModels.SQLEvidence>().Where(ev => ev.LocalID == evidence.LocalID).SingleOrDefault();
                db.Delete(dbEvi);
            }
        }
    }
}