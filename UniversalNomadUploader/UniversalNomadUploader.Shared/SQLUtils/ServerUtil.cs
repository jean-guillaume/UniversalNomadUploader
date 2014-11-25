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

    public static void SetServers()
    {
      setLiveServer();
      setDemoServer();
      setQAServer();
      setBetaServer();
      setUAT1Server();
      setUAT2Server();
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
          server.HostUrl = "onefileuat2.onefile.co.uk";
          server.ServerKey = "suat2";
          server.ServerName = "UAT2";
          server.WsUrl = "ignapiuat2.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 6,
            HostUrl = "onefileuat2.onefile.co.uk",
            ServerKey = "suat2",
            ServerName = "UAT2",
            WsUrl = "ignapiuat2.onefile.co.uk"
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
          server.HostUrl = "onefileuat1.onefile.co.uk";
          server.ServerKey = "suat1";
          server.ServerName = "UAT1";
          server.WsUrl = "ignapiuat1.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 5,
            HostUrl = "onefileuat1.onefile.co.uk",
            ServerKey = "suat1",
            ServerName = "UAT1",
            WsUrl = "ignapiuat1.onefile.co.uk"
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
          server.HostUrl = "www5.onefile.co.uk";
          server.ServerKey = "sof5";
          server.ServerName = "Beta";
          server.WsUrl = "ignapibeta.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 4,
            HostUrl = "www5.onefile.co.uk",
            ServerKey = "sof5",
            ServerName = "Beta",
            WsUrl = "ignapibeta.onefile.co.uk"
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
          server.HostUrl = "www3.onefile.co.uk";
          server.ServerKey = "sof3";
          server.ServerName = "QA";
          server.WsUrl = "ignapi3.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 3,
            HostUrl = "www3.onefile.co.uk",
            ServerKey = "sof3",
            ServerName = "QA",
            WsUrl = "ignapi3.onefile.co.uk"
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
          server.HostUrl = "www2.onefile.co.uk";
          server.ServerKey = "sof2";
          server.ServerName = "Demo";
          server.WsUrl = "ignapi2.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 2,
            HostUrl = "www2.onefile.co.uk",
            ServerKey = "sof2",
            ServerName = "Demo",
            WsUrl = "ignapi2.onefile.co.uk"
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
          server.HostUrl = "live.onefile.co.uk";
          server.ServerKey = "sof1";
          server.ServerName = "Live";
          server.WsUrl = "ignapi.onefile.co.uk";
          int success = db.Update(server);
        }
        else
        {
          int success = db.Insert(new Server()
          {
            ServerID = 1,
            HostUrl = "live.onefile.co.uk",
            ServerKey = "sof1",
            ServerName = "Live",
            WsUrl = "ignapi.onefile.co.uk"
          });
        }
      }
    }
  }
}
