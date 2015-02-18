using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.DataModels.SQLModels;
using UniversalNomadUploader.SQLUtils;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

namespace UniversalNomadUploader
{    

    public class DBManager
    {
        const int SALTLENGTH = 32;
        static String dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.Current.Resources["DatabaseName"].ToString());

        public DBManager() { }

        public static void CreateTables()
        {
            using (var db = new SQLiteConnection(DBManager.dbPath))
            {
                db.CreateTable<Server>();
                db.CreateTable<UniversalNomadUploader.DataModels.SQLModels.User>();
                db.CreateTable<SQLEvidence>();
                db.CreateTable<EventLog>();
            }
        }

        public int AddEvidence(String _fileName, String _extension, DateTime _createdDate, int _serverID, String _name, MimeTypes _mimeType)
        {
            Evidence evi = new Evidence(_fileName, _extension, _createdDate, _serverID, _name, _mimeType);
            SQLEvidence sqlEvi = new SQLEvidence(evi);

            using (var db = new SQLiteConnection(dbPath))
            {
                return db.Insert(sqlEvi);
            }
        }

        public IEnumerable<IGrouping<String, Evidence>> readAllEvidence()
        {
            using (var db = new SQLiteConnection(dbPath))
            {
               return ToFunctionalEvidence(db.Table<SQLEvidence>().Where(usr => usr.ServerID == 0)).ToList().GroupBy(x => x.Name.Substring(0,1)).ToList().OrderBy(x => x.Key).ToList();

               //return rawFunctionnalEvidenceList
               //                         .GroupBy(x => x.Name.Substring(0,1))
               //                         .OrderBy(x => x.Key);                
            }
        }

        public static IEnumerable<DataModels.FunctionalModels.Evidence> ToFunctionalEvidence(IEnumerable<SQLEvidence> Evs)
        {
            List<DataModels.FunctionalModels.Evidence> e = new List<DataModels.FunctionalModels.Evidence>();
            foreach (var item in Evs)
            {
                e.Add(ToFunctionalEvidence(item));
            }
            return e;
        }

        public static DataModels.FunctionalModels.Evidence ToFunctionalEvidence(SQLEvidence item)
        {
            return new DataModels.FunctionalModels.Evidence(item);
        }

        public String getServerWSURLfromDB()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return db.Table<Server>().Where(srv => srv.ServerID == (int)GlobalVariables.SelectedServer).SingleOrDefault().WsUrl;
            }
        }

        public async Task UpdateUser(UniversalNomadUploader.DataModels.FunctionalModels.User user)
        {
            await Task.Run(() => PrivateUpdateUser(user));
        }

        private void PrivateUpdateUser(UniversalNomadUploader.DataModels.FunctionalModels.User user)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                UniversalNomadUploader.DataModels.SQLModels.User dbuser = db.Table<UniversalNomadUploader.DataModels.SQLModels.User>().Where(usr => usr.LocalID == GlobalVariables.LoggedInUser.LocalID).SingleOrDefault();
                if (dbuser != null)
                {
                    UserUtil.SetLastLoggedInUser(new UniversalNomadUploader.DataModels.FunctionalModels.User(dbuser));
                    dbuser.UserID = user.UserID;
                    dbuser.FirstName = user.FirstName;
                    dbuser.WasLastLogin = 1;
                    dbuser.LastName = user.LastName;
                    dbuser.OrganisationID = user.OrganisationID;
                    int success = db.Update(dbuser);
                    GlobalVariables.LoggedInUser = new UniversalNomadUploader.DataModels.FunctionalModels.User(dbuser);
                }
            }
        }

        public async Task InsertUser(UniversalNomadUploader.DataModels.FunctionalModels.User user, String Pass)
        {
            await Task.Run(() => PrivateInsertUser(user, Pass));
        }

        private void PrivateInsertUser(UniversalNomadUploader.DataModels.FunctionalModels.User user, String Pass)
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
                    GlobalVariables.LoggedInUser = new UniversalNomadUploader.DataModels.FunctionalModels.User(newUser);
                }
                else
                {
                    dbuser.SessionID = user.SessionID;
                    int success = db.Update(dbuser);
                    GlobalVariables.LoggedInUser = new UniversalNomadUploader.DataModels.FunctionalModels.User(dbuser);
                }
            }
        }



        /*public async void add()
        {
            await m_connection.CreateTableAsync<SQLEvidence>();
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-1), 0, "1name21", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-1), 0, "1name2", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-2), 0, "1name3", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-1), 0, "1name4", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-3), 0, "1name5", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-3), 0, "1name6", MimeTypes.Picture);
            this.AddEvidence("2cf8f5e9-4781-487e-a955-4d40284248a9", "jpg", DateTime.Today.AddDays(-2), 0, "1name7", MimeTypes.Picture);
        }*/
    }
}
