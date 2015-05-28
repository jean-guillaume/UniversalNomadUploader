using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.SQLModels;

namespace UniversalNomadUploader.SQLUtils
{
    public class SQLEventLogUtil
    {
        public static async Task InsertEventAsync(String EventDetails, LogType type)
        {
            await Task.Run(() => InsertEvent(EventDetails, type));        
        }

        public static void InsertEvent(string EventDetails, LogType type)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                SQLEventLog log = new SQLEventLog();
                log.EventDetails = EventDetails;
                log.UserID = GlobalVariables.LoggedInUser.LocalID;
                log.ServerID = (int)GlobalVariables.SelectedServer;
                log.Type = (int)type;
                log.EventDate = DateTime.Now;
                db.Insert(log);
            }
        }

    }
}
