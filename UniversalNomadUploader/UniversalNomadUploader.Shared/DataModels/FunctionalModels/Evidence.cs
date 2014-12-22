using System;
using System.Collections.Generic;
using System.Text;
using UniversalNomadUploader.DataModels.Enums;

namespace UniversalNomadUploader.DataModels.FunctionalModels
{
    public class Evidence
    {
        public Evidence()
        {

        }

        public Evidence(SQLModels.Evidence e)
        {
            this.LocalID = e.LocalID;
            this.UserID = e.UserID;
            this.ServerID = e.ServerID;
            this.FileName = e.FileName;
            this.Extension = e.Extension;
            this.Size = e.Size;
            this.CreatedDate = e.CreatedDate;
            this.HasTryUploaded = e.TriedUpload;
            this.UploadedDate = e.UploadedDate;
            this.UploadError = e.UploadError;
            this.Name = e.Name;
            this.Type = (MimeTypes)e.Type;
        }

        public int LocalID { get; set; }
        public int UserID { get; set; }
        public int ServerID { get; set; }
        public String FileName { get; set; }
        public String Extension { get; set; }
        public String Name { get; set; }
        public Double Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public Uri ImagePath
        {
            get
            {
                switch (Type)
                {
                    case MimeTypes.Audio:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Audio.png");
                    case MimeTypes.CompressedFolder:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Zip.png");
                    case MimeTypes.Corel:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Corel.png");
                    case MimeTypes.Database:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Database.png");
                    case MimeTypes.Movie:
                        return new System.Uri(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "/_VidThumbs/" + FileName + ".jpg");
                    case MimeTypes.PDF:
                        return new System.Uri("ms-appx:///Assets/FileIcons/PDF.png");
                    case MimeTypes.Picture:
                        return new System.Uri(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "/" + FileName + "." + Extension );
                    case MimeTypes.PowerPoint:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Powerpoint.png");
                    case MimeTypes.Publisher:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Publisher.png");
                    case MimeTypes.Spreadsheet:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Spreadsheet.png");
                    case MimeTypes.Text:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Text.png");
                    case MimeTypes.Word:
                        return new System.Uri("ms-appx:///Assets/FileIcons/Word.png");
                    case MimeTypes.Unknown:
                        return new System.Uri("ms-appx:///Assets/FileIcons/File.png");
                    default:
                        return new System.Uri("ms-appx:///Assets/FileIcons/File.png");
                }
            }
        }
        
        public Uri UploadStatus
        {
            get
            {
                switch (HasTryUploaded)
                {
                    case true:
                        if (UploadedDate > DateTime.MinValue)
                            return new System.Uri("ms-appx:///Assets/StatusIcons/Online.png");
                        else
                            return new System.Uri("ms-appx:///Assets/StatusIcons/Warning.png");
                    case false:
                        return new System.Uri("ms-appx:///Assets/StatusIcons/Offline.png");
                    default:
                        return new System.Uri("ms-appx:///Assets/StatusIcons/Offline.png");
                }
            }
        }

        public bool HasTryUploaded { get; set; }
        public DateTime UploadedDate { get; set; }
        public String UploadError { get; set; }
        public MimeTypes Type { get; set; }
        public bool IsUploading { get; set; }
    }
}
