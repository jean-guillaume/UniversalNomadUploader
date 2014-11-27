using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.FunctionalModels;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

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

    public static async Task InsertUser(User user, String Pass)
    {
      await Task.Run(() => PrivateInsertUser(user, Pass));
    }

    private static void PrivateInsertUser(User user, String Pass)
    {
      using (var db = new SQLiteConnection(GlobalVariables.dbPath))
      {
        // Use Password Based Key Derivation Function 2 (PBKDF2 or RFC2898)
        KeyDerivationAlgorithmProvider pbkdf2 = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha512);

        // Do not store passwords in strings if you can avoid them. The
        // password may be retained in memory until it is garbage collected.
        // Crashing the application and looking at the memory dump may 
        // reveal it.
        IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(Pass, BinaryStringEncoding.Utf8);
        CryptographicKey key = pbkdf2.CreateKey(passwordBuffer);

        // Use random salt and 10,000 iterations. Store the salt along with 
        // the derviedBytes (see below).
        IBuffer salt = CryptographicBuffer.GenerateRandom(32);
        KeyDerivationParameters parameters = KeyDerivationParameters.BuildForPbkdf2(salt, 10000);

        // Store the returned 32 bytes along with the salt for later verification
        byte[] derviedBytes = CryptographicEngine.DeriveKeyMaterial(key, parameters, 32).ToArray();

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
