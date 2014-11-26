using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.SQLModels
{
    public class Evidence
    {
        public Evidence()
        {
        }

        public Evidence(FunctionalModels.Evidence e)
        {
            this.LocalID = e.LocalID;
            this.UserID = e.UserID;
            this.ServerID = e.ServerID;
            this.FileName = e.FileName;
            this.Extension = e.Extension;
            this.Size = e.Size;
            this.CreatedDate = e.CreatedDate;
            this.Name = e.Name;
        }

        [PrimaryKey]
        [AutoIncrement]
        public int LocalID { get; set; }
        public int UserID { get; set; }
        public int ServerID { get; set; }
        public String FileName { get; set; }
        public String Name { get; set; }
        public String Extension { get; set; }
        public Double Size { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
