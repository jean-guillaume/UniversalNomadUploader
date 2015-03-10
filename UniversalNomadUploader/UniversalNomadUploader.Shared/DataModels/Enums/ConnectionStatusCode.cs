using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.Common
{
    public enum connectionStatus
    {
        success,
        badPassword,
        badUsername,
        sqlError,
        authenticationFailed
    }    
}
