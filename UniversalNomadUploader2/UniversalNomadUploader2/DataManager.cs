using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.DataModels.FunctionalModels;

namespace UniversalNomadUploader
{
    public class DataManager
    {
        private DBManager m_DBManager;
        private ServerManager m_ServerManager;
        private CaptureEvidence m_CaptureEvidence;

        public DBManager dbManager
        {
            get { return m_DBManager; }
            set { m_DBManager = value; }
        }

        public ServerManager serverManager
        {
            get { return m_ServerManager; }
            set { m_ServerManager = value; }
        }

        public CaptureEvidence captureEvidence
        {
            get { return m_CaptureEvidence; }
            set { m_CaptureEvidence = value; }
        }

        public DataManager()
        {
            m_CaptureEvidence = new CaptureEvidence();
        }

        public async Task<Guid> authenticateToServer(String _username, String _password)
        {
            Guid Session = await m_ServerManager.Authenticate(_username, _password, GlobalVariables.SelectedServer, m_DBManager.getServerWSURLfromDB());

            await m_DBManager.InsertUser(new User() { Username = _username.ToUpper(), SessionID = Session }, _password);
            await m_DBManager.UpdateUser(await APIUtils.UserUtil.GetProfile());

            return Session;

        }
    }
}
