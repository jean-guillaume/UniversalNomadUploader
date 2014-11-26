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

        public async static Task InsertEvidenceAsync(Evidence evi)
        {
            await Task.Run(() => InsertEvidence(evi));
        }

        public static void InsertEvidence(Evidence evi)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                db.Insert(new DataModels.SQLModels.Evidence(evi));
            }
        }
    }
}
