using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.FunctionalModels
{
    public class FunctionnalUser
    {

        public FunctionnalUser(APIModels.APIUser user)
        {
            this.UserID = user.UserID;
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
            this.OrganisationID = user.OrganisationID;
            this.MaximumUploadSize = user.MaximumUploadSize;
        }

        public FunctionnalUser(SQLModels.SQLUser dbuser)
        {
            this.UserID = dbuser.UserID;
            this.FirstName = dbuser.FirstName;
            this.LastName = dbuser.LastName;
            this.OrganisationID = dbuser.OrganisationID;
            this.LocalID = dbuser.LocalID;
            this.ServerID = dbuser.LocalID;
            this.SessionID = dbuser.SessionID;
            this.Username = dbuser.Username;
            this.MaximumUploadSize = dbuser.MaximumUploadSize;
        }

        public FunctionnalUser()
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
        public int MaximumUploadSize { get; set; }
    }
}
