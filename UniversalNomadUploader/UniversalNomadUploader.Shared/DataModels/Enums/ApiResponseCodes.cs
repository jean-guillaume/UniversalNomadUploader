using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.DataModels.Enums
{
    public enum ApiResponseCodes
    {
        LearnerExistsWithPICSID = -9,
        APINotEnabledForOrganisation = -8,
        FormDoesntExist = -7,
        LearnerNotAssignedForm = -6,
        XMLFormNotValid = -5,
        NoSession = -4,
        IPRestrictedError = -3,
        SessionExpired = -2,
        InvalidSessionID = -1,
        Success = 0,
    }
}
