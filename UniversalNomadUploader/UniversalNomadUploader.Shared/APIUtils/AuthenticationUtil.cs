using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.SQLUtils;

namespace UniversalNomadUploader.APIUtils
{
    public class AuthenticationUtil
    {
        public static async Task<Guid> Authenticate(String UserName, String Password, ServerEnum ServerID)
        {
            String WSUrl = ServerUtil.getServerWSUrl();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Username", UserName);
                client.DefaultRequestHeaders.Add("X-Password", Password);
                String url = ((ServerID == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/Authentication/MobileAuthenticate";
                var content = new StringContent("");
                using (var response = await client.PostAsync(url, content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        String data = await response.Content.ReadAsStringAsync();
                        data = data.Replace("\"", "");
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            return Guid.Parse(data);
                        }    
                    }
                    return Guid.Empty;
                }
            }
        }

        public async static Task<bool> VerifySessionAsync()
        {
            Guid SessionID = await SQLUtils.UserUtil.GetSessionID();
            String WSUrl = ServerUtil.getServerWSUrl();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-SessionID", SessionID.ToString());
                String url = ((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/Authentication/VerifySession";
                using (var response = await client.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
