using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalNomadUploader.DataModels.SQLModels
{
  public class SQLServer
  {
    [PrimaryKey]
    public int ServerID { get; set; }
    public String HostUrl { get; set; }
    public String ServerName { get; set; }
    public String ServerKey { get; set; }
    public String WsUrl { get; set; }
  }
}
