using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using Windows.Networking.Connectivity;

namespace UniversalNomadUploader.Common
{
    public static class GlobalVariables
    {
        private static ServerEnum _SelectedServer = ServerEnum.Live;
        public static ServerEnum SelectedServer { get { return _SelectedServer; } set { _SelectedServer = value; } }
        public static String dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.Current.Resources["DatabaseName"].ToString());
        private static User _LoggedInUser = null;
        public static User LoggedInUser
        {
            get
            {
                if (_LoggedInUser == null)
                {
                    _LoggedInUser =  SQLUtils.UserUtil.GetLastLoggedInUser();
                    _SelectedServer = (ServerEnum)_LoggedInUser.ServerID;
                }
                return _LoggedInUser;
            }
            set
            {
                _LoggedInUser = value;
            }
        }
        public static bool IsOffline { get; set; }

        public static bool HasInternetAccess()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = (connections != null) && (connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
            return internet;
        }

        public static Dictionary<String, MimeTypes> ValidExtensions()
        {
            Dictionary<String, MimeTypes> extensions = new Dictionary<String, MimeTypes>();
            extensions.Add(".3ga", MimeTypes.Audio);
            extensions.Add(".3gp", MimeTypes.Movie);
            extensions.Add(".3gpp", MimeTypes.Movie);
            extensions.Add(".aac", MimeTypes.Audio);
            extensions.Add(".accdb", MimeTypes.Database);
            extensions.Add(".aiff", MimeTypes.Audio);
            extensions.Add(".amr", MimeTypes.Audio);
            extensions.Add(".asf", MimeTypes.Movie);
            extensions.Add(".avi", MimeTypes.Movie);
            extensions.Add(".awb", MimeTypes.Audio);
            extensions.Add(".bmp", MimeTypes.Picture);
            extensions.Add(".caf", MimeTypes.Audio);
            extensions.Add(".csv", MimeTypes.Spreadsheet);
            extensions.Add(".doc", MimeTypes.Word);
            extensions.Add(".docm", MimeTypes.Word);
            extensions.Add(".docx", MimeTypes.Word);
            extensions.Add(".dot", MimeTypes.Word);
            extensions.Add(".dotm", MimeTypes.Word);
            extensions.Add(".dotx", MimeTypes.Word);
            extensions.Add(".dss", MimeTypes.Audio);
            extensions.Add(".dwg", MimeTypes.Picture);
            extensions.Add(".flv", MimeTypes.Movie);
            extensions.Add(".gif", MimeTypes.Picture);
            extensions.Add(".ics", MimeTypes.Text);
            extensions.Add(".jp2", MimeTypes.Picture);
            extensions.Add(".jpeg", MimeTypes.Picture);
            extensions.Add(".jpg", MimeTypes.Picture);
            extensions.Add(".m4a", MimeTypes.Audio);
            extensions.Add(".mdb", MimeTypes.Database);
            extensions.Add(".mdi", MimeTypes.Picture);
            extensions.Add(".mov", MimeTypes.Movie);
            extensions.Add(".mp3", MimeTypes.Audio);
            extensions.Add(".mp4", MimeTypes.Movie);
            extensions.Add(".mpeg", MimeTypes.Movie);
            extensions.Add(".mpg", MimeTypes.Movie);
            extensions.Add(".mpg4", MimeTypes.Movie);
            extensions.Add(".mpp", MimeTypes.Publisher);
            extensions.Add(".msg", MimeTypes.Text);
            extensions.Add(".notes", MimeTypes.Picture);
            extensions.Add(".odp", MimeTypes.PowerPoint);
            extensions.Add(".odt", MimeTypes.Text);
            extensions.Add(".pages", MimeTypes.Text);
            extensions.Add(".pdf", MimeTypes.PDF);
            extensions.Add(".png", MimeTypes.Picture);
            extensions.Add(".pot", MimeTypes.PowerPoint);
            extensions.Add(".potm", MimeTypes.PowerPoint);
            extensions.Add(".potx", MimeTypes.PowerPoint);
            extensions.Add(".ppam", MimeTypes.PowerPoint);
            extensions.Add(".pps", MimeTypes.PowerPoint);
            extensions.Add(".ppsm", MimeTypes.PowerPoint);
            extensions.Add(".ppsx", MimeTypes.PowerPoint);
            extensions.Add(".ppt", MimeTypes.PowerPoint);
            extensions.Add(".pptm", MimeTypes.PowerPoint);
            extensions.Add(".pptx", MimeTypes.PowerPoint);
            extensions.Add(".psd", MimeTypes.Picture);
            extensions.Add(".pub", MimeTypes.Publisher);
            extensions.Add(".rar", MimeTypes.CompressedFolder);
            extensions.Add(".rtf", MimeTypes.Word);
            extensions.Add(".swf", MimeTypes.Movie);
            extensions.Add(".tif", MimeTypes.Picture);
            extensions.Add(".tiff", MimeTypes.Picture);
            extensions.Add(".tsv", MimeTypes.Text);
            extensions.Add(".txt", MimeTypes.Text);
            extensions.Add(".vsd", MimeTypes.Picture);
            extensions.Add(".wav", MimeTypes.Audio);
            extensions.Add(".wma", MimeTypes.Audio);
            extensions.Add(".wmv", MimeTypes.Movie);
            extensions.Add(".wp3", MimeTypes.Audio);
            extensions.Add(".wpf", MimeTypes.Corel);
            extensions.Add(".wps", MimeTypes.Word);
            extensions.Add(".xlam", MimeTypes.Spreadsheet);
            extensions.Add(".xls", MimeTypes.Spreadsheet);
            extensions.Add(".xlsb", MimeTypes.Spreadsheet);
            extensions.Add(".xlsm", MimeTypes.Spreadsheet);
            extensions.Add(".xlsx", MimeTypes.Spreadsheet);
            extensions.Add(".xlt", MimeTypes.Spreadsheet);
            extensions.Add(".xltm", MimeTypes.Spreadsheet);
            extensions.Add(".xltx", MimeTypes.Spreadsheet);
            extensions.Add(".xps", MimeTypes.Text);
            extensions.Add(".zip", MimeTypes.CompressedFolder);
            return extensions;
        }

        public static MimeTypes GetMimeTypeFromExtension(string p)
        {
            MimeTypes type;
            if (ValidExtensions().TryGetValue(p, out type))
            {
                return type;
            }
            else
            {
                return MimeTypes.Unknown;
            }
        }
    }
}
