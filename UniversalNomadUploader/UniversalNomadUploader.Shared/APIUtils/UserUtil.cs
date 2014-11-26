using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.Exceptions;
using UniversalNomadUploader.SQLUtils;

namespace UniversalNomadUploader.APIUtils
{
    public class UserUtil
    {
        public static async Task<User> GetProfile()
        {
            Guid SessionID = await SQLUtils.UserUtil.GetSessionID();
            String WSUrl = ServerUtil.getServerWSUrl(GlobalVariables.SelectedServer);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-SessionID", SessionID.ToString());
                String url = ((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/User/GetProfile/";
                using (var response = await client.GetAsync(url))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new ApiException((ApiResponseCodes)(-10));
                    }
                    if (response.ReasonPhrase == "OK" || Convert.ToInt32(response.ReasonPhrase) == 0)
                    {
                        String data = await response.Content.ReadAsStringAsync();
                        return new User(JsonConvert.DeserializeObject<DataModels.APIModels.User>(data));
                    }
                    else
                    {
                        throw new ApiException((ApiResponseCodes)Convert.ToInt32(response.ReasonPhrase));
                    }
                }
            }
        }
    }
}
