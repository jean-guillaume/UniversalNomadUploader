using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;

namespace UniversalNomadUploader.Common
{
  public class GlobalVariables
  {
    private static ServerEnum _SelectedServer = ServerEnum.Live;
    public static ServerEnum SelectedServer { get { return _SelectedServer; } set { _SelectedServer = value; } }
    public static String dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.Current.Resources["DatabaseName"].ToString());
    public static User LoggedInUser = null;
  }
}
