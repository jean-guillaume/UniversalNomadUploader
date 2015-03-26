using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.Enums
{
    public enum EvidenceStatus
    {
        BadEvidenceName, //Bad name entered by the user
        BadFileName, //The file name generation failed
        MaximumSizeFileExceeded, //The file is too big
        OK
    }
}
