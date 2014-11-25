using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.FunctionalModels;

namespace UniversalNomadUploader.SQLUtils
{
  public class UserUtil
  {
    public static async Task<Guid> GetSessionID()
    {
      return await Task.Run(
        () => PrivateGetSessionID()
      );
    }

    private static Guid PrivateGetSessionID()
    {
      using (var db = new SQLiteConnection(GlobalVariables.dbPath))
      {
        UniversalNomadUploader.DataModels.SQLModels.User user = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.LocalID == GlobalVariables.LoggedInUser.LocalID).SingleOrDefault();
        if (user != null)
        {
          return user.SessionID;
        }
        return Guid.Empty;
      }
    }

    public async static Task<String> GetFirstName()
    {
      return await Task.Run(() => PrivateGetFirstName());
    }

    private static string PrivateGetFirstName()
    {
      using (var db = new SQLiteConnection(GlobalVariables.dbPath))
      {
        UniversalNomadUploader.DataModels.SQLModels.User dbuser = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.LocalID == GlobalVariables.LoggedInUser.LocalID).SingleOrDefault();
        if (dbuser != null)
          return dbuser.FirstName;
        else
          return "";
      }
    }

    public static async Task UpdateUser(User user)
    {
      await Task.Run(() => PrivateUpdateUser(user));
    }

    private static void PrivateUpdateUser(User user)
    {
      using (var db = new SQLiteConnection(GlobalVariables.dbPath))
      {
        UniversalNomadUploader.DataModels.SQLModels.User dbuser = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.LocalID == GlobalVariables.LoggedInUser.LocalID).SingleOrDefault();
        if (dbuser != null)
        {
          dbuser.UserID = user.UserID;
          dbuser.FirstName = user.FirstName;
          dbuser.LastName = user.LastName;
          dbuser.OrganisationID = user.OrganisationID;
          int success = db.Update(dbuser);
          GlobalVariables.LoggedInUser = new User(dbuser);
        }
      }
    }

    public static async Task InsertUser(User user)
    {
      await Task.Run(() => PrivateInsertUser(user));
    }

    private static void PrivateInsertUser(User user)
    {
      using (var db = new SQLiteConnection(GlobalVariables.dbPath))
      {
        UniversalNomadUploader.DataModels.SQLModels.User dbuser = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.Username == user.Username).SingleOrDefault();
        if (dbuser == null)
        {
          UniversalNomadUploader.DataModels.SQLModels.User newUser = new UniversalNomadUploader.DataModels.SQLModels.User()
          {
            Username = user.Username,
            SessionID = user.SessionID,
            ServerID = (int)GlobalVariables.SelectedServer
          };
          int success = db.Insert(newUser);
          GlobalVariables.LoggedInUser = new User(newUser);
        }
        else
        {
          dbuser.SessionID = user.SessionID;
          int success = db.Update(dbuser);
          GlobalVariables.LoggedInUser = new User(dbuser);
        }
      }
    }
  }
}
