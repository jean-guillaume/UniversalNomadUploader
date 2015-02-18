using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using UniversalNomadUploader.DataModels.Enums;

namespace UniversalNomadUploader.DataModels.SQLModels
{
    public class SQLEvidence
    {
        public SQLEvidence()
        {
        }

        public SQLEvidence(FunctionalModels.FunctionnalEvidence e)
        {
            this.LocalID = e.LocalID;
            this.UserID = e.UserID;
            this.ServerID = e.ServerID;
            this.FileName = e.FileName;
            this.Extension = e.Extension;
            this.Size = e.Size;
            this.CreatedDate = e.CreatedDate;
            this.UploadError = e.UploadError;
            this.UploadedDate = e.UploadedDate;
            this.TriedUpload = e.HasTryUploaded;
            this.Name = (e.Name == null) ? "" : e.Name;
            this.Type = (int)e.Type;
        }

        [PrimaryKey]
        [AutoIncrement]
        public int LocalID { get; set; }
        public DateTime CreatedDate { get; set; }
        public String Extension { get; set; }
        public String FileName { get; set; }        
        public String Name { get; set; }
        public int ServerID { get; set; }
        public Double Size { get; set; }        
        public bool TriedUpload { get; set; }
        public int Type { get; set; }
        public DateTime UploadedDate { get; set; }
        public String UploadError { get; set; }
        public int UserID { get; set; }
        
    }
}
