using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace UniversalNomadUploader.Common
{
    class UploadDateFlyoutConverter// : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value != null && (DateTime)value > DateTime.MinValue)
            {
                return "Uploaded on: " + Environment.NewLine + ((DateTime)value).ToString("dddd MMMM yyyy HH:mm tt");
            }
            return "";

        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}
