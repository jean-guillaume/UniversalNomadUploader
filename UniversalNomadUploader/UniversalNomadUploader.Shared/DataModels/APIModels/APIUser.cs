using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.APIModels
{
    public class APIUser
    {
        public int UserID { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public int OrganisationID { get; set; }
        public int MaximumUploadSize { get; set; }

    }
}
