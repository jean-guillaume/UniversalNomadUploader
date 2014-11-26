using System;
using System.Collections.Generic;
using System.Text;
using UniversalNomadUploader.DataModels.Enums;

namespace UniversalNomadUploader.Exceptions
{
    public class ApiException : Exception
    {
        public override string Message
        {
            get
            {
                switch (_errorMessage)
                {
                    case ApiResponseCodes.LearnerExistsWithPICSID:
                        return "Learner already exists with PicsID";
                    case ApiResponseCodes.APINotEnabledForOrganisation:
                        return "Api not enabled for centre";
                    case ApiResponseCodes.FormDoesntExist:
                        return "Form does not exist";
                    case ApiResponseCodes.LearnerNotAssignedForm:
                        return "Learner is not assigned to the form";
                    case ApiResponseCodes.XMLFormNotValid:
                        return "XML is not valid for the form structure";
                    case ApiResponseCodes.NoSession:
                        return "No session id";
                    case ApiResponseCodes.IPRestrictedError:
                        return "IP is restricted";
                    case ApiResponseCodes.SessionExpired:
                        return "Session has expired";
                    case ApiResponseCodes.InvalidSessionID:
                        return "Invalid session id";
                    case ApiResponseCodes.Success:
                        return "Success";
                    default:
                        return "Response from server unknown";
                };
            }
        }

        private ApiResponseCodes _errorMessage = ApiResponseCodes.Success;
        public ApiException(ApiResponseCodes errocode)
        {
            _errorMessage = errocode;
        }
    }
}
