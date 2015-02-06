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
        public const int SALTLENGTH = 32;
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
                    SetLastLoggedInUser(new User(dbuser));
                    dbuser.UserID = user.UserID;
                    dbuser.FirstName = user.FirstName;
                    dbuser.WasLastLogin = 1;
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
                HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512);
                CryptographicHash hash = provider.CreateHash();

                IBuffer salt = CryptographicBuffer.ConvertStringToBinary(Encoding.UTF8.GetString(CryptographicBuffer.GenerateRandom(32).ToArray(), 0, SALTLENGTH), BinaryStringEncoding.Utf8);
                hash.Append(salt);
                IBuffer password = CryptographicBuffer.ConvertStringToBinary(Pass, BinaryStringEncoding.Utf8);
                hash.Append(password);
                IBuffer hashedBuffer = hash.GetValueAndReset();


                UniversalNomadUploader.DataModels.SQLModels.User dbuser = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.Username == user.Username && usr.ServerID == (int)GlobalVariables.SelectedServer).SingleOrDefault();
                if (dbuser == null)
                {
                    UniversalNomadUploader.DataModels.SQLModels.User newUser = new UniversalNomadUploader.DataModels.SQLModels.User()
                    {
                        Username = user.Username,
                        SessionID = user.SessionID,
                        Password = CryptographicBuffer.EncodeToBase64String(hashedBuffer),
                        Salt = CryptographicBuffer.EncodeToBase64String(salt),
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

        public static Boolean AuthenticateOffline(String Username, String Password)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.User u = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.Username == Username && usr.ServerID == (int)GlobalVariables.SelectedServer).SingleOrDefault();
                if (u != null)
                {
                    HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512);

                    CryptographicHash hash = provider.CreateHash();

                    IBuffer salt = CryptographicBuffer.ConvertStringToBinary(Encoding.UTF8.GetString(Convert.FromBase64String(u.Salt), 0, Convert.FromBase64String(u.Salt).Length), BinaryStringEncoding.Utf8);
                    hash.Append(salt);
                    IBuffer password = CryptographicBuffer.ConvertStringToBinary(Password, BinaryStringEncoding.Utf8);
                    hash.Append(password);
                    IBuffer hashedBuffer = hash.GetValueAndReset();
                    String EnteredPass = CryptographicBuffer.EncodeToBase64String(hashedBuffer);
                    if (EnteredPass == u.Password)
                    {
                        SetLastLoggedInUser(new User(u));
                        GlobalVariables.LoggedInUser = new User(u);
                        return true;
                    }
                }
                return false;
            }
        }

        public static void SetLastLoggedInUser(User us)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                var users = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(o => o.LocalID > 0);
                foreach (var item in users)
                { 
                    item.WasLastLogin = 0;
                    db.Update(item);
                }
                DataModels.SQLModels.User u = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.LocalID == us.LocalID).SingleOrDefault();
                if (u != null)
                {
                    u.WasLastLogin = 1;
                    int test = db.Update(u);
                }
            }
        }

        public static User GetLastLoggedInUser()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                DataModels.SQLModels.User u = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.WasLastLogin == 1).SingleOrDefault();
                if (u != null)
                {
                        return new User(u);
                }
                return null;
            }
        }
    }
}
