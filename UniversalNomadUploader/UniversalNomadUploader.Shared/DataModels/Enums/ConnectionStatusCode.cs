using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.Common
{
    public enum connectionStatus
    {
        Success,
        BadPassword,
        BadUsername,
        SqlError,
        AuthenticationFailed
    }    
}
