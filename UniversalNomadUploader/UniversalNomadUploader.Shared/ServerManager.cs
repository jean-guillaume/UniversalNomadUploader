using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.Exceptions;
using UniversalNomadUploader.SQLUtils;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Notifications;

namespace UniversalNomadUploader
{
    /// <summary>
    /// This class handle all the access to the server request. Every code communicating with the server should be handled by this class.
    /// </summary>
    public class ServerManager
    {
        String m_userName;
        String m_password;
        CancellationTokenSource m_cts;

        public ServerManager(String _userName, String _password)
        {
            m_userName = _userName;
            m_password = _password;
            m_cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Authenticate to the server
        /// </summary>
        /// <returns>Status code:
        /// >=0 : success
        /// -1 : username/password IsNullOrWhiteSpace
        /// -2 : Adding user in sql db failed
        /// -3 : Incorrect username or password</returns>
        public async Task<connectionStatus> Connect()
        {
            if (String.IsNullOrWhiteSpace(m_userName))
            {
                return connectionStatus.BadUsername;
            }

            if (String.IsNullOrWhiteSpace(m_password))
            {
                return connectionStatus.BadPassword;
            }

            Boolean HasAuthed = false;
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["Username"] = m_userName;

            if (GlobalVariables.HasInternetAccess())
            {
                Guid Session = await Authenticate(GlobalVariables.SelectedServer);
                if (Session != Guid.Empty)
                {
                    String ErrorMessage = String.Empty;
                    try
                    {
                        await UniversalNomadUploader.SQLUtils.SQLUserUtil.InsertUser(new FunctionnalUser() { Username = m_userName, SessionID = Session }, m_password);
                        await UniversalNomadUploader.SQLUtils.SQLUserUtil.UpdateUser(await GetProfile());
                    }
                    catch (ApiException)
                    {
                        return connectionStatus.SqlError;
                    }

                    GlobalVariables.IsOffline = false;
                    HasAuthed = true;
                }
            }
            else
            {
                if (SQLUtils.SQLUserUtil.AuthenticateOffline(m_userName, m_password))
                {
                    GlobalVariables.IsOffline = true;
                    HasAuthed = true;
                }
            }
            if (!HasAuthed)
            {
                return connectionStatus.AuthenticationFailed;
            }

            return 0;
        }

        public async Task<bool> VerifySessionAsync()
        {
            Guid SessionID = await SQLUtils.SQLUserUtil.GetSessionID();
            String WSUrl = SQLServerUtil.getServerWSUrl();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-SessionID", SessionID.ToString());
                String url = ((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/Authentication/VerifySession";
                try
                {
                    using (var response = await client.GetAsync(url))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task<Guid> Authenticate(ServerEnum ServerID)
        {
            String WSUrl = SQLServerUtil.getServerWSUrl();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Username", m_userName);
                client.DefaultRequestHeaders.Add("X-Password", m_password);
                String url = ((ServerID == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/Authentication/MobileAuthenticate";
                var content = new StringContent("");
                try
                {
                    using (var response = await client.PostAsync(url, content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            String data = await response.Content.ReadAsStringAsync();
                            data = data.Replace("\"", "");
                            if (!String.IsNullOrWhiteSpace(data))
                            {
                                Guid result;
                                Guid.TryParse(data, out result);
                                return result;
                            }
                        }
                        return Guid.Empty;
                    }
                }
                catch (Exception)
                {
                    return Guid.Empty;
                }
            }
        }

        public async Task<FunctionnalUser> GetProfile()
        {
            Guid SessionID = await SQLUtils.SQLUserUtil.GetSessionID();
            String WSUrl = SQLServerUtil.getServerWSUrl();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-SessionID", SessionID.ToString());
                String url = ((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + WSUrl + "/User/MobileGetProfile/";
                try
                {
                    using (var response = await client.GetAsync(url))
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new ApiException((ApiResponseCodes)(-10));
                        }
                        if (response.ReasonPhrase == "OK" || Convert.ToInt32(response.ReasonPhrase) == 0)
                        {
                            String data = await response.Content.ReadAsStringAsync();
                            return new FunctionnalUser(JsonConvert.DeserializeObject<DataModels.APIModels.APIUser>(data));
                        }
                        else
                        {
                            throw new ApiException((ApiResponseCodes)Convert.ToInt32(response.ReasonPhrase));
                        }
                    }
                }
                catch (Exception)
                {
                    throw new ApiException(ApiResponseCodes.Unknown);
                }
            }
        }

        /// <summary>
        /// Initialize and upload an Evidence to the server
        /// </summary>
        /// <param name="_evi">Evidence to upload</param>
        /// <returns></returns>
        public async Task<UploadStatus> UploadEvidence(FunctionnalEvidence _evi)
        {
            StorageFile uploadFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(_evi.FileName + "." + _evi.Extension);

            if (_evi.Size > (GlobalVariables.LoggedInUser.MaximumUploadSize * 1024 * 1024) && GlobalVariables.LoggedInUser.MaximumUploadSize >0)
            {
                StorageFolder partFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(_evi.Name, CreationCollisionOption.ReplaceExisting);

                List<StorageFile> partFile = await Helper.SplitFile(uploadFile, (int)((GlobalVariables.LoggedInUser.MaximumUploadSize * 1024.0 * 1024.0) * 0.9), partFolder);
                String TransferID = null;
                int numberOfTry = 0;

                for (int i = 0; i < partFile.Count; i++)
                {
                    BackgroundUploader uploader = new BackgroundUploader();
                    uploader.SetRequestHeader("X-FileName", (_evi.Name == null) ? "" : _evi.Name);
                    uploader.SetRequestHeader("ContentType", uploadFile.ContentType);
                    uploader.SetRequestHeader("X-Extension", uploadFile.FileType.Replace(".", ""));
                    uploader.SetRequestHeader("X-SessionID", GlobalVariables.LoggedInUser.SessionID.ToString());

                    if (TransferID != null)
                    {
                        uploader.SetRequestHeader("X-TransferID", TransferID);
                    }

                    uploader.SetRequestHeader("X-TotalSize", _evi.Size.ToString());
                    uploader.SetRequestHeader("X-BlockID", i.ToString());
                    uploader.SetRequestHeader("X-TotalBlockNumber", partFile.Count.ToString());

                    UploadOperation upload = uploader.CreateUpload(new Uri(((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + "localhost:14688/User/MobileUploadLargeEvidence"), partFile[i]);
                    await HandleUploadAsync(upload, true, _evi);

                    ResponseInformation response = upload.GetResponseInformation();

                    if(response == null)
                    {
                        _evi.HasTryUploaded = true;
                        _evi.UploadError = "Unable to contact the server";
                        SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                        SQLEventLogUtil.InsertEvent(_evi.Name + " uploaded failed, reason: " + "Evidence " + _evi.Name + " part " + i + "failed to be uploaded, unable to contact the server.", LogType.Upload);
                        return UploadStatus.ServerError;
                    }

                    if (response.StatusCode >= 400)
                    {
                        _evi.HasTryUploaded = true;
                        _evi.UploadError = "Server error during the upload of the splitted file";
                        SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                        SQLEventLogUtil.InsertEvent(_evi.Name + " uploaded failed, reason: " + "Evidence " + _evi.Name + " part " + i + "failed to be uploaded, unable to contact the server.", LogType.Upload);
                        return UploadStatus.ServerError;
                    }

                    if (TransferID == null)
                    {
                        TransferID = response.Headers["X-TransferID"];
                    }

                    Boolean success = false;
                    Boolean.TryParse(response.Headers["X-Success"], out success);

                    //if it fails it try again once
                    if ((success == false && numberOfTry == 0))
                    {
                        numberOfTry++; //next failed will result in an upload failed
                        i--; //permits to redo the current iteration
                    }
                    else if (success == false && numberOfTry > 0)
                    {
                        _evi.HasTryUploaded = true;
                        _evi.UploadError = "One part of the splitted evidence failed to be uploaded";
                        SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                        SQLEventLogUtil.InsertEvent(_evi.Name + " uploaded failed, reason: " + "Evidence " + _evi.Name + " part " + i + "failed to be uploaded after a second try.", LogType.Upload);
                        return UploadStatus.SplitFail;
                    }
                    else
                    {
                        numberOfTry = 0;
                    }

                }
                
                StorageFolder eviPart = await ApplicationData.Current.LocalFolder.GetFolderAsync(_evi.Name);
                await eviPart.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            else
            {
                BackgroundUploader uploader = new BackgroundUploader();
                uploader.SetRequestHeader("X-FileName", (_evi.Name == null) ? "" : _evi.Name);
                uploader.SetRequestHeader("ContentType", uploadFile.ContentType);
                uploader.SetRequestHeader("X-Extension", uploadFile.FileType.Replace(".", ""));
                uploader.SetRequestHeader("X-SessionID", GlobalVariables.LoggedInUser.SessionID.ToString());

                UploadOperation upload = uploader.CreateUpload(new Uri(((GlobalVariables.SelectedServer == ServerEnum.DEV) ? "http://" : "https://") + SQLServerUtil.getServerWSUrl() + "/User/MobileUploadEvidence"), uploadFile);
                await HandleUploadAsync(upload, true, _evi);
            }

            return UploadStatus.OK;
        }

        /// <summary>
        /// Upload the evidence to the server
        /// </summary>
        /// <param name="_upload">upload operation</param>
        /// <param name="_start">Upload already runned before or not</param>
        /// <param name="_evi">Evidence to upload</param>
        /// <returns></returns>
        private async Task<UploadStatus> HandleUploadAsync(UploadOperation _upload, bool _start, FunctionnalEvidence _evi)
        {
            try
            {
                if (_start)
                {
                    // Start the upload 
                    await _upload.StartAsync().AsTask(m_cts.Token);
                }
                else
                {
                    // The upload was already running when the application started, re-attach the progress handler.
                    await _upload.AttachAsync().AsTask(m_cts.Token);
                }

                _evi.HasTryUploaded = true;
                _evi.UploadedDate = DateTime.Now;

                SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                await SQLEventLogUtil.InsertEventAsync(_evi.Name + " uploaded successfully", LogType.Upload);
            }
            catch (TaskCanceledException)
            {
                _evi.HasTryUploaded = true;
                _evi.UploadError = "Upload was cancelled (Task cancellation)";
                SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                SQLEventLogUtil.InsertEvent(_evi.Name + " uploaded cancelled", LogType.Upload);
            }
            catch (Exception ex)
            {
                _evi.HasTryUploaded = true;
                _evi.UploadError = ex.Message;
                SQLEvidenceUtil.UpdateEvidenceSyncStatus(_evi);
                SQLEventLogUtil.InsertEvent(_evi.Name + " uploaded failed, reason: " + ex.Message + Environment.NewLine + Environment.NewLine + "Stack trace: " + Environment.NewLine + ex.StackTrace, LogType.Upload);
                return UploadStatus.FailedToUpload;
            }

            return UploadStatus.OK;
        }

        /// <summary>
        /// Cancel the uploading of the evidence in progress
        /// </summary>
        public void CancelProcessing()
        {
            m_cts.Cancel();
        }
    }
}
