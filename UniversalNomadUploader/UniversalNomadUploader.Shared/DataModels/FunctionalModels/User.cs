using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.FunctionalModels
{
  public class User
  {
    
    public User(SQLModels.User dbuser)
    {
      this.UserID = dbuser.UserID;
      this.FirstName = dbuser.FirstName;
      this.LastName = dbuser.LastName;
      this.OrganisationID = dbuser.OrganisationID;
      this.LocalID = dbuser.LocalID;
      this.ServerID = dbuser.LocalID;
      this.SessionID = dbuser.SessionID;
      this.Username = dbuser.Username;
    }

    public User()
    {
    }

    public int LocalID { get; set; }
    public Guid SessionID { get; set; }
    public String Username { get; set; }
    public int ServerID { get; set; }
    public int UserID { get; set; }
    public String FirstName { get; set; }
    public String LastName { get; set; }
    public int OrganisationID { get; set; }
  }
}
