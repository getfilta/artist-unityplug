using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using EvtSource;
using Newtonsoft.Json;
using Filta.Datatypes;
using Newtonsoft.Json.Linq;

namespace Filta {
    public class Backend {

        public static Backend Instance { get; private set; }
        static Backend() {
            Instance = new Backend();
        }

        public EventHandler BundleQueue = delegate { };
        private const string RunLocallySetting = "Filta_RunLocally";
        public bool RunLocally {
            get { return EditorPrefs.GetBool(RunLocallySetting, false); }
            set { EditorPrefs.SetBool(RunLocallySetting, value); }
        }


        private string PRIV_COLLECTION_URL { get { return $"https://firestore.googleapis.com/v1/projects/{(Global.UseTestEnvironment ? "filta-dev" : "filta-machina")}/databases/(default)/documents/priv_collection/{Authentication.Instance.Uid}"; } }
        private string TEST_FUNC_LOCATION { get { return $"http://localhost:5001/{(Global.UseTestEnvironment ? "filta-dev" : "filta-machina")}/us-central1/"; } }
        private string FUNC_LOCATION { get { return Global.UseTestEnvironment ? "https://us-central1-filta-dev.cloudfunctions.net/" : "https://us-central1-filta-machina.cloudfunctions.net/"; } }
        private string RTDB_URLBASE { get { return Global.UseTestEnvironment ? "https://filta-dev-default-rtdb.firebaseio.com" : "https://filta-machina.firebaseio.com"; } }
        private string UPLOAD_URL { get { return RunLocally ? TEST_FUNC_LOCATION + "uploadArtSource" : FUNC_LOCATION + "uploadUnityPackage"; } }
        private string DELETE_PRIV_ART_URL { get { return ConstructUrl("deletePrivArt"); } }
        private string ConstructUrl(string functionName) {
            return RunLocally ? TEST_FUNC_LOCATION + functionName : FUNC_LOCATION + functionName;
        }
        private const string releaseURL = "https://raw.githubusercontent.com/getfilta/artist-unityplug/main/releaseLogs.json";


        private EventSourceReader _evt = null;
        private string _currentBundle;

        public void ListenToQueue() {
            if (_evt == null) {
                _evt = new EventSourceReader(new Uri($"{RTDB_URLBASE}/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{Authentication.Instance.Uid}\"")).Start();
                _evt.MessageReceived += (sender, e) => {
                    if (e.Event != "keep-alive") {
                        BundleQueue(this, EventArgs.Empty);
                    }
                };
                _evt.Disconnected += async (sender, e) => {
                    await Task.Delay(e.ReconnectDelay);
                    try {
                        if (!_evt.IsDisposed) {
                            _evt.Start(); // Reconnect to the same URL
                        }
                    } catch (Exception exception) {
                        Debug.LogError(exception.Message);
                    }

                };
            }
        }
        public void DisposeQueue() {
            _evt?.Dispose();
            _evt = null;
        }

        public async Task<string> Upload(string selectedArtKey, string selectedArtTitle, Hash128 hash, byte[] bytes) {
            _currentBundle = null;
            WWWForm postData = new();
            if (selectedArtKey != "temp") {
                Debug.LogWarning("Updating Art with artid: " + selectedArtKey);
                postData.AddField("artid", selectedArtKey);
            }
            postData.AddField("uid", Authentication.Instance.LoginToken);
            postData.AddField("hash", hash.ToString());
            postData.AddField("title", selectedArtTitle);
            using UnityWebRequest www = UnityWebRequest.Post(UPLOAD_URL, postData);
            await www.SendWebRequest();
            Global.FireStatusChange(this, "Connected! Uploading... (3/5)");
            var response = www.downloadHandler.text;
            UploadBundleResponse parsed;
            if (!string.IsNullOrEmpty(www.error)) {
                Global.FireStatusChange(this, $"Error Uploading: {www.error}");
                Debug.LogError($"{response}:{www.error}");
                return null;
            } else {
                try {
                    parsed = JsonUtility.FromJson<UploadBundleResponse>(response);
                } catch {
                    Global.FireStatusChange(this, "Error! Check console for more information", true);
                    Debug.LogError(response);
                    return null;
                }
            }
            using UnityWebRequest upload = UnityWebRequest.Put(parsed.url, bytes);
            await upload.SendWebRequest();
            if (!string.IsNullOrEmpty(upload.error)) {
                Global.FireStatusChange(this, $"Error Uploading: {upload.error}");
                Debug.LogError($"{upload.downloadHandler.text}:{upload.error}");
                return null;
            }
            _currentBundle = parsed.artid;
            return parsed.artid;
        }

        public async Task<string> DeletePrivateArt(string artId) {
            WWWForm postData = new();
            postData.AddField("uid", Authentication.Instance.LoginToken);
            postData.AddField("artid", artId);
            using UnityWebRequest www = UnityWebRequest.Post(DELETE_PRIV_ART_URL, postData);
            await www.SendWebRequest();
            var response = www.downloadHandler.text;
            if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError) {
                Debug.LogError(www.error + " " + www.downloadHandler.text);
                return null;
            } else {
                return response;
            }
        }

        public async Task<List<ReleaseInfo>> GetMasterReleaseInfo() {
            try {
                using UnityWebRequest req = UnityWebRequest.Get(releaseURL);
                await req.SendWebRequest();
                return JsonConvert.DeserializeObject<List<ReleaseInfo>>(req.downloadHandler.text);
            } catch (Exception e) {
                Debug.LogError(e.Message);
                return new();
            }
        }


        public async Task<ArtsAndBundleStatus> GetArtsAndBundleStatus() {
            using UnityWebRequest req = UnityWebRequest.Get(PRIV_COLLECTION_URL);
            req.SetRequestHeader("authorization", $"Bearer {Authentication.Instance.LoginToken}");
            await req.SendWebRequest();
            if (req.responseCode == 404) {
                Debug.LogWarning("No uploads found. User could be new or service is down.");
                Global.FireStatusChange(this, "No uploads found", true);
                return new ArtsAndBundleStatus();
            }
            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError) {
                throw new Exception(req.error.ToString());
            }
            if (req.downloadHandler != null) {
                var jsonResult = JObject.Parse(req.downloadHandler.text);
                var result = new ArtsAndBundleStatus();
                ParseArtMetas(jsonResult, result);
                GetActiveBundles(result);
                return result;
            } else {
                Global.FireStatusChange(this, "Error Deleting. Check console for details.", true);
                Debug.LogError("Request Result: " + req.result);
                return new ArtsAndBundleStatus();
            }
        }

        private void ParseArtMetas(JObject json, ArtsAndBundleStatus collection) {
            var fields = json["fields"];
            if (fields == null) {
                return;
            }
            if (fields.Type != JTokenType.Object) {
                return;
            }

            foreach (var artMetaJson in fields.Children<JProperty>()) {
                string name = artMetaJson.Name;
                ArtMeta value = new() {
                    artId = name
                };
                foreach (var field in artMetaJson.Value["mapValue"]["fields"].Children()) {
                    var fieldName = field.Value<JProperty>().Name;
                    var previewObject = field.Value<JProperty>().Value as JObject;
                    switch (fieldName) {
                        case "artist":
                            value.artist = previewObject.Value<string>("stringValue");
                            break;
                        case "creationTime":
                            value.creationTime = previewObject.Value<string>("integerValue");
                            break;
                        case "title":
                            value.title = previewObject.Value<string>("stringValue");
                            break;
                        case "preview":
                            value.preview = previewObject.Value<string>("stringValue");
                            break;
                        case "bundleQueuePosition":
                            value.bundleQueuePosition = previewObject.Value<int>("integerValue");
                            break;
                        case "bundleStatus":
                            value.bundleStatus = previewObject.Value<string>("stringValue");
                            break;
                        case "lastUpdated":
                            value.lastUpdated = previewObject.Value<Int64>("integerValue");
                            break;
                    }
                }

                collection.ArtMetas.Add(name, value);
            }
        }

        private void GetActiveBundles(ArtsAndBundleStatus collection) {
            // analyze the artmetas with their bundle status and produce list of bundles, 
            // plus any recent victories or defeats
            collection.RecentStatusUpdate = ArtsAndBundleStatus.StatusUpdate.None;
            foreach (var artMeta in collection.ArtMetas) {
                if (artMeta.Value.bundleStatus == "need-package" ||
                    artMeta.Value.bundleStatus == "package" ||
                    artMeta.Value.bundleStatus == "bundling") {
                    collection.Bundles.Add(artMeta.Key, new Bundle() {
                        title = artMeta.Value.title,
                        bundleQueuePosition = artMeta.Value.bundleQueuePosition,
                        bundleStatus = artMeta.Value.bundleStatus,
                        lastUpdated = artMeta.Value.lastUpdated
                    });
                } else if (artMeta.Value.artId == _currentBundle && artMeta.Value.bundleStatus == "bundled") {
                    _currentBundle = null;
                    Global.FireStatusChange(this, $"{artMeta.Value.title} successfully processed! (5/5)", false);
                    collection.RecentStatusUpdate = ArtsAndBundleStatus.StatusUpdate.Success;
                } else if (artMeta.Value.artId == _currentBundle && artMeta.Value.bundleStatus == "error-bundling") {
                    _currentBundle = null;
                    Global.FireStatusChange(this, $"{artMeta.Value.title} : error processing :(", true);
                    collection.RecentStatusUpdate = ArtsAndBundleStatus.StatusUpdate.Error;
                }
            }
        }

        private async Task<TResponse> CallFunction<TRequest, TResponse>(string requestName, TRequest request, bool useAuth = false) {
            var url = ConstructUrl(requestName);
            var postData = $"{{\"data\":{JsonUtility.ToJson(request)}}}";
            using UnityWebRequest req = new(url, "POST"); // UnityWebRequest.Post(url, postData);
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(postData);
            req.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (useAuth) {
                req.SetRequestHeader("authorization", $"Bearer {Authentication.Instance.LoginToken}");
            }
            await req.SendWebRequest();
            if (req.responseCode != 200) {
                throw new Exception(req.error.ToString());
            }

            var jsonResult = JObject.Parse(req.downloadHandler.text);
            if (jsonResult["error"] != null) {
                throw new Exception(jsonResult["error"].ToString());
            }

            var result = JsonConvert.DeserializeObject<TResponse>(jsonResult["result"].ToString());
            return result;
        }

        public async Task<AskForRemoteLoginResponse> AskForRemoteLogin() {
            AskForRemoteLoginRequest request = new() { };
            var response = await CallFunction<AskForRemoteLoginRequest, AskForRemoteLoginResponse>("askForRemoteLogin", request);
            return response;
        }

        public async Task<GetRemoteLoginAskStatusResponse> GetRemoteLoginAskStatus(string key) {
            GetRemoteLoginAskStatusRequest request = new() {
                statusKey = key
            };
            var response = await CallFunction<GetRemoteLoginAskStatusRequest, GetRemoteLoginAskStatusResponse>("getRemoteLoginAskStatus", request);
            return response;
        }
    }


    public class FunctionCallData<T> {
        public T data;
    }
    public class ArtsAndBundleStatus {
        public enum StatusUpdate { Success, Error, None }

        public Dictionary<string, ArtMeta> ArtMetas { get; } = new();
        public Dictionary<string, Bundle> Bundles { get; } = new();
        public StatusUpdate RecentStatusUpdate { get; set; } = StatusUpdate.None;
    }

    public class Bundle {
        public string title;
        public int bundleQueuePosition;
        public string bundleStatus;
        public Int64 lastUpdated;
    }

    public class AskForRemoteLoginRequest { }
    public class AskForRemoteLoginResponse {
        public string pin;
        public string statusKey;
        public string url;
    }

    public class GetRemoteLoginAskStatusRequest {
        public string statusKey;
    }
    public class GetRemoteLoginAskStatusResponse {
        public string status;
        public string token;
    }

    public class UnityWebRequestAwaiter : INotifyCompletion {
        private readonly UnityWebRequestAsyncOperation asyncOp;
        private Action continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp) {
            this.asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        public bool IsCompleted { get { return asyncOp.isDone; } }

        public void GetResult() { }

        public void OnCompleted(Action continuation) {
            this.continuation = continuation;
        }

        private void OnRequestCompleted(AsyncOperation obj) {
            continuation();
        }
    }

    public static class ExtensionMethods {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp) {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }

    [Serializable]
    public class UploadBundleResponse {
        public string url;
        public string artid;
    }

    public struct PluginInfo {
        public enum FilterType { Face, Body, Fusion }
        public int version;
        public FilterType filterType;
        public bool resetOnRecord;
    }

    public class ReleaseInfo {
        public Version version;
        public string releaseNotes;
        public class Version {
            public int pluginAppVersion;
            public int pluginMajorVersion;
            public int pluginMinorVersion;

            public int ToInt() {
                return (pluginAppVersion * 100) + (pluginMajorVersion * 10) + (pluginMinorVersion);
            }
        }
    }
}