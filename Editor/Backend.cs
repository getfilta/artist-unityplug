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

        public static Backend Instance { get; }
        static Backend() {
            Instance = new Backend();
        }

        public EventHandler BundleQueue = delegate { };
        private const string RunLocallySetting = "Filta_RunLocally";
        public static bool RunLocally {
            get => EditorPrefs.GetBool(RunLocallySetting, false);
            set => EditorPrefs.SetBool(RunLocallySetting, value);
        }


        private static string PrivCollectionURL => $"https://firestore.googleapis.com/v1/projects/{(Global.UseTestEnvironment ? "filta-dev" : "filta-machina")}/databases/(default)/documents/priv_collection/{Authentication.Instance.Uid}";
        private static string TestFuncLocation => $"http://localhost:5001/{(Global.UseTestEnvironment ? "filta-dev" : "filta-machina")}/us-central1/";
        private static string FuncLocation => Global.UseTestEnvironment ? "https://us-central1-filta-dev.cloudfunctions.net/" : "https://us-central1-filta-machina.cloudfunctions.net/";
        private static string RtdbUrlbase => Global.UseTestEnvironment ? "https://filta-dev-default-rtdb.firebaseio.com" : "https://filta-machina.firebaseio.com";
        private static string UploadURL => RunLocally ? TestFuncLocation + "artist-uploadArtSource" : FuncLocation + "artist-uploadUnityPackage";
        private static string DeletePrivArtURL => ConstructUrl("artist-deletePrivArt");

        private static string ConstructUrl(string functionName) {
            return RunLocally ? TestFuncLocation + functionName : FuncLocation + functionName;
        }
        private const string ReleaseURL = "https://raw.githubusercontent.com/getfilta/artist-unityplug/main/releaseLogs.json";
        private const string RegistryURL = "https://registry.npmjs.org/com.getfilta.artist-unityplug";

        private EventSourceReader _evt = null;
        private string _currentBundle;

        public void ListenToQueue() {
            if (_evt == null) {
                _evt = new EventSourceReader(new Uri($"{RtdbUrlbase}/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{Authentication.Instance.Uid}\"")).Start();
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
            using UnityWebRequest www = UnityWebRequest.Post(UploadURL, postData);
            await www.SendWebRequest();
            Global.FireStatusChange(this, "Connected! Uploading... (3/5)");
            var response = www.downloadHandler.text;
            UploadBundleResponse parsed;
            if (!string.IsNullOrEmpty(www.error)) {
                Global.FireStatusChange(this, $"Error Uploading: {www.error}");
                Debug.LogError($"{response}:{www.error}");
                return null;
            }

            try {
                parsed = JsonUtility.FromJson<UploadBundleResponse>(response);
            } catch {
                Global.FireStatusChange(this, "Error! Check console for more information", true);
                Debug.LogError(response);
                return null;
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
            using UnityWebRequest www = UnityWebRequest.Post(DeletePrivArtURL, postData);
            await www.SendWebRequest();
            var response = www.downloadHandler.text;
            if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError) {
                Debug.LogError(www.error + " " + www.downloadHandler.text);
                return null;
            }

            return response;
        }

        public async Task<List<ReleaseInfo>> GetMasterReleaseInfo() {
            try {
                using UnityWebRequest req = UnityWebRequest.Get(RegistryURL);
                await req.SendWebRequest();
                JObject jsonResult = JObject.Parse(req.downloadHandler.text);
                JToken versions = jsonResult["versions"];
                if (versions == null) {
                    Global.FireStatusChange(this, "Could not find versions");
                    Debug.LogError("Could not find versions");
                    return null;
                }

                List<ReleaseInfo> releaseInfos = new List<ReleaseInfo>();
                foreach (JProperty prop in versions) {
                    JToken version = prop.Value;
                    JToken release = version["release"];
                    if (release == null) {
                        continue;
                    }
                    ReleaseInfo info = release.ToObject<ReleaseInfo>();
                    releaseInfos.Add(info);
                }
                return releaseInfos;
            } catch (Exception e) {
                Global.FireStatusChange(this, e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        public async Task<ArtsAndBundleStatus> GetArtsAndBundleStatus() {
            using UnityWebRequest req = UnityWebRequest.Get(PrivCollectionURL);
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
            }

            Global.FireStatusChange(this, "Error Deleting. Check console for details.", true);
            Debug.LogError("Request Result: " + req.result);
            return new ArtsAndBundleStatus();
        }

        public void LogToServer(LoggingLevel level, string title, string payload) {
            var entry = new TelemetryEntry() {
                title = title,
                payload = payload,
                level = level,
                source = TelemetrySource.Plugin,
                ts = 0
            };
            SendTelemetryRequest request = new() {
                entries = new[] { entry }
            };

            _ = CallFunction<SendTelemetryRequest, SendTelemetryResponse>("ops-sendTelemetry", request);
        }

        public void LogAnalyticsEvent(string eventName, params AnalyticsEventParam[] eventParams) {
            LogAnalyticsEventRequest request = new() {
                eventName = eventName,
                eventParams = eventParams
            };
            _ = CallFunction<LogAnalyticsEventRequest, LogAnalyticsEventResponse>("ops-logAnalyticsEvent", request, true);
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
                        case "version":
                            value.version = previewObject.Value<int>("integerValue");
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
                    Global.FireStatusChange(this, $"{artMeta.Value.title} v{artMeta.Value.version} successfully processed! (5/5)", false);
                    collection.RecentStatusUpdate = ArtsAndBundleStatus.StatusUpdate.Success;
                    EditorUtility.DisplayDialog("Success!", $"{artMeta.Value.title} v{artMeta.Value.version} successfully processed!", "Ok");
                } else if (artMeta.Value.artId == _currentBundle && artMeta.Value.bundleStatus == "error-bundling") {
                    _currentBundle = null;
                    Global.FireStatusChange(this, $"{artMeta.Value.title} : error processing :(", true);
                    EditorUtility.DisplayDialog("Something Went Wrong!", $"{artMeta.Value.title} v{artMeta.Value.version} failed to process!", "Ok");
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
                Debug.LogError(req.error.ToString());
                throw new Exception(req.error.ToString());
            }

            var jsonResult = JObject.Parse(req.downloadHandler.text);
            if (jsonResult["error"] != null) {
                Debug.LogError(req.error.ToString());
                throw new Exception(jsonResult["error"].ToString());
            }

            var result = JsonConvert.DeserializeObject<TResponse>(jsonResult["result"].ToString());
            return result;
        }

        public async Task<AskForRemoteLoginResponse> AskForRemoteLogin() {
            AskForRemoteLoginRequest request = new() { };
            var response = await CallFunction<AskForRemoteLoginRequest, AskForRemoteLoginResponse>("auth-askForRemoteLogin", request);
            return response;
        }

        public async Task<GetRemoteLoginAskStatusResponse> GetRemoteLoginAskStatus(string key) {
            GetRemoteLoginAskStatusRequest request = new() {
                statusKey = key
            };
            var response = await CallFunction<GetRemoteLoginAskStatusRequest, GetRemoteLoginAskStatusResponse>("auth-getRemoteLoginAskStatus", request);
            return response;
        }

        public async Task<GetPrivCollectionResponse> GetUserPrivCollection(string uid, string wallet) {
            Debug.Log($"{uid},{wallet}");
            GetPrivCollectionRequest request = new() { uid = uid, wallet = wallet };
            var response = await CallFunction<GetPrivCollectionRequest, GetPrivCollectionResponse>("artist-getPrivCollection", request, true);
            return response;
        }

        public async Task<GetPrivCollectionUnityPackageResponse> GetUserPrivUnityPackage(string artId) {
            GetPrivCollectionUnityPackageRequest request = new() { artId = artId };
            var response = await CallFunction<GetPrivCollectionUnityPackageRequest, GetPrivCollectionUnityPackageResponse>("artist-getPrivCollectionUnityPackage", request, true);
            return response;
        }

        public async Task<byte[]> GetUnityPackage(string url) {
            try {
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Content-Type", "application/octet-stream");
                await request.SendWebRequest();
                return request.downloadHandler.data;
            } catch (Exception e) {
                Debug.LogError($"Error downloading package. {e.Message}");
                throw new Exception(e.Message);
            }
        }

        public async Task<GetAccessResponse> GetAccess() {
            GetAccessRequest request = new() { };
            var response = await CallFunction<GetAccessRequest, GetAccessResponse>("artist-getAccess", request, true);
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

        public bool IsCompleted => asyncOp.isDone;

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
        public Version pluginVersion;
        public FilterType filterType;
        public bool resetOnRecord;
        public bool dynamicLightOn;
    }

    public class ScopedRegistry {
        public string name;
        public string url;
        public string[] scopes;
    }

    public class ManifestJson {
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();

        public List<ScopedRegistry> scopedRegistries = new List<ScopedRegistry>();
    }

    public class ReleaseInfo {
        public Version version;
        public string releaseNotes;
    }

    public class Version {
        public int pluginAppVersion;
        public int pluginMajorVersion;
        public int pluginMinorVersion;

        public int ToInt() {
            //since minor versions can exceed 10, increasing the weight of major version and app version.
            return (pluginAppVersion * 1000) + (pluginMajorVersion * 100) + (pluginMinorVersion);
        }
    }

    public enum LoggingLevel {
        NONE = 0,
        ERROR = 1,
        WARN = 2,
        LOG = 3,
        DEBUG = 4,
        VERBOSE = 5,
    }

    public static class TelemetrySource {
        public const string Plugin = "plugin";
    }

    // Matches packages/shared/src/infra/telemetry.ts
    [Serializable]
    public class TelemetryEntry {
        public string title;
        public string payload;
        public LoggingLevel level;
        public string source; // see TelemetrySource constants
        public long ts; // js timestamp
    }

    [Serializable]
    public class SendTelemetryRequest {
        public TelemetryEntry[] entries;
    }

    public class SendTelemetryResponse {
        public string result;
    }

    [Serializable]
    public class AnalyticsEventParam {
        public string name;
        public string value;
    }
    public class LogAnalyticsEventRequest {
        public string eventName;
        public AnalyticsEventParam[] eventParams;
    }
    public class LogAnalyticsEventResponse {
        public string result;
    }

    public class GetPrivCollectionRequest {
        public string uid;
        public string wallet;
    }
    public class GetPrivCollectionResponse {
        public ArtMeta[] collection;
    }

    public class GetPrivCollectionUnityPackageRequest {
        public string artId;
    }
    public class GetPrivCollectionUnityPackageResponse {
        public string signedUrl;
    }

    public class GetAccessRequest {
    }
    public class GetAccessResponse {
        public bool isAdmin;
    }
}