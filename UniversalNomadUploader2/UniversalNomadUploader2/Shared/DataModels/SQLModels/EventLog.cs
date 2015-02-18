using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.SQLModels
{
    public class EventLog
    {
        [PrimaryKey]
        [AutoIncrement]
        public int LocalID { get; set; }
        public int UserID { get; set; }
        public int ServerID { get; set; }
        public String EventDetails { get; set; }
        public int Type { get; set; }
        public DateTime EventDate { get; set; }
    }
}
