using System;
using System.Collections.Generic;
using System.Text;
using UniversalNomadUploader.DataModels.FunctionalModels;
using System.Threading.Tasks;
using System.Linq;
using UniversalNomadUploader.Common;
using SQLite;

namespace UniversalNomadUploader.SQLUtils
{
    public class EvidenceUtil
    {
        public async static Task<IEnumerable<Evidence>> GetEvidenceAsync()
        {
            return await Task.Run(
                () => GetEvidence()
              );
        }

        public static IEnumerable<Evidence> GetEvidence()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return EvidenceConverter.ToFunctionalEvidence(db.Table<DataModels.SQLModels.Evidence>().Where(usr => usr.UserID == GlobalVariables.LoggedInUser.LocalID));
            }
        }

        public async static Task<int> InsertEvidenceAsync(Evidence evi)
        {
            return await Task.FromResult<int>(InsertEvidence(evi));
        }

        public static int InsertEvidence(Evidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return db.Insert(new DataModels.SQLModels.Evidence(evi) { HasUploaded = false });
            }
        }


        public async static Task UpdateEvidenceSyncStatusAsync(Evidence evi)
        {
            await Task.Run(() => UpdateEvidenceSyncStatus(evi));
        }

        public static void UpdateEvidenceSyncStatus(Evidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.Evidence dbEvi = db.Table<DataModels.SQLModels.Evidence>().Where(ev => ev.FileName == evi.FileName).SingleOrDefault();
                if (dbEvi != null)
                {
                    dbEvi.HasUploaded = evi.HasUploaded;
                    if (evi.UploadedDate != null)
                        dbEvi.UploadedDate = evi.UploadedDate;
                    if (evi.UploadError != null)
                        dbEvi.UploadError = evi.UploadError;
                    db.Update(dbEvi);
                }
            }
        }

        public async static Task UpdateEvidenceNameAsync(Evidence evi)
        {
            await Task.Run(() => UpdateEvidenceName(evi));
        }

        public static void UpdateEvidenceName(Evidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.Evidence dbEvi = db.Table<DataModels.SQLModels.Evidence>().Where(ev => ev.FileName == evi.FileName).SingleOrDefault();
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
                DataModels.SQLModels.Evidence dbEvi = db.Table<DataModels.SQLModels.Evidence>().Where(ev => ev.FileName == LoacalFileName).SingleOrDefault();
                if (dbEvi != null)
                {
                    return dbEvi.Name;
                }
                return "";
            }
        }

        public async static Task DeleteAsync(Evidence evidence)
        {
            await Task.Run(() => Delete(evidence));
        }

        public static void Delete(Evidence evidence)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.Evidence dbEvi = db.Table<DataModels.SQLModels.Evidence>().Where(ev => ev.LocalID == evidence.LocalID).SingleOrDefault();
                db.Delete(dbEvi);
            }
        }
    }
}