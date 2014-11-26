using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.SQLModels;

namespace UniversalNomadUploader.SQLUtils
{
    public static class ServerUtil
    {
        /// <summary>
        /// Sets up the server variables
        /// these are static and should not change
        /// any change will done by the web team
        /// </summary>

        private const String DEVHOST = "localhost:14685";
        private const String UAT1HOST = "onefileuat1.onefile.co.uk";
        private const String UAT2HOST = "onefileuat2.onefile.co.uk";
        private const String BETAHOST = "www5.onefile.co.uk";
        private const String QAHOST = "www3.onefile.co.uk";
        private const String DEMOHOST = "www2.onefile.co.uk";
        private const String LIVEHOST = "live.onefile.co.uk";


        private const String DEVWS = "localhost:14689";
        private const String UAT1WS = "wsapiuat1.onefile.co.uk";
        private const String UAT2WS = "wsapiuat2.onefile.co.uk";
        private const String BETAWS = "wsapibeta.onefile.co.uk";
        private const String QAWS = "wsapi3.onefile.co.uk";
        private const String DEMOWS = "wsapi2.onefile.co.uk";
        private const String LIVEWS = "wsapi.onefile.co.uk";

        private const String DEVNAME = "DEV";
        private const String UAT1NAME = "UAT1";
        private const String UAT2NAME = "UAT2";
        private const String BETANAME = "BETA";
        private const String QANAME = "QA";
        private const String DEMONAME = "DEMO";
        private const String LIVENAME = "LIVE";

        private const String DEVKEY = "slh1";
        private const String UAT1KEY = "suat1";
        private const String UAT2KEY = "suat2";
        private const String BETAKEY = "sof5";
        private const String QAKEY = "sof3";
        private const String DEMOKEY = "sof2";
        private const String LIVEKEY = "sof1";


        public static void SetServers()
        {
            setLiveServer();
            setDemoServer();
            setQAServer();
            setBetaServer();
            setUAT1Server();
            setUAT2Server();
            setDEVServer();
        }

        private static void setDEVServer()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "slh1").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 7;
                    server.HostUrl = DEVHOST;
                    server.ServerKey = DEVKEY;
                    server.ServerName = DEVNAME;
                    server.WsUrl = DEVWS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 7,
                        HostUrl = DEVHOST,
                        ServerKey = DEVKEY,
                        ServerName = DEVNAME,
                        WsUrl = DEVWS
                    });
                }
            }
        }

        public static String getServerWSUrl(ServerEnum ServerID)
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                return db.Table<Server>().Where(srv => srv.ServerID == (int)ServerID).SingleOrDefault().WsUrl;
            }
        }

        private static void setUAT2Server()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "suat2").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 6;
                    server.HostUrl = UAT2HOST;
                    server.ServerKey = UAT2KEY;
                    server.ServerName = UAT2NAME;
                    server.WsUrl = UAT2WS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 6,
                        HostUrl = UAT2HOST,
                        ServerKey = UAT2KEY,
                        ServerName = UAT2NAME,
                        WsUrl = UAT2WS
                    });
                }
            }
        }

        private static void setUAT1Server()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "suat1").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 5;
                    server.HostUrl = UAT1HOST;
                    server.ServerKey = UAT1KEY;
                    server.ServerName = UAT1NAME;
                    server.WsUrl = UAT1WS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 5,
                        HostUrl = UAT1HOST,
                        ServerKey = UAT1KEY,
                        ServerName = UAT1NAME,
                        WsUrl = UAT1WS
                    });
                }
            }
        }

        private static void setBetaServer()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "sof5").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 4;
                    server.HostUrl = BETAHOST;
                    server.ServerKey = BETAKEY;
                    server.ServerName = BETANAME;
                    server.WsUrl = BETAWS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 4,
                        HostUrl = BETAHOST,
                        ServerKey = BETAKEY,
                        ServerName = BETANAME,
                        WsUrl = BETAWS
                    });
                }
            }
        }

        private static void setQAServer()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "sof3").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 3;
                    server.HostUrl = QAHOST;
                    server.ServerKey = QAKEY;
                    server.ServerName = QANAME;
                    server.WsUrl = QAWS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 3,
                        HostUrl = QAHOST,
                        ServerKey = QAKEY,
                        ServerName = QANAME,
                        WsUrl = QAWS
                    });
                }
            }
        }

        private static void setDemoServer()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "sof2").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 2;
                    server.HostUrl = DEMOHOST;
                    server.ServerKey = DEMOKEY;
                    server.ServerName = DEMONAME;
                    server.WsUrl = DEMOWS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 2,
                        HostUrl = DEMOHOST,
                        ServerKey = DEMOKEY,
                        ServerName = DEMONAME,
                        WsUrl = DEMOWS
                    });
                }
            }
        }

        private static void setLiveServer()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                Server server = db.Table<Server>().Where(srv => srv.ServerKey == "sof1").SingleOrDefault();
                if (server != null)
                {
                    server.ServerID = 1;
                    server.HostUrl = LIVEHOST;
                    server.ServerKey = LIVEKEY;
                    server.ServerName = LIVENAME;
                    server.WsUrl = LIVEWS;
                    int success = db.Update(server);
                }
                else
                {
                    int success = db.Insert(new Server()
                    {
                        ServerID = 1,
                        HostUrl = LIVEHOST,
                        ServerKey = LIVEKEY,
                        ServerName = LIVENAME,
                        WsUrl = LIVEWS
                    });
                }
            }
        }
    }
}
