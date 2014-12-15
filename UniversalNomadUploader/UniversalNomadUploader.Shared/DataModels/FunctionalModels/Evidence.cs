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
                        return new System.Uri("ms-appx:///Assets/AudioPreview.png");
                    case MimeTypes.CompressedFolder:
                        return new System.Uri("ms-appx:///Assets/ZipPreview.png");
                    case MimeTypes.Corel:
                        return new System.Uri("ms-appx:///Assets/CorelPreview.png");
                    case MimeTypes.Database:
                        return new System.Uri("ms-appx:///Assets/DatabasePreview.png");
                    case MimeTypes.Movie:
                        return new System.Uri("ms-appx:///Assets/VideoPreview.png");
                    case MimeTypes.PDF:
                        return new System.Uri("ms-appx:///Assets/PDFPreview.png");
                    case MimeTypes.Picture:
                        return new System.Uri("ms-appx:///Assets/ImagePreview.png");
                    case MimeTypes.PowerPoint:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                    case MimeTypes.Publisher:
                        return new System.Uri("ms-appx:///Assets/PublisherPreview.png");
                    case MimeTypes.Spreadsheet:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                    case MimeTypes.Text:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                    case MimeTypes.Word:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                    case MimeTypes.Unknown:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                    default:
                        return new System.Uri("ms-appx:///Assets/GeneralPreview.png");
                }
            }
        }
        public Uri UploadSymbol
        {
            get
            {
                switch (Type)
                {
                    case MimeTypes.Audio:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Audio.png");
                    case MimeTypes.CompressedFolder:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Zip.png");
                    case MimeTypes.Corel:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Corel.png");
                    case MimeTypes.Database:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Database.png");
                    case MimeTypes.Movie:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Video.png");
                    case MimeTypes.PDF:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/PDF.png");
                    case MimeTypes.Picture:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Image.png");
                    case MimeTypes.PowerPoint:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Powerpoint.png");
                    case MimeTypes.Publisher:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Publisher.png");
                    case MimeTypes.Spreadsheet:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Spreadsheet.png");
                    case MimeTypes.Text:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Text.png");
                    case MimeTypes.Word:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Word.png");
                    case MimeTypes.Unknown:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Generic.png");
                    default:
                        return new System.Uri("ms-appx:///Assets/TinyIcons/Generic.png");
                }
            }
        }

        public bool HasTryUploaded { get; set; }
        public DateTime UploadedDate { get; set; }
        public String UploadError { get; set; }
        public MimeTypes Type { get; set; }
    }
}
