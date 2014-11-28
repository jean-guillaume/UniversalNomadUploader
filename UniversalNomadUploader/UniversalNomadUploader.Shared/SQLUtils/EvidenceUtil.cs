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
                return db.Insert(new DataModels.SQLModels.Evidence(evi));
            }
        }

        public async static Task UpdateEvidenceAsync(Evidence evi)
        {
            await Task.Run(() => UpdateEvidence(evi));
        }

        public static void UpdateEvidence(Evidence evi)
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
    }
}
