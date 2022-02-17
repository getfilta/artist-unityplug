﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using EvtSource;
using Newtonsoft.Json;
using Filta.Datatypes;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Filta {
    public class DevPanel : EditorWindow {
        private string email = "";
        private string password = "";
        private bool _stayLoggedIn;
        private const string TEST_FUNC_LOCATION = "http://localhost:5000/filta-machina/us-central1/";
        private const string FUNC_LOCATION = "https://us-central1-filta-machina.cloudfunctions.net/";
        private const string REFRESH_KEY = "RefreshToken";
        private const long UPLOAD_LIMIT = 100000000;
        private string UPLOAD_URL { get { return runLocally ? TEST_FUNC_LOCATION + "uploadArtSource" : FUNC_LOCATION + "uploadUnityPackage"; } }
        private string DELETE_PRIV_ART_URL { get { return runLocally ? TEST_FUNC_LOCATION + "deletePrivArt" : FUNC_LOCATION + "deletePrivArt"; } }
        private const string loginURL = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";
        private const string refreshURL = "https://securetoken.googleapis.com/v1/token?key=";
        private const string releaseURL = "https://raw.githubusercontent.com/getfilta/artist-unityplug/main/release.json";
        private const string packagePath = "Packages/com.getfilta.artist-unityplug";
        private const string fbaseKey = "AIzaSyAiefSo-GLf2yjEwbXhr-1MxMx0A6vXHO0";
        private const string variantTempSave = "Assets/Filter.prefab";
        private string _statusBar = "";
        private string statusBar { get { return _statusBar; } set { _statusBar = value; this.Repaint(); } }
        private bool runLocally = false;
        private string selectedArtTitle = "";
        private string selectedArtKey = "";
        private Vector2 leftScrollPosition;
        private Vector2 rightScrollPosition;

        private Dictionary<string, ArtMeta> privateCollection = new Dictionary<string, ArtMeta>();
        private static LoginResponse loginData;

        private PluginInfo _pluginInfo;
        private static DateTime _expiryTime;
        private bool _watchingQueue;
        private GUIStyle s;

        private ReleaseInfo _masterReleaseInfo;
        private ReleaseInfo _localReleaseInfo;
        
        private AddRequest _addRequest;


        [MenuItem("Filta/Artist Panel (Dockable)")]
        static void InitDockable() {
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), false, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.Show();
        }

        [MenuItem("Filta/Artist Panel (Always On Top)")]
        static void InitFloating() {
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), true, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.ShowUtility();
        }

        #region Simulator
        private SimulatorBase _simulator;
        private Simulator _faceSimulator;
        private BodySimulator _bodySimulator;
        private bool _activeSimulator;
        private bool _loggingIn;
        private int _vertexNumber;

        private static string GetVersionNumber() {
            ReleaseInfo releaseInfo = GetLocalReleaseInfo();
            return $"v{releaseInfo.version.pluginAppVersion}.{releaseInfo.version.pluginMajorVersion}.{releaseInfo.version.pluginMinorVersion}";
        }

        private async void OnEnable() {
            s = new GUIStyle();
            EditorApplication.playModeStateChanged += FindSimulator;
            EditorSceneManager.activeSceneChangedInEditMode += HandleSceneChange;
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            _localReleaseInfo = GetLocalReleaseInfo();
            SetPluginInfo();
            if (loginData == null || String.IsNullOrEmpty(loginData.idToken)) {
                await LoginAutomatic();
            } else {
                if (DateTime.Now > _expiryTime) {
                    loginData = null;
                    await LoginAutomatic();
                } else {
                    await GetPrivateCollection();
                    GetFiltersOnQueue();
                    GetMasterReleaseInfo();
                }
            }
        }

        private void HandleSceneChange(Scene oldScene, Scene newScene) {
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            SetPluginInfo();
        }

        private void FindSimulator(PlayModeStateChange stateChange) {
            _simulator = FindObjectOfType<SimulatorBase>();
            GameObject simulatorObject = _simulator.gameObject;
            if (_simulator != null) {
                _activeSimulator = true;
                if (_simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
                    _faceSimulator = simulatorObject.GetComponent<Simulator>();
                } else {
                    _bodySimulator = simulatorObject.GetComponent<BodySimulator>();
                }
            }
        }

        private void SetPluginInfo() {
            PluginInfo.FilterType filterType = _simulator._simulatorType == SimulatorBase.SimulatorType.Body
                ? PluginInfo.FilterType.Body
                : PluginInfo.FilterType.Face;
            _pluginInfo = new PluginInfo { version = _localReleaseInfo.version.pluginAppVersion, filterType = filterType, resetOnRecord = false};
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= FindSimulator;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChange;
            DisposeQueue();
        }

        private void HandleSimulator() {
            if (!_activeSimulator) return;
            EditorGUILayout.LabelField("Simulator", EditorStyles.boldLabel);
            if (_simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
                HandleFaceSimulator();
            } else {
                HandleBodySimulator();
            }

            if (!_simulator.IsSetUpProperly()) {
                EditorGUILayout.LabelField("Simulator is not set up properly");
                if (GUILayout.Button("Try Automatic Setup")) {
                    _simulator.TryAutomaticSetup();
                }
            }

        }


        private void HandleBodySimulator() {
            if (!_bodySimulator.isPose) {
                if (GUILayout.Button("Show T-Pose Visualiser")) {
                    _bodySimulator.ToggleVisualiser(true);
                }
                if (_bodySimulator.isPlaying) {
                    if (GUILayout.Button("Stop")) {
                        _bodySimulator.PauseSimulator();
                    }
                } else {
                    if (GUILayout.Button("Play")) {
                        _bodySimulator.ResumeSimulator();
                    }
                }
            } else {
                if (GUILayout.Button("Show Simulated Visualiser")) {
                    _bodySimulator.ToggleVisualiser(false);
                }
            }

        }

        private void HandleFaceSimulator() {

            EditorGUILayout.BeginHorizontal();
            if (_faceSimulator.isPlaying) {
                if (GUILayout.Button("Stop")) {
                    _faceSimulator.PauseSimulator();
                }
            } else {
                if (GUILayout.Button("Play")) {
                    _faceSimulator.ResumeSimulator();
                }
            }
            if (GUILayout.Button("Reset")) {
                _faceSimulator.ResetSimulator();
            }
            EditorGUILayout.EndHorizontal();
            _faceSimulator.showFaceMeshVisualiser =
                EditorGUILayout.Toggle("Show Face Mesh Visualiser", _faceSimulator.showFaceMeshVisualiser);
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Create Vertex Trackers", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _vertexNumber = EditorGUILayout.IntField("Vertex Index", _vertexNumber);
            if (GUILayout.Button("Create")) {
                if (_faceSimulator.vertexTrackers == null) {
                    _faceSimulator.vertexTrackers = new List<Simulator.VertexTracker>();
                }
                GameObject newVertex = _faceSimulator.GenerateVertexTracker(_vertexNumber);
                //To select the newly created vertex tracker so that the user would be aware it has been selected.
                Selection.activeGameObject = newVertex;
                _vertexNumber = 0;
                //takes away keyboard control from the input field (for the vertex number) so it actually shows the reset vertex number
                EditorGUI.FocusTextInControl(null);
            }
            EditorGUILayout.EndHorizontal();
            _faceSimulator.showVertexNumbers = EditorGUILayout.Toggle("Show Vertex Index", _faceSimulator.showVertexNumbers);
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Create Face Mesh", EditorStyles.boldLabel);
            if (GUILayout.Button("Create")) {
                GameObject newFace = _faceSimulator.SpawnNewFaceMesh();
                Selection.activeGameObject = newFace;
            }
        }

        #endregion

        #region Bundle Queue

        private Dictionary<string, Bundle> _bundles = new Dictionary<string, Bundle>();
        private EventSourceReader _evt;
        private async void GetFiltersOnQueue() {
            _bundles = new Dictionary<string, Bundle>();
            string getUrlQueue = $"https://filta-machina.firebaseio.com/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{loginData.localId}\"&print=pretty";
            UnityWebRequest request = UnityWebRequest.Get(getUrlQueue);
            await request.SendWebRequest();
            JObject results = JObject.Parse(request.downloadHandler.text);
            foreach (JProperty prop in results.Properties()) {
                string id = prop.Name;
                string bundleTitle = prop.Value["title"].Value<string>();
                int queue = prop.Value["queue"].Value<int>();
                _bundles.Add(id, new Bundle { queue = queue, title = bundleTitle });
            }
            ListenToQueue();
        }

        private void ListenToQueue() {
            _evt = new EventSourceReader(new Uri($"https://filta-machina.firebaseio.com/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{loginData.localId}\"")).Start();
            _evt.MessageReceived += (sender, e) => {
                if (e.Event == "put") {
                    try {
                        QueueResponse response = JsonConvert.DeserializeObject<QueueResponse>(e.Message);
                        string[] paths = response.path.Split('/');
                        if (response.data is int queue) {
                            _bundles[paths[1]].queue = queue;
                        } else {
                            SetStatusMessage($"{_bundles[paths[1]].title} successfully processed! (5/5)");
                            _bundles.Remove(paths[1]);
                        }

                    } catch (Exception exception) {
                        if (exception is JsonReaderException) {
                            return;
                        }
                        Debug.LogError(exception.Message);
                    }
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

        private void DisplayQueue() {
            if (_bundles == null || _bundles.Count <= 0)
                return;
            GUILayout.Label("Filters being processed", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter name");
            GUILayout.Label("Queue number");
            GUILayout.EndHorizontal();
            foreach (KeyValuePair<string, Bundle> bundle in _bundles) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(bundle.Value.title);
                GUILayout.Label(bundle.Value.queue == 999 ? "still uploading" : bundle.Value.queue.ToString());
                GUILayout.EndHorizontal();
            }
            DrawUILine(Color.gray);
        }

        private void DisposeQueue() {
            _evt?.Dispose();
        }

        private void OnInspectorUpdate() {
            Repaint();
        }

        public class Bundle {
            public string title;
            public int queue;
        }

        public class QueueResponse {
            public string path;
            public int? data;
        }

        #endregion
        void OnGUI() {
            Login();
            if (isLoggedIn()) {
                EditorGUILayout.BeginHorizontal();
                leftScrollPosition = GUILayout.BeginScrollView(leftScrollPosition);
                if (loginData != null && loginData.idToken != "") {
                    CreateNewScene();
                    if (_activeSimulator) {
                        DrawUILine(Color.gray);
                        HandleSimulator();
                        DrawUILine(Color.gray);
                    }
                    EditorGUILayout.LabelField("Extra settings", EditorStyles.boldLabel);
                    _pluginInfo.resetOnRecord = EditorGUILayout.Toggle("Reset Filter On Record", _pluginInfo.resetOnRecord);
                    DrawUILine(Color.gray);
                    DisplayQueue();
                }

                GUILayout.FlexibleSpace();
                HandleNewPluginVersion();
                AdvancedSettings();
                GUILayout.EndScrollView();

                rightScrollPosition = GUILayout.BeginScrollView(rightScrollPosition);

                if (loginData != null && loginData.idToken != "") {
                    if (selectedArtKey != "") {
                        SelectedArt();
                    } else {
                        PrivateCollection();
                    }
                }
                GUILayout.FlexibleSpace();
                Logout();
                GUILayout.EndScrollView();
                EditorGUILayout.EndHorizontal();
            } else {
                GUILayout.FlexibleSpace();
                HandleNewPluginVersion();
                AdvancedSettings();
            }
            DrawUILine(Color.gray);

            EditorGUILayout.LabelField(statusBar, s);
        }

        private void AdvancedSettings() {
            runLocally = GUILayout.Toggle(runLocally, "(ADVANCED) Use local firebase host");
        }

        private void UpdatePanel() {
            _addRequest = UnityEditor.PackageManager.Client.Add("https://github.com/getfilta/artist-unityplug.git");
            SetStatusMessage("Updating plugin! Please wait a while");
        }
        public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        private string _sceneName;
        void CreateNewScene() {
            EditorGUILayout.LabelField("Create new filter scene", EditorStyles.boldLabel);
            _sceneName = (string)EditorGUILayout.TextField("Filter scene filename:", _sceneName);
            GUI.enabled = !String.IsNullOrWhiteSpace(_sceneName);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Face Filter")) {
                CreateScene(SimulatorBase.SimulatorType.Face);
            }

            if (GUILayout.Button("Create Body Filter ")) {
                CreateScene(SimulatorBase.SimulatorType.Body);
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

        }

        void CreateScene(SimulatorBase.SimulatorType type) {
            string templateSceneName = type == SimulatorBase.SimulatorType.Face
                ? "templateScene.unity"
                : "templateScene-body.unity";
            string scenePath = $"{packagePath}/Core/{templateSceneName}";
            bool success;
            if (!AssetDatabase.IsValidFolder("Assets/Filters")) {
                AssetDatabase.CreateFolder("Assets", "Filters");
            }

            success = AssetDatabase.CopyAsset(scenePath, $"Assets/Filters/{_sceneName}.unity");
            if (!success) {
                SetStatusMessage("Failed to create new filter scene file", true);
                Debug.LogError("Failed to create new filter scene file");
            } else {
                if (!String.IsNullOrEmpty(SceneManager.GetActiveScene().name)) {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                }

                EditorSceneManager.OpenScene($"Assets/Filters/{_sceneName}.unity", OpenSceneMode.Single);
            }
        }

        private async void GenerateAndUploadAssetBundle() {
            if (String.IsNullOrEmpty(selectedArtKey)) {
                //selectedArtKey = SceneManager.GetActiveScene().name;
                Debug.LogError("Error uploading! selectedArtKey is empty. Please report this bug");
                return;
            }

            bool assetBundleButton = GUILayout.Button($"Upload your filter to Filta");
            if (!assetBundleButton) { return; }
            if (DateTime.Now > _expiryTime) {
                loginData = null;
                if (!await LoginAutomatic())
                    return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Error", "You cannot complete this task while in Play Mode. Please leave Play Mode", "Ok");
                return;
            }


            GameObject filterObject = _simulator._filterObject.gameObject;
            if (filterObject == null) {
                EditorUtility.DisplayDialog("Error", "The object 'Filter' wasn't found in the hierarchy. Did you rename/remove it?", "Ok");
                return;
            }

            if (CheckForUnreadableMeshes(filterObject)) {
                return;
            }
            SetStatusMessage("Exporting... (1/5)");
            try {
                if (_simulator._simulatorType == SimulatorBase.SimulatorType.Body) {
                    _bodySimulator.PauseSimulator();
                    _bodySimulator.RevertAvatarsToTPose();
                }

                //PrefabUtility.ApplyPrefabInstance(filterObject, InteractionMode.AutomatedAction);
                GameObject filterDuplicate = Instantiate(filterObject);
                filterDuplicate.name = "Filter";
                PrefabUtility.SaveAsPrefabAsset(filterDuplicate, variantTempSave, out bool success);
                DestroyImmediate(filterDuplicate);
                if (success) {
                    AssetImporter.GetAtPath(variantTempSave).assetBundleName =
                        "filter";
                } else {
                    EditorUtility.DisplayDialog("Error",
                        "The object 'Filter' isn't a prefab. Did you delete it from your assets?", "Ok");
                    SetStatusMessage("Failed to generate asset bundle.", true);
                    return;
                }

            } catch {
                EditorUtility.DisplayDialog("Error",
                    "The object 'Filter' isn't a prefab. Did you delete it from your assets?", "Ok");
                SetStatusMessage("Failed to generate asset bundle.", true);
                return;
            } finally {
                if (_simulator._simulatorType == SimulatorBase.SimulatorType.Body) {
                    _bodySimulator.ResumeSimulator();
                }
            }

            string pluginInfoPath = Path.Combine(Application.dataPath, "pluginInfo.json");
            try {
                File.WriteAllText(
                    pluginInfoPath, JsonConvert.SerializeObject(_pluginInfo));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            } catch {
                EditorUtility.DisplayDialog("Error", "There was a problem editing the pluginInfo.json. Did you delete it from your assets?", "Ok");
                SetStatusMessage("Failed to generate asset bundle.", true);
                return;
            }

            string[] packagePaths = { "Assets/pluginInfo.json", variantTempSave };
            AssetDatabase.ExportPackage(packagePaths, "asset.unitypackage",
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), "asset.unitypackage");
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > UPLOAD_LIMIT) {
                HandleOversizePackage(packagePaths);
                return;
            }
            /*string assetBundleDirectory = "AssetBundles";
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            var manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                    BuildAssetBundleOptions.None,
                                    BuildTarget.iOS);
            assetBundlePath = $"{assetBundleDirectory}/filter";*/

            SetStatusMessage("Connecting... (2/5)");
            byte[] bytes = File.ReadAllBytes(pathToPackage);
            Hash128 hash = Hash128.Compute(bytes);
            /*if (!BuildPipeline.GetHashForAssetBundle(assetBundlePath, out hash))
            {
                statusBar = "Asset bundle not found";
                return;
            }*/
            WWWForm postData = new WWWForm();
            if (selectedArtKey != "temp") {
                Debug.LogWarning("Updating Art with artid: " + selectedArtKey);
                postData.AddField("artid", selectedArtKey);
            }
            postData.AddField("uid", loginData.idToken);
            postData.AddField("hash", hash.ToString());
            postData.AddField("title", selectedArtTitle);
            var www = UnityWebRequest.Post(UPLOAD_URL, postData);
            await www.SendWebRequest();
            SetStatusMessage("Connected! Uploading... (3/5)");
            var response = www.downloadHandler.text;
            UploadBundleResponse parsed;
            try {
                parsed = JsonUtility.FromJson<UploadBundleResponse>(response);
            } catch {
                SetStatusMessage("Error! Check console for more information", true);
                Debug.LogError(response);
                return;
            }
            if (_bundles.ContainsKey(parsed.artid)) {
                SetStatusMessage("Error: Previous upload still being processed. Please wait a few minutes and try again.", true);
                return;
            }
            _bundles.Add(parsed.artid, new Bundle { queue = 999, title = selectedArtTitle });
            UnityWebRequest upload = UnityWebRequest.Put(parsed.url, bytes);
            await upload.SendWebRequest();
            await GetPrivateCollection();
            selectedArtKey = parsed.artid;
            SetStatusMessage("Upload successful. Processing... (4/5)");
            AssetDatabase.DeleteAsset(variantTempSave);
        }
        
        private void HandleNewPluginVersion() {
            if (_masterReleaseInfo == null || _localReleaseInfo == null) {
                return;
            }

            if (_masterReleaseInfo.version.ToInt() > _localReleaseInfo.version.ToInt()) {
                ReleaseInfo.Version masterVersion = _masterReleaseInfo.version;
                GUILayout.Label(
                    $"New plugin version available! v{masterVersion.pluginAppVersion}.{masterVersion.pluginMajorVersion}.{masterVersion.pluginMinorVersion}",
                    EditorStyles.largeLabel);
                GUILayout.Label(_localReleaseInfo.releaseNotes);
                if (_addRequest != null && !_addRequest.IsCompleted) {
                    GUI.enabled = false;
                } 
                else if(_addRequest != null && !_addRequest.IsCompleted) {
                    SetStatusMessage("Successfully updated plugin");
                    _addRequest = null;
                }
                else {
                    GUI.enabled = true;
                }
                if (GUILayout.Button("Get latest plugin version")) {
                    UpdatePanel();
                }

                GUI.enabled = true;
            }
        }

        private async void GetMasterReleaseInfo() {
            try {
                UnityWebRequest req = UnityWebRequest.Get(releaseURL);
                await req.SendWebRequest();
                _masterReleaseInfo = JsonConvert.DeserializeObject<ReleaseInfo>(req.downloadHandler.text);
            }
            catch (Exception e) {
                Debug.LogError(e.Message);
            }
            
        }

        private static ReleaseInfo GetLocalReleaseInfo() {
            string data = File.ReadAllText($"{packagePath}/release.json");
            return JsonConvert.DeserializeObject<ReleaseInfo>(data);
        }
        
        

        private void HandleOversizePackage(string[] path) {
            string[] pathNames = AssetDatabase.GetDependencies(path);
            string readout = "";
            Dictionary<string, long> fileSizes = new Dictionary<string, long>();
            for (int i = 0; i < pathNames.Length; i++) {
                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), pathNames[i]);
                FileInfo fileInfo = new FileInfo(fullPath);
                fileSizes.Add(pathNames[i], fileInfo.Length);
            }
            fileSizes = new Dictionary<string, long>(fileSizes.OrderByDescending(pair => pair.Value));
            long limit = 0;
            foreach (KeyValuePair<string, long> file in fileSizes) {
                readout += $"\n {file.Key} - {file.Value / 1000000f:#.##}MB";
                limit += file.Value;
                if (limit > UPLOAD_LIMIT) {
                    break;
                }
            }

            EditorUtility.DisplayDialog("Error",
                $"Your filter is over {UPLOAD_LIMIT / 1000000}MB, please reduce the size. These are the files that might be causing this. {readout}",
                "Ok");
        }

        private async Task<bool> LoginAutomatic() {
            string token = PlayerPrefs.GetString(REFRESH_KEY, null);
            if (String.IsNullOrEmpty(token)) {
                return false;
            }

            bool success = false;
            _loggingIn = true;
            WWWForm postData = new WWWForm();
            postData.AddField("grant_type", "refresh_token");
            postData.AddField("refresh_token", token);
            UnityWebRequest www = UnityWebRequest.Post(refreshURL + fbaseKey, postData);
            SetStatusMessage("Logging you in...");
            await www.SendWebRequest();
            _loggingIn = false;
            string response = www.downloadHandler.text;
            if (response.Contains("TOKEN_EXPIRED")) {
                SetStatusMessage("Error: Token has expired", true);
            } else if (response.Contains("USER_NOT_FOUND")) {
                SetStatusMessage("Error: User was not found", true);
            } else if (response.Contains("INVALID_REFRESH_TOKEN")) {
                SetStatusMessage("Error: Invalid token provided", true);
            } else if (response.Contains("id_token")) {
                RefreshResponse refreshData = JsonUtility.FromJson<RefreshResponse>(response);
                loginData = new LoginResponse { refreshToken = refreshData.refresh_token, idToken = refreshData.id_token, expiresIn = refreshData.expires_in, localId = refreshData.user_id };
                SetStatusMessage("Login successful!");
                _expiryTime = DateTime.Now.AddSeconds(loginData.expiresIn);
                PlayerPrefs.SetString(REFRESH_KEY, loginData.refreshToken);
                PlayerPrefs.Save();
                success = true;
                try {
                    await GetPrivateCollection();
                    GetFiltersOnQueue();
                    GetMasterReleaseInfo();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
            } else {
                SetStatusMessage("Unknown Error. Check console for more information.", true);
                Debug.LogError(response);
            }

            return success;
        }

        private void Logout() {
            if (isLoggedIn()) {
                DrawUILine(Color.gray);
                bool logout = GUILayout.Button("Logout");
                if (logout) {
                    password = "";
                    loginData = null;
                    PlayerPrefs.SetString(REFRESH_KEY, null);
                    PlayerPrefs.Save();
                    GUI.FocusControl(null);
                }
            }
        }

        private bool isLoggedIn() {
            return loginData != null && !String.IsNullOrEmpty(loginData.idToken);
        }

        private async void Login() {
            if (isLoggedIn()) {
                return;
            }
            if (!_loggingIn) {
                EditorGUILayout.LabelField("Login to your user account", EditorStyles.boldLabel);
                email = (string)EditorGUILayout.TextField("email", email);
                password = (string)EditorGUILayout.PasswordField("password", password);
                _stayLoggedIn = EditorGUILayout.Toggle("Stay logged in", _stayLoggedIn);
            }
            bool submitButton = !_loggingIn && GUILayout.Button("Login");
            if (!submitButton) { return; }

            selectedArtKey = "";
            _loggingIn = true;
            WWWForm postData = new WWWForm();
            postData.AddField("email", email);
            postData.AddField("password", password);
            postData.AddField("returnSecureToken", "true");
            var www = UnityWebRequest.Post(loginURL + fbaseKey, postData);
            SetStatusMessage("Connecting...");

            await www.SendWebRequest();
            _loggingIn = false;
            var response = www.downloadHandler.text;
            if (response.Contains("EMAIL_NOT_FOUND")) {
                SetStatusMessage("Error: Email not found", true);
                return;
            } else if (response.Contains("MISSING_PASSWORD")) {
                SetStatusMessage("Error: Missing Password", true);
                return;
            } else if (response.Contains("INVALID_PASSWORD")) {
                SetStatusMessage("Error: Invalid Password", true);
                return;
            } else if (response.Contains("idToken")) {
                loginData = JsonUtility.FromJson<LoginResponse>(response);
                SetStatusMessage("Login successful!");
                _expiryTime = DateTime.Now.AddSeconds(loginData.expiresIn);
                if (_stayLoggedIn) {
                    PlayerPrefs.SetString(REFRESH_KEY, loginData.refreshToken);
                    PlayerPrefs.Save();
                }

                try {
                    await GetPrivateCollection();
                    GetFiltersOnQueue();
                    GetMasterReleaseInfo();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
            } else {
                SetStatusMessage("Unknown Error. Check console for more information.", true);
                Debug.LogError(response);
            }
        }

        private async Task GetPrivateCollection() {
            string url = $"https://firestore.googleapis.com/v1/projects/filta-machina/databases/(default)/documents/priv_collection/{loginData.localId}";
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("authorization", $"Bearer {loginData.idToken}");
                await req.SendWebRequest();
                if (req.responseCode == 404) {
                    Debug.LogWarning("No uploads found. User could be new or service is down.");
                    SetStatusMessage("No uploads found", true);
                    return;
                }
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError) {
                    throw new Exception(req.error.ToString());
                }
                if (req.downloadHandler != null) {
                    var jsonResult = JObject.Parse(req.downloadHandler.text);
                    var result = ParseArtMetas(jsonResult);
                    privateCollection = result;
                } else {
                    SetStatusMessage("Error Deleting. Check console for details.", true);
                    Debug.LogError("Request Result: " + req.result);
                }
                this.Repaint();
            }
        }

        private Dictionary<string, ArtMeta> ParseArtMetas(JObject json) {
            var output = new Dictionary<string, ArtMeta>();
            var fields = json["fields"];
            if (fields == null) {
                return output;
            }
            if (fields.Type != JTokenType.Object) {
                return output;
            }

            foreach (var artMetaJson in fields.Children<JProperty>()) {
                string name = artMetaJson.Name;
                ArtMeta value = new ArtMeta();
                value.artId = name;
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
                    }
                }

                output.Add(name, value);
            }
            return output;
        }


        private bool _showPrivCollection = true;
        private void PrivateCollection() {
            if (!String.IsNullOrEmpty(SceneManager.GetActiveScene().name)) {
                EditorGUILayout.LabelField("Choose the Filta upload to update:", EditorStyles.boldLabel);
                bool newClicked = GUILayout.Button("CREATE NEW FILTA UPLOAD");
                EditorGUILayout.Space();
                if (newClicked) {
                    selectedArtTitle = SceneManager.GetActiveScene().name;
                    selectedArtKey = "temp";
                }
                if (privateCollection == null || privateCollection.Count < 1) { return; }

                _showPrivCollection = EditorGUILayout.Foldout(_showPrivCollection, "Private Filta Collection");
                if (!_showPrivCollection)
                    return;
                foreach (var item in privateCollection) {
                    bool clicked = GUILayout.Button(item.Value.title);
                    if (clicked) {
                        selectedArtTitle = item.Value.title;
                        selectedArtKey = item.Key;
                    }
                }
            }
        }

        private async void DeletePrivArt(string artId) {
            if (GUILayout.Button("Delete upload from Filta")) {
                if (!EditorUtility.DisplayDialog("Delete", "Are you sure you want to delete this from Filta?", "yes", "cancel")) {
                    return;
                }
            } else {
                return;
            }
            if (DateTime.Now > _expiryTime) {
                loginData = null;
                if (!await LoginAutomatic())
                    return;
            }
            SetStatusMessage("Deleting...");
            try {
                WWWForm postData = new WWWForm();
                postData.AddField("uid", loginData.idToken);
                postData.AddField("artid", artId);
                var www = UnityWebRequest.Post(DELETE_PRIV_ART_URL, postData);
                await www.SendWebRequest();
                var response = www.downloadHandler.text;
                if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError) {
                    SetStatusMessage("Error Deleting. Check console for details.", true);
                    Debug.LogError(www.error + " " + www.downloadHandler.text);
                    return;
                }
                SetStatusMessage($"Delete: {response}");
            } finally {
                privateCollection.Remove(selectedArtKey);
                selectedArtKey = "";
            }
        }

        private void SelectedArt() {
            if (GUILayout.Button("Back")) {
                selectedArtKey = "";
                return;
            }

            EditorGUILayout.Space();
            selectedArtTitle = (string)EditorGUILayout.TextField("Title", selectedArtTitle);
            EditorGUILayout.Space();
            GenerateAndUploadAssetBundle();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DeletePrivArt(selectedArtKey);
        }

        private void SetStatusMessage(string message, bool isError = false) {
            s.normal.textColor = isError ? Color.red : Color.white;
            statusBar = message;
        }

        private bool CheckForUnreadableMeshes(GameObject filterParent) {
            bool result = false;
            string dialog = "All meshes used with SkinnedMeshRenderers must be marked as readable. Select the mesh(es) and set Read/Write to true in the Inspector. \n \n List of affected gameObjects: ";
            SkinnedMeshRenderer[] skinnedMeshRenderers = filterParent.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++) {
                if (!skinnedMeshRenderers[i].sharedMesh.isReadable) {
                    result = true;
                    dialog += $" {skinnedMeshRenderers[i].gameObject.name},";
                }
            }

            if (result) {
                EditorUtility.DisplayDialog("Error", dialog, "Ok");
            }

            return result;
        }
    }

    [Serializable]
    public class LoginResponse {
        public string localId;
        public string displayName;
        public string idToken;
        public string refreshToken;
        public int expiresIn;
    }

    [Serializable]
    public class RefreshResponse {
        public string id_token;
        public string refresh_token;
        public int expires_in;
        public string user_id;
    }

    [Serializable]
    public class UploadBundleResponse {
        public string url;
        public string artid;
    }

    public class UnityWebRequestAwaiter : INotifyCompletion {
        private UnityWebRequestAsyncOperation asyncOp;
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

    public struct PluginInfo {
        public enum FilterType { Face, Body }
        public int version;
        public FilterType filterType;
        public bool resetOnRecord;
    }

    public class ReleaseInfo
    {
        public Version version;
        public string releaseNotes;
        public class Version
        {
            public int pluginAppVersion;
            public int pluginMajorVersion;
            public int pluginMinorVersion;

            public int ToInt() {
                return (pluginAppVersion * 100) + (pluginMajorVersion * 10) + (pluginMinorVersion);
            }
        }
    }
}