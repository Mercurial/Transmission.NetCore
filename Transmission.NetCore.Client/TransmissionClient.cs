using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Transmission.NetCore.Client.Models;

namespace Transmission.NetCore.Client
{
    public class TransmissionClient
    {
        public string Host { get; private set; }
        public string SessionID { get; private set; }
        public int CurrentTag { get; private set; }

        private string _authorization;
        private bool _needAuthorization;

        /// <summary>
		/// Initialize client
		/// </summary>
		/// <param name="host">Host adresse</param>
		/// <param name="sessionID">Session ID</param>
		/// <param name="login">Login</param>
		/// <param name="password">Password</param>
		public TransmissionClient(string host, string sessionID = null, string login = null, string password = null)
        {
            this.Host = host;
            this.SessionID = sessionID;

            if (!String.IsNullOrWhiteSpace(login))
            {
                var authBytes = Encoding.UTF8.GetBytes(login + ":" + password);
                var encoded = Convert.ToBase64String(authBytes);

                this._authorization = String.Format("Basic {0}", encoded);
                this._needAuthorization = true;
            }
        }

        #region Session

        /// <summary>
        /// Close current session (API: session-close)
        /// </summary>
        public async void SessionCloseAsync()
        {
            var request = new TransmissionRequest("session-close", null);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Set information to current session (API: session-set)
        /// </summary>
        /// <param name="settings">New session settings</param>
        public async void SessionSetAsync(SessionSettings settings)
        {
            var request = new TransmissionRequest("session-set", settings.ToDictionary());
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Get session statistic
        /// </summary>
        /// <returns>Session stat</returns>
        public async Task<Statistic> SessionGetStatisticAsync()
        {
            var request = new TransmissionRequest("session-stats", null);
            var response = await SendRequestAsync(request);
            var result = response.Deserialize<Statistic>();
            return result;
        }

        /// <summary>
        /// Get information of current session (API: session-get)
        /// </summary>
        /// <returns>Session information</returns>
        public async Task<SessionInformation> SessionGetAsync()
        {
            var request = new TransmissionRequest("session-get", null);
            var response = await SendRequestAsync(request);
            var result = response.Deserialize<SessionInformation>();
            return result;
        }

        #endregion

        #region Torrents
        /// <summary>
        /// Add torrent (API: torrent-add)
        /// </summary>
        /// <returns>Torrent info (ID, Name and HashString)</returns>
        public async Task<CreatedTorrent> TorrentAddAsync(NewTorrent torrent)
        {
            if (String.IsNullOrWhiteSpace(torrent.Metainfo) && String.IsNullOrWhiteSpace(torrent.Filename))
                throw new Exception("Either \"filename\" or \"metainfo\" must be included.");

            var request = new TransmissionRequest("torrent-add", torrent.ToDictionary());
            var response = await SendRequestAsync(request);
            var jObject = response.Deserialize<JObject>();

            if (jObject == null || jObject.First == null)
                return null;

            CreatedTorrent result = null;

            if (jObject.TryGetValue("torrent-duplicate", out JToken value))
                result = JsonConvert.DeserializeObject<CreatedTorrent>(value.ToString());
            else if (jObject.TryGetValue("torrent-added", out value))
                result = JsonConvert.DeserializeObject<CreatedTorrent>(value.ToString());

            return result;
        }

        /// <summary>
        /// Set torrent params (API: torrent-set)
        /// </summary>
        /// <param name="torrentSet">New torrent params</param>
        public async void TorrentSetAsync(TorrentSettings settings)
        {
            var request = new TransmissionRequest("torrent-set", settings.ToDictionary());
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Get fields of torrents from ids (API: torrent-get)
        /// </summary>
        /// <param name="fields">Fields of torrents</param>
        /// <param name="ids">IDs of torrents (null or empty for get all torrents). int values for get by ID, string values for get by hash</param>
        /// <returns>Torrents info</returns>
        public async Task<Torrents> TorrentGetAsync(string[] fields, params object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "fields", fields }
            };

            if (ids != null && ids.Length > 0)
                arguments.Add("ids", ids);

            var request = new TransmissionRequest("torrent-get", arguments);

            var response = await SendRequestAsync(request);
            var result = response.Deserialize<Torrents>();

            return result;
        }

        /// <summary>
        /// Remove torrents
        /// </summary>
        /// <param name="ids">Torrents id</param>
        /// <param name="deleteLocalData">Remove local data</param>
        public async void TorrentRemoveAsync(object[] ids, bool deleteData = false)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids },
                { "delete-local-data", deleteData }
            };

            var request = new TransmissionRequest("torrent-remove", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Start torrents (API: torrent-start)
        /// </summary>
        /// <param name="ids">Torrents id</param>
        public async void TorrentStartAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("torrent-start", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Start now torrents (API: torrent-start-now)
        /// </summary>
        /// <param name="ids">Torrents id</param>
        public async void TorrentStartNowAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("torrent-start-now", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Stop torrents (API: torrent-stop)
        /// </summary>
        /// <param name="ids">Torrents id</param>
        public async void TorrentStopAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("torrent-stop", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Verify torrents (API: torrent-verify)
        /// </summary>
        /// <param name="ids">Torrents id</param>
        public async void TorrentVerifyAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("torrent-verify", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Move torrents in queue on top (API: queue-move-top)
        /// </summary>
        /// <param name="ids">Torrents id</param>
        public async void TorrentQueueMoveTopAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("queue-move-top", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Move up torrents in queue (API: queue-move-up)
        /// </summary>
        /// <param name="ids"></param>
        public async void TorrentQueueMoveUpAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("queue-move-up", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Move down torrents in queue (API: queue-move-down)
        /// </summary>
        /// <param name="ids"></param>
        public async void TorrentQueueMoveDownAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("queue-move-down", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Move torrents to bottom in queue  (API: queue-move-bottom)
        /// </summary>
        /// <param name="ids"></param>
        public async void TorrentQueueMoveBottomAsync(object[] ids)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids }
            };

            var request = new TransmissionRequest("queue-move-bottom", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Set new location for torrents files (API: torrent-set-location)
        /// </summary>
        /// <param name="ids">Torrent ids</param>
        /// <param name="location">The new torrent location</param>
        /// <param name="move">Move from previous location</param>
        public async void TorrentSetLocationAsync(object[] ids, string location, bool move)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", ids },
                { "location", location },
                { "move", move }
            };
            var request = new TransmissionRequest("torrent-set-location", arguments);
            var response = await SendRequestAsync(request);
        }

        /// <summary>
        /// Rename a file or directory in a torrent (API: torrent-rename-path)
        /// </summary>
        /// <param name="ids">The torrent whose path will be renamed</param>
        /// <param name="path">The path to the file or folder that will be renamed</param>
        /// <param name="name">The file or folder's new name</param>
        public async Task<RenamedTorrent> TorrentRenamePathAsync(object id, string path, string name)
        {
            var arguments = new Dictionary<string, object>
            {
                { "ids", new object[] { id } },
                { "path", path },
                { "name", name }
            };

            var request = new TransmissionRequest("torrent-rename-path", arguments);
            var response = await SendRequestAsync(request);

            var result = response.Deserialize<RenamedTorrent>();

            return result;
        }
        #endregion

        #region System

        /// <summary>
        /// See if your incoming peer port is accessible from the outside world (API: port-test)
        /// </summary>
        /// <returns>Accessible state</returns>
        public async Task<bool> PortTestAsync()
        {
            var request = new TransmissionRequest("port-test", null);
            var response = await SendRequestAsync(request);

            var data = response.Deserialize<JObject>();
            var result = (bool)data.GetValue("port-is-open");
            return result;
        }

        #endregion

        private async Task<TransmissionResponse> SendRequestAsync(TransmissionRequest request)
        {
            TransmissionResponse result = new TransmissionResponse();

            request.Tag = ++CurrentTag;

            try
            {

                byte[] byteArray = Encoding.UTF8.GetBytes(request.ToJson());

                //Prepare http web request
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Host);

                webRequest.ContentType = "application/json-rpc";
                //webRequest.Headers.Add("X-Transmission-Session-Id:" + SessionID);
                webRequest.Headers["X-Transmission-Session-Id"] = SessionID;
                webRequest.Method = "POST";
                //webRequest.ContentLength = byteArray.Length;

                if (_needAuthorization)
                    webRequest.Headers["Authorization"] = _authorization;

                using (Stream dataStream = await webRequest.GetRequestStreamAsync())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                //Send request and prepare response
                using (var webResponse = await webRequest.GetResponseAsync())
                {
                    using (Stream responseStream = webResponse.GetResponseStream())
                    {
                        var reader = new StreamReader(responseStream, Encoding.UTF8);
                        var responseString = reader.ReadToEnd();
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<TransmissionResponse>(responseString);

                        if (result.Result != "success")
                            throw new Exception(result.Result);
                    }
                }
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Conflict)
                {
                    if (ex.Response.Headers.AllKeys.Length > 0)
                    {
                        //If session id expiried, try get session id and send request
                        SessionID = ex.Response.Headers["X-Transmission-Session-Id"];

                        if (SessionID == null)
                            throw new Exception("Session ID Error");

                        result = await SendRequestAsync(request);
                    }
                }
                else
                    throw ex;
            }

            return result;
        }
    }
}
