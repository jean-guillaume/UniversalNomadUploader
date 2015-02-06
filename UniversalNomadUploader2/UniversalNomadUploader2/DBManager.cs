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
using Windows.Storage;

namespace UniversalNomadUploader
{
    public class DBManager
    {
        String m_dbName;
        String m_dbFullPath;

        public DBManager(String _dbName)
        {
            m_dbName = _dbName;
            this.initialize();
        }

        public async Task<bool> DoesDbExist(string DatabaseName)
        {
            bool dbexist = true;
            try
            {
                StorageFile storageFile = await ApplicationData.Current.LocalFolder.GetFileAsync(DatabaseName);
            }
            catch
            {
                dbexist = false;
            }

            return dbexist;
        }

        public async void initialize()
        {
            bool isExisting = await DoesDbExist(m_dbName);
            m_dbFullPath = ApplicationData.Current.LocalFolder.Path + "\\" + m_dbName;

            if (isExisting == false)
            {
                CreateDatabase();
            }
        }

        public void CreateDatabase()
        {
            SQLiteConnection create = new SQLiteConnection(m_dbFullPath);
            create.CreateTable<SQLEvidence>();
        }

        public int AddEvidence(String _fileName, String _extension, DateTime _createdDate, int _serverID, String _name, MimeTypes _mimeType)
        {
            Evidence evi = new Evidence(_fileName, _extension, _createdDate, _serverID, _name, _mimeType);
            SQLEvidence sqlEvi = new SQLEvidence(evi);

            using (var db = new SQLiteConnection(m_dbFullPath))
            {
                return db.Insert(sqlEvi);
            }
        }

        public IEnumerable<IGrouping<String, Evidence>> readAllEvidence()
        {
            using (var db = new SQLiteConnection(m_dbFullPath))
            {
               IEnumerable<Evidence> rawFunctionnalEvidenceList = ToFunctionalEvidence(db.Table<SQLEvidence>().Where(usr => usr.ServerID == 0));

               return rawFunctionnalEvidenceList
                                        .GroupBy(x => x.Name.Substring(0,1))
                                        .OrderBy(x => x.Key);                
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
