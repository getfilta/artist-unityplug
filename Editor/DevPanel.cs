using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using EvtSource;
using Newtonsoft.Json;
using Filta.Datatypes;
using Newtonsoft.Json.Linq;
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

        //Version number for changes in plugin that need accommodating on the app.
        private const int pluginAppVersion = 1;
        //Plugin major version number.
        private const int pluginMajorVersion = 4;
        //Plugin minor version number.
        private const int pluginMinorVersion = 0;
        

        [MenuItem("Filta/Artist Panel")]
        static void Init() {
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), false, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.Show();
        }

        #region Simulator
        private SimulatorBase _simulator;
        private Simulator _faceSimulator;
        private BodySimulator _bodySimulator;
        private bool _activeSimulator;
        private bool _loggingIn;
        private int _vertexNumber;

        private static string GetVersionNumber(){
            return $"v{pluginAppVersion}.{pluginMajorVersion}.{pluginMinorVersion}";
        }

        private async void OnEnable(){
            s = new GUIStyle();
            EditorApplication.playModeStateChanged += FindSimulator;
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            _pluginInfo = new PluginInfo
                {version = pluginAppVersion, isBody = _simulator._simulatorType == SimulatorBase.SimulatorType.Body};
            if (loginData == null || String.IsNullOrEmpty(loginData.idToken)) {
                await LoginAutomatic();
            } else {
                if (DateTime.Now > _expiryTime) {
                    loginData = null;
                    await LoginAutomatic();
                } else {
                    await GetPrivateCollection();
                    GetFiltersOnQueue();
                }
            }
        }

        private void FindSimulator(PlayModeStateChange stateChange){
            _simulator = FindObjectOfType<SimulatorBase>();
            GameObject simulatorObject = _simulator.gameObject;
            if (_simulator != null){
                _activeSimulator = true;
                if (_simulator._simulatorType == SimulatorBase.SimulatorType.Face){
                    _faceSimulator = simulatorObject.GetComponent<Simulator>();
                }
                else{
                    _bodySimulator = simulatorObject.GetComponent<BodySimulator>();
                }
            }
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= FindSimulator;
            DisposeQueue();
        }

        private void HandleSimulator() {
            if (!_activeSimulator) return;
            EditorGUILayout.LabelField("Simulator", EditorStyles.boldLabel);
            if (_simulator._simulatorType == SimulatorBase.SimulatorType.Face){
                HandleFaceSimulator();
            }
            else{
                HandleBodySimulator();
            }

            if (!_simulator.IsSetUpProperly()){
                EditorGUILayout.LabelField("Simulator is not set up properly");
                if (GUILayout.Button("Try Automatic Setup")){
                    _simulator.TryAutomaticSetup();
                }
            }
            
        }

        private void HandleBodySimulator(){
            if (_bodySimulator.isPlaying){
                if (GUILayout.Button("Stop")){
                    _bodySimulator.PauseSimulator();
                }
            }
            else{
                if (GUILayout.Button("Play")){
                    _bodySimulator.ResumeSimulator();
                }
            }
        }
        
        private void HandleFaceSimulator(){
            if (_faceSimulator.isPlaying) {
                if (GUILayout.Button("Stop")) {
                    _faceSimulator.PauseSimulator();
                }
            } else {
                if (GUILayout.Button("Play")) {
                    _faceSimulator.ResumeSimulator();
                }
            }

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
            if (GUILayout.Button("Create")){
                GameObject newFace = _faceSimulator.SpawnNewFaceMesh();
                Selection.activeGameObject = newFace;
            }
        }

        #endregion

        #region Bundle Queue
        
        private Dictionary<string, Bundle> _bundles;
        private EventSourceReader _evt;
        private async void GetFiltersOnQueue(){
            _bundles = new Dictionary<string, Bundle>();
            string getUrlQueue = $"https://filta-machina.firebaseio.com/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{loginData.localId}\"&print=pretty";
            UnityWebRequest request = UnityWebRequest.Get(getUrlQueue);
            await request.SendWebRequest();
            JObject results = JObject.Parse(request.downloadHandler.text);
            foreach (JProperty prop in results.Properties()){
                string id = prop.Name;
                string bundleTitle = prop.Value["title"].Value<string>();
                int queue = prop.Value["queue"].Value<int>();
                _bundles.Add(id, new Bundle{queue = queue, title = bundleTitle});
            }
            ListenToQueue();
        }

        private void ListenToQueue(){
            _evt = new EventSourceReader(new Uri($"https://filta-machina.firebaseio.com/bundle_queue.json?orderBy=\"artistId\"&equalTo=\"{loginData.localId}\"")).Start();
            _evt.MessageReceived += (sender, e) =>
            {
                if (e.Event == "put"){
                    try{
                        QueueResponse response = JsonConvert.DeserializeObject<QueueResponse>(e.Message);
                        string[] paths = response.path.Split('/');
                        if (response.data is int queue){
                            _bundles[paths[1]].queue = queue;
                        }
                        else{
                            SetStatusMessage($"{_bundles[paths[1]].title} successfully bundled!");
                            _bundles.Remove(paths[1]);
                        }
                        
                    }
                    catch(Exception exception){
                        if (exception is JsonReaderException){
                            return;
                        }
                        Debug.LogError(exception.Message);
                    }
                }
            };
            _evt.Disconnected += async (sender, e) => {
                await Task.Delay(e.ReconnectDelay);
                try{
                    if (!_evt.IsDisposed){
                        _evt.Start(); // Reconnect to the same URL
                    }
                }
                catch (Exception exception){
                    Debug.LogError(exception.Message);
                }

            };
        }

        private void DisplayQueue(){
            if (_bundles == null || _bundles.Count <= 0)
                return;
            GUILayout.Label("Filters being processed", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter name");
            GUILayout.Label("Queue number");
            GUILayout.EndHorizontal();
            foreach (KeyValuePair<string, Bundle> bundle in _bundles){
                GUILayout.BeginHorizontal();
                GUILayout.Label(bundle.Value.title);
                GUILayout.Label(bundle.Value.queue == 999 ? "still uploading" :bundle.Value.queue.ToString());
                GUILayout.EndHorizontal();
            }
            DrawUILine(Color.gray);
        }

        private void DisposeQueue(){
            _evt?.Dispose();
        }

        private void OnInspectorUpdate(){
            Repaint();
        }

        public class Bundle
        {
            public string title;
            public int queue;
        }

        public class QueueResponse
        {
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
                    if (_activeSimulator){
                        DrawUILine(Color.gray);
                        HandleSimulator();
                        DrawUILine(Color.gray);
                    }
                    DisplayQueue();
                }

                GUILayout.FlexibleSpace();
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
                AdvancedSettings();
            }
            DrawUILine(Color.gray);
            
            EditorGUILayout.LabelField(statusBar);
        }

        private void AdvancedSettings() {
            runLocally = GUILayout.Toggle(runLocally, "(ADVANCED) Use local firebase host");
            if (GUILayout.Button("Get latest plugin version")) {
                UpdatePanel();
            };
        }

        private void UpdatePanel() {
            UnityEditor.PackageManager.Client.Add("https://github.com/getfilta/artist-unityplug.git");
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

            if (GUILayout.Button("Create Body Filter ")){
                CreateScene(SimulatorBase.SimulatorType.Body);
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            FindSimulator(PlayModeStateChange.EnteredEditMode);

        }

        void CreateScene(SimulatorBase.SimulatorType type){
            string templateSceneName = type == SimulatorBase.SimulatorType.Face
                ? "templateScene.unity"
                : "templateScene-body.unity";
            string scenePath = $"Packages/com.getfilta.artist-unityplug/Core/{templateSceneName}";
            bool success;
            if (!AssetDatabase.IsValidFolder("Assets/Filters")){
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

            if (CheckForUnreadableMeshes(filterObject)){
                return;
            }
            SetStatusMessage("Generating asset bundles");
            try {
                //PrefabUtility.ApplyPrefabInstance(filterObject, InteractionMode.AutomatedAction);
                GameObject filterDuplicate = Instantiate(filterObject);
                filterDuplicate.name = "Filter";
                PrefabUtility.SaveAsPrefabAsset(filterDuplicate, variantTempSave, out bool success);
                DestroyImmediate(filterDuplicate);
                if (success){
                    AssetImporter.GetAtPath(variantTempSave).assetBundleName =
                        "filter";
                }
                else{
                    EditorUtility.DisplayDialog("Error", "The object 'Filter' isn't a prefab. Did you delete it from your assets?", "Ok");
                    SetStatusMessage("Failed to generate asset bundle.", true);
                    return;
                }
                
            } catch
            {
                EditorUtility.DisplayDialog("Error", "The object 'Filter' isn't a prefab. Did you delete it from your assets?", "Ok");
                SetStatusMessage("Failed to generate asset bundle.", true);
                return;
            }

            string pluginInfoPath = Path.Combine(Application.dataPath, "pluginInfo.json");
            try{
                File.WriteAllText(
                    pluginInfoPath,JsonConvert.SerializeObject(_pluginInfo));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch{
                EditorUtility.DisplayDialog("Error", "There was a problem editing the pluginInfo.json. Did you delete it from your assets?", "Ok");
                SetStatusMessage("Failed to generate asset bundle.", true);
                return;
            }

            string[] packagePaths = {"Assets/pluginInfo.json", variantTempSave};
            AssetDatabase.ExportPackage(packagePaths, "asset.unitypackage",
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), "asset.unitypackage");
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > UPLOAD_LIMIT){
                EditorUtility.DisplayDialog("Error", "Your filter is over 100MB, please reduce the size", "Ok");
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
            SetStatusMessage("Asset bundle generated");
            
            SetStatusMessage("Connecting...");
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
            SetStatusMessage("Connected! Uploading...");
            var response = www.downloadHandler.text;
            UploadBundleResponse parsed;
            try {
                parsed = JsonUtility.FromJson<UploadBundleResponse>(response);
            } catch {
                SetStatusMessage("Error! Check console for more information", true);
                Debug.LogError(response);
                return;
            }
            _bundles.Add(parsed.artid, new Bundle{queue = 999, title = selectedArtTitle});
            UnityWebRequest upload = UnityWebRequest.Put(parsed.url, bytes);
            await upload.SendWebRequest();
            await GetPrivateCollection();
            selectedArtKey = parsed.artid;
            SetStatusMessage("Upload successful");
            AssetDatabase.DeleteAsset(variantTempSave);
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
            } else if (response.Contains("MISSING_PASSWORD")) {
                SetStatusMessage("Error: Missing Password", true);
            } else if (response.Contains("INVALID_PASSWORD")) {
                SetStatusMessage("Error: Invalid Password", true);
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
            string url = $"https://filta-machina.firebaseio.com/priv_collection/{loginData.localId}/.json?auth={loginData.idToken}";
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                await req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError) {
                    throw new Exception(req.error.ToString());
                }
                var result = JsonConvert.DeserializeObject<Dictionary<string, ArtMeta>>(req.downloadHandler.text);
                privateCollection = result;
                this.Repaint();
            }
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

        private void SetStatusMessage(string message, bool isError = false){
            if (isError){
                s.normal.textColor = Color.red;
                return;
            }
            s.normal.textColor = Color.white;
            statusBar = message;
        }

        private bool CheckForUnreadableMeshes(GameObject filterParent){
            bool result = false;
            string dialog = "All meshes used with SkinnedMeshRenderers must be marked as readable. Select the mesh(es) and set Read/Write to true in the Inspector. \n \n List of affected gameObjects: " ;
            SkinnedMeshRenderer[] skinnedMeshRenderers = filterParent.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++){
                if (!skinnedMeshRenderers[i].sharedMesh.isReadable){
                    result = true;
                    dialog += $" {skinnedMeshRenderers[i].gameObject.name},";
                }
            }

            if (result){
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
        public int version;
        public bool isBody;
    }
}