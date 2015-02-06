using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.APIModels
{
    public class User
    {
        public int UserID { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public int OrganisationID { get; set; }
    }
}
