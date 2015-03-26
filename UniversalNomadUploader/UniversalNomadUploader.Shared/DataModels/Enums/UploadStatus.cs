using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.Enums
{
    public enum UploadStatus
    {
        DBFailedToRecord, //Failed to access/record into the database
        FailedToUpload, //Upload failed for an unknown reason
        NoInternetConnection, //Not connected to Internet
        ServerError, //The server has returned an HTTP error >=400
        SizeExceeded, //The size of the evidence is too big
        SplitFail, //error from splitfile
        OK
    }
}
