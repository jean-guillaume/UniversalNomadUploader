using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.SQLModels
{
  public class SQLUser
  {
    [PrimaryKey]
    [AutoIncrement]
    public int LocalID { get; set; }
    public int UserID { get; set; }
    public int ServerID { get; set; }
    public int OrganisationID { get; set; }
    public String FirstName { get; set; }
    public String LastName { get; set; }
    public Guid SessionID { get; set; }
    public String Password { get; set; }
    public String Username { get; set; }
    public String Salt { get; set; }
    public int WasLastLogin { get; set; }
  }
}
