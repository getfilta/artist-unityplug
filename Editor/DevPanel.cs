#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Filta {
    public class DevPanel : EditorWindow {
        private bool _stayLoggedIn;

        private int _selectedTab = 0;
        private string[] _toolbarTitles = { "Simulator", "Uploader" };
        private int _selectedSimulator;
        private string[] _simulatorTitles = {"Face", "Body"};
        private const long UPLOAD_LIMIT = 100000000;
        private const string packagePath = "Packages/com.getfilta.artist-unityplug";
        private const string variantTempSave = "Assets/Filter.prefab";
        private string _statusBar = "";
        private string statusBar { get { return _statusBar; } set { _statusBar = value; this.Repaint(); } }
        private string selectedArtTitle = "";
        private string selectedArtKey = "";
        private Vector2 simulatorScrollPosition;
        private Vector2 uploaderScrollPosition;

        private ArtsAndBundleStatus artsAndBundleStatus = new();

        private PluginInfo _pluginInfo;
        private bool _watchingQueue;
        private GUIStyle s;
        private Color _normalBackgroundColor;

        private List<ReleaseInfo> _masterReleaseInfo;
        private ReleaseInfo _localReleaseInfo;

        private AddRequest _addRequest;

        private const int Uploading = -1;
        private const int Limbo = 999;
        private const string TempSelectedArtKey = "temp";
        private const float RefreshTime = 15;
        private bool _isRefreshing;
        private double _refreshTimer;
        private DateTime _lastGuiTime;
        
        private const string KnowledgeBaseLink =
            "https://filta.notion.site/Artist-Knowledge-Base-2-0-bea6981130894902aa1c70f0adaa4112";
        private const string PublishPageLink = "https://www.getfilta.com/mint";

        [MenuItem("Filta/Artist Panel (Dockable)", false, 0)]
        static void InitDockable() {
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), false, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.Show();
        }

        [MenuItem("Filta/Artist Panel (Always On Top)", false, 1)]
        static void InitFloating() {
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), true, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.ShowUtility();
        }

        [MenuItem("Filta/Load Default Editor Layout", false, 4)]
        private static void LoadDefaultLayout() {
            string path = Path.GetFullPath($"{packagePath}/Core/FiltaLayout.wlt");
            LayoutUtility.LoadLayoutFromAsset(path);
        }

        [MenuItem("Filta/Documentation, Tutorials, Examples and FAQ", false, 5)]
        private static void OpenKnowledgeBase() {
            Application.OpenURL(KnowledgeBaseLink);
        }

        [MenuItem("Filta/Log Out", false, 6)]
        static void LogOut() {
            Authentication.Instance.LogOut(true);
            DevPanel window = (DevPanel)GetWindow(typeof(DevPanel), true, $"Filta: Artist Panel - {GetVersionNumber()}");
            window.SetStatusMessage("Logged out");
            GUI.FocusControl(null);
        }

        private const string RunLocallyMenuName = "Filta/(ADVANCED) Use local firebase host";

        [MenuItem(RunLocallyMenuName, false, 30)]
        private static void ToggleRunLocally() {
            Backend.Instance.RunLocally = !Backend.Instance.RunLocally;
        }

        [MenuItem(RunLocallyMenuName, true, 30)]
        private static bool ToggleRunLocallyValidate() {
            Menu.SetChecked(RunLocallyMenuName, Backend.Instance.RunLocally);
            return true;
        }

        private const string TestEnvirMenuName = "Filta/(ADVANCED) Use test environment (forces a logout)";

        [MenuItem(TestEnvirMenuName, false, 30)]
        private static void ToggleTestEnvir() {
            LogOut();
            Global.UseTestEnvironment = !Global.UseTestEnvironment;
        }

        [MenuItem(TestEnvirMenuName, true, 30)]
        private static bool ToggleTestEnvirValidate() {
            Menu.SetChecked(TestEnvirMenuName, Global.UseTestEnvironment);
            return true;
        }

        #region Simulator

        private SimulatorBase.SimulatorType _simulatorType;
        private FusionSimulator _fusionSimulator;
        private SimulatorBase _simulator;
        private Simulator _faceSimulator;
        private BodySimulator _bodySimulator;
        private bool _activeSimulator;
        private int _vertexNumber;

        private static string GetVersionNumber() {
            ReleaseInfo releaseInfo = GetLocalReleaseInfo();
            return $"v{releaseInfo.version.pluginAppVersion}.{releaseInfo.version.pluginMajorVersion}.{releaseInfo.version.pluginMinorVersion}";
        }

        private async void OnEnable() {
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>($"{packagePath}/Editor/icon.png");
            titleContent = new GUIContent($"Filta: Artist Panel - {GetVersionNumber()}", icon);
            s = new GUIStyle();
            EditorApplication.playModeStateChanged += FindSimulator;
            EditorSceneManager.activeSceneChangedInEditMode += HandleSceneChange;
            Global.StatusChange += HandleStatusChange;
            Backend.Instance.BundleQueue += OnBundleQueueUpdate;
            Authentication.Instance.AuthStateChanged += HandleAuthStateChange;
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            _localReleaseInfo = GetLocalReleaseInfo();
            SetPluginInfo();
            if (!Authentication.Instance.IsLoggedIn) {
                await LoginAutomatic();
            } else {
                if (Authentication.Instance.IsLoginExpired) {
                    await EnsureUnexpiredLogin();
                } else {
                    await RefreshExternalDatasources();
                }
            }
        }

        private void HandleSceneChange(Scene oldScene, Scene newScene) {
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            SetPluginInfo();
        }

        private void SetSimulator(SimulatorBase.SimulatorType type) {
            if (_fusionSimulator == null || _fusionSimulator.activeType == type) {
                return;
            }
            _fusionSimulator.activeType = type;
            switch (type) {
                case SimulatorBase.SimulatorType.Face:
                    _simulator = _fusionSimulator.faceSimulator;
                    _fusionSimulator.faceSimulator.Enable();
                    _fusionSimulator.bodySimulator.Disable();
                    break;
                case SimulatorBase.SimulatorType.Body:
                    _simulator = _fusionSimulator.bodySimulator;
                    _fusionSimulator.faceSimulator.Disable();
                    _fusionSimulator.bodySimulator.Enable();
                    break;
            }
        }

        private void FindSimulator(PlayModeStateChange stateChange) {
            _simulator = null;
            _activeSimulator = false;
            _fusionSimulator = FindObjectOfType<FusionSimulator>();
            if (_fusionSimulator != null) {
                _simulatorType = SimulatorBase.SimulatorType.Fusion;
                _faceSimulator = _fusionSimulator.faceSimulator;
                _bodySimulator = _fusionSimulator.bodySimulator;
                SetSimulator(SimulatorBase.SimulatorType.Body);
                _activeSimulator = true;
            } else {
                _simulator = FindObjectOfType<SimulatorBase>();
                if (_simulator == null) {
                    SetStatusMessage("Not a filter scene. Create a filter by selecting Create New Filter in the dev panel", true);
                    return;
                }
                GameObject simulatorObject = _simulator.gameObject;
                if (_simulator != null) {
                    _activeSimulator = true;
                    if (_simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
                        _faceSimulator = simulatorObject.GetComponent<Simulator>();
                        _simulatorType = SimulatorBase.SimulatorType.Face;
                    } else {
                        _bodySimulator = simulatorObject.GetComponent<BodySimulator>();
                        _simulatorType = SimulatorBase.SimulatorType.Body;
                    }
                }
            }
        }

        private void SetPluginInfo() {
            if (!_activeSimulator) {
                return;
            }

            PluginInfo.FilterType filterType = PluginInfo.FilterType.Face;
            switch (_simulatorType) {
                case SimulatorBase.SimulatorType.Face:
                    filterType = PluginInfo.FilterType.Face;
                    break;
                case SimulatorBase.SimulatorType.Body:
                    filterType = PluginInfo.FilterType.Body;
                    break;
                case SimulatorBase.SimulatorType.Fusion:
                    filterType = PluginInfo.FilterType.Fusion;
                    break;
            }
            _pluginInfo = new PluginInfo { version = _localReleaseInfo.version.pluginAppVersion, filterType = filterType, resetOnRecord = false };
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= FindSimulator;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChange;
            Global.StatusChange -= HandleStatusChange;
            Backend.Instance.BundleQueue -= OnBundleQueueUpdate;
            Authentication.Instance.AuthStateChanged -= HandleAuthStateChange;
            DisposeQueue();
        }

        private void HandleStatusChange(object sender, StatusChangeEventArgs e) {
            SetStatusMessage(e.Message, e.IsError);
        }

        private void HandleSimulator() {
            _selectedSimulator = GUILayout.Toolbar(_selectedSimulator, _simulatorTitles);
            switch (_selectedSimulator) {
                case 0:
                    SetSimulator(SimulatorBase.SimulatorType.Face);
                    break;
                case 1:
                    SetSimulator(SimulatorBase.SimulatorType.Body);
                    break;
            }
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
                    if (GUILayout.Button("Pause")) {
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

        private void SetButtonColor(bool isRed) {
            _normalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = isRed ? Color.magenta : Color.green;
        }

        private void ResetButtonColor() {
            GUI.backgroundColor = _normalBackgroundColor;
        }

        private void HandleFaceSimulator() {

            EditorGUILayout.BeginHorizontal();
            if (_faceSimulator.isPlaying) {
                if (GUILayout.Button("Pause")) {
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

            string visualizerButtonTitle = _faceSimulator.showFaceMeshVisualiser ? "Hide Face Mesh Visualiser" : "Show Face Mesh Visualiser";
            SetButtonColor(_faceSimulator.showFaceMeshVisualiser);
            if (GUILayout.Button(visualizerButtonTitle)) {
                _faceSimulator.showFaceMeshVisualiser = !_faceSimulator.showFaceMeshVisualiser;
            }
            ResetButtonColor();
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
            SetButtonColor(_faceSimulator.showVertexNumbers);
            string vertexIndexButtonTitle = _faceSimulator.showVertexNumbers ? "Hide Vertex Index" : "Show Vertex Index";
            if (GUILayout.Button(vertexIndexButtonTitle)) {
                _faceSimulator.showVertexNumbers = !_faceSimulator.showVertexNumbers;
            }
            ResetButtonColor();
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Create Face Mesh", EditorStyles.boldLabel);
            if (GUILayout.Button("Create")) {
                GameObject newFace = _faceSimulator.SpawnNewFaceMesh();
                Selection.activeGameObject = newFace;
            }
        }

        #endregion

        #region Bundle Queue

        private void GetFiltersOnQueue() {
            ListenToQueue();
        }

        private void ListenToQueue() {
            Backend.Instance.ListenToQueue();
        }

        private void DisplayQueue() {
            if (artsAndBundleStatus.Bundles.Count <= 0)
                return;
            GUILayout.Label("Filters being processed", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter name");
            GUILayout.Label("Queue number");
            GUILayout.EndHorizontal();
            foreach (KeyValuePair<string, Bundle> bundle in artsAndBundleStatus.Bundles) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(bundle.Value.title);
                if (bundle.Value.bundleQueuePosition == Uploading) {
                    GUILayout.Label("still uploading");
                } else if (bundle.Value.bundleQueuePosition == Limbo) {
                    GUILayout.Label("in Limbo");
                } else {
                    GUILayout.Label(bundle.Value.bundleQueuePosition.ToString());
                }
                GUILayout.EndHorizontal();
            }
        }

        private async void OnBundleQueueUpdate(object sender, EventArgs eventArgs) {
            await RefreshArtsAndBundleStatus();
        }

        private void DisposeQueue() {
            Backend.Instance.BundleQueue -= OnBundleQueueUpdate;
            Backend.Instance.DisposeQueue();
        }

        private void OnInspectorUpdate() {
            Repaint();
        }

        public class QueueResponse {
            public string path;
            public int? data;
        }

        #endregion
        void OnGUI() {
            Login();
            AutoRefreshArts();
            if (Authentication.Instance.IsLoggedIn) {
                _selectedTab = GUILayout.Toolbar(_selectedTab, _toolbarTitles);
                switch (_selectedTab) {
                    case 0:
                        DrawSimulator();
                        break;
                    case 1:
                        DrawUploader();
                        break;
                }
            } else if (Authentication.Instance.AuthState == AuthenticationState.LoggedOut){
                DrawSimulator();
            } else {
                GUILayout.FlexibleSpace();
                HandleNewPluginVersion();
            }
            DrawUILine(Color.gray);

            EditorGUILayout.LabelField(statusBar, s);
        }

        private void DrawSimulator() {
            simulatorScrollPosition = GUILayout.BeginScrollView(simulatorScrollPosition);
            CreateNewScene();
            if (_activeSimulator) {
                DrawUILine(Color.gray);
                HandleSimulator();
                DrawUILine(Color.gray);
            } else {
                EditorGUILayout.LabelField("Not a Filter scene. Create a new Filter above", EditorStyles.boldLabel);
                DrawUILine(Color.gray);
            }
            EditorGUILayout.LabelField("Extra settings", EditorStyles.boldLabel);
            float originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 215;
            _pluginInfo.resetOnRecord = EditorGUILayout.Toggle("Reset filter when user starts recording", _pluginInfo.resetOnRecord);
            EditorGUIUtility.labelWidth = originalValue;
            DrawUILine(Color.gray);

            GUILayout.FlexibleSpace();
            HandleNewPluginVersion();
            GUILayout.EndScrollView();
        }

        private void DrawUploader() {
            uploaderScrollPosition = GUILayout.BeginScrollView(uploaderScrollPosition);

            if (Authentication.Instance.IsLoggedIn) {
                if (selectedArtKey != "") {
                    SelectedArt();
                } else {
                    PrivateCollection();
                }
            }
            GUILayout.FlexibleSpace();
            DisplayQueue();
            GUILayout.EndScrollView();
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
                _sceneName = "";
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Create Body Filter ")) {
                CreateScene(SimulatorBase.SimulatorType.Body);
                _sceneName = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Create Face + Body Filter")) {
                CreateScene(SimulatorBase.SimulatorType.Fusion);
                _sceneName = "";
                GUI.FocusControl(null);
            }
            
            GUI.enabled = true;

        }

        void CreateScene(SimulatorBase.SimulatorType type) {
            string templateSceneName;
            switch (type) {
                case SimulatorBase.SimulatorType.Face:
                    templateSceneName = "templateScene.unity";
                    break;
                case SimulatorBase.SimulatorType.Body:
                    templateSceneName = "templateScene-body.unity";
                    break;
                case SimulatorBase.SimulatorType.Fusion:
                    templateSceneName = "templateScene-fusion.unity";
                    break;
                default:
                    templateSceneName = "templateScene.unity";
                    break;
            }
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
                SetStatusMessage("Created new scene");
            }
        }

        private async void GenerateAndUploadAssetBundle() {
            if (String.IsNullOrEmpty(selectedArtKey)) {
                //selectedArtKey = SceneManager.GetActiveScene().name;
                Debug.LogError("Error uploading! selectedArtKey is empty. Please report this bug");
                return;
            }
            string buttonTitle = selectedArtKey == TempSelectedArtKey ? "Upload new filter to Filta" : "Update your filta";
            bool assetBundleButton = GUILayout.Button(buttonTitle);
            if (!assetBundleButton) { return; }
            if (!await EnsureUnexpiredLogin()) {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Error", "You cannot complete this task while in Play Mode. Please leave Play Mode", "Ok");
                return;
            }

            if (selectedArtKey != TempSelectedArtKey && artsAndBundleStatus.Bundles.ContainsKey(selectedArtKey)) {
                //Only allows uploading of filter if a version is NOT currently being bundled or if it is in Limbo
                //Limbo is when the filter has been successfully uploaded to cloud storage, but something has stopped it from being processed by assetbundler
                var bundle = artsAndBundleStatus.Bundles[selectedArtKey];
                // or if it has been in the "uploading" state for more than 5 minutes 
                bool isTooLongUploading = bundle.bundleQueuePosition == Uploading && Global.GetTimeSince(bundle.lastUpdated) > TimeSpan.FromMinutes(5);
                if (bundle.bundleQueuePosition != Limbo && !isTooLongUploading) {
                    SetStatusMessage("Error: Previous upload still being processed. Please wait up to 5 minutes and try again.", true);
                    return;
                }
            }

            GameObject filterObject = _simulator._filterObject.gameObject;
            if (filterObject == null) {
                EditorUtility.DisplayDialog("Error", "The object 'Filter' wasn't found in the hierarchy. Did you rename/remove it?", "Ok");
                return;
            }
            if (CheckForUnreadableMeshes(filterObject)) {
                return;
            }
            if (CheckObjectsOutsideFilter()) {
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
            string uploadResultKey = await Backend.Instance.Upload(selectedArtKey, selectedArtTitle, hash, bytes);
            await RefreshArtsAndBundleStatus();
            if (uploadResultKey != null) {
                selectedArtKey = uploadResultKey;
                SetStatusMessage("Upload successful. Processing... (4/5)");
            }
            AssetDatabase.DeleteAsset(variantTempSave);
        }

        private void HandleNewPluginVersion() {
            if (_masterReleaseInfo == null || _localReleaseInfo == null) {
                return;
            }

            if (_masterReleaseInfo[^1].version.ToInt() > _localReleaseInfo.version.ToInt()) {
                DisplayReleaseNotes();
                if (_addRequest != null && !_addRequest.IsCompleted) {
                    GUI.enabled = false;
                } else if (_addRequest != null && !_addRequest.IsCompleted) {
                    SetStatusMessage("Successfully updated plugin");
                    _addRequest = null;
                } else {
                    GUI.enabled = true;
                }
                if (GUILayout.Button("Get latest plugin version")) {
                    UpdatePanel();
                }

                GUI.enabled = true;
            }
        }

        Vector2 _scrollPos;
        void DisplayReleaseNotes() {
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(100));
            for (int i = _masterReleaseInfo.Count - 1; i >= 0; i--) {
                ReleaseInfo masterInfo = _masterReleaseInfo[i];
                ReleaseInfo.Version masterVersion = masterInfo.version;
                if (masterVersion.ToInt() <= _localReleaseInfo.version.ToInt()) {
                    break;
                }

                string label = i == _masterReleaseInfo.Count - 1 ? "New plugin version available! " : "";
                label += $"v{masterVersion.pluginAppVersion}.{masterVersion.pluginMajorVersion}.{masterVersion.pluginMinorVersion}";
                GUILayout.Label(label, EditorStyles.largeLabel);
                GUILayout.Label(masterInfo.releaseNotes);
            }
            EditorGUILayout.EndScrollView();
        }

        private async Task RefreshExternalDatasources() {
            await RefreshArtsAndBundleStatus();
            Backend.Instance.ListenToQueue();
            _masterReleaseInfo = await Backend.Instance.GetMasterReleaseInfo();
        }

        private static ReleaseInfo GetLocalReleaseInfo() {
            string data = File.ReadAllText($"{packagePath}/releaseLogs.json");
            return JsonConvert.DeserializeObject<List<ReleaseInfo>>(data)[^1];
        }



        private void HandleOversizePackage(string[] path) {
            string[] pathNames = AssetDatabase.GetDependencies(path);
            string readout = "";
            Dictionary<string, long> fileSizes = new();
            for (int i = 0; i < pathNames.Length; i++) {
                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), pathNames[i]);
                FileInfo fileInfo = new(fullPath);
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

        private bool CheckObjectsOutsideFilter() {
            Scene scene = SceneManager.GetActiveScene();
            List<GameObject> rootObjects = new(scene.rootCount);
            scene.GetRootGameObjects(rootObjects);
            string extraObjects = "";
            for (int i = 0; i < rootObjects.Count; i++) {
                //Check if it's the simulator, filter, main camera or it's inactive.
                //This works under the assumption that artists would still want to be warned even if object is disabled.
                if (rootObjects[i] == _simulator.gameObject || rootObjects[i] == _simulator._filterObject.gameObject ||
                    rootObjects[i] == Camera.main.gameObject) {
                    continue;
                }
                extraObjects += $"\n{rootObjects[i].name}";
                if (!rootObjects[i].activeSelf) {
                    extraObjects += " (inactive)";
                }
            }

            if (!String.IsNullOrEmpty(extraObjects)) {
                bool answer = EditorUtility.DisplayDialog("Warning",
                    $"There are some gameObjects in the scene that aren't children of the Filter object. They will not be included in your filter. Here's a list. {extraObjects}\nDo you wish to proceed?",
                    "Continue", "Cancel");
                return !answer;
            }

            return false;
        }

        public async Task<bool> EnsureUnexpiredLogin() {
            if (Authentication.Instance.IsLoginExpired) {
                Authentication.Instance.LogOut(false);
                return await LoginAutomatic();
            }
            return true;
        }

        private async Task<bool> Login(bool stayLoggedIn) {
            LoginResult result = await Authentication.Instance.Login(stayLoggedIn);
            if (result == LoginResult.Success) {
                try {
                    await RefreshExternalDatasources();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
            }
            return result == LoginResult.Success;
        }

        private async Task<bool> LoginAutomatic() {
            LoginResult result = await Authentication.Instance.LoginAutomatic();
            if (result == LoginResult.Success) {
                try {
                    await RefreshExternalDatasources();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
            } else {
                SetStatusMessage("Login failed");
                Debug.LogError($"Login failed with result: {result}");
            }
            return result == LoginResult.Success;
        }

        private void HandleAuthStateChange(object sender, EventArgs unused) {
            // conveniently, setting status does a repaint. Otherwise we could
            // do it here.
            switch (Authentication.Instance.AuthState) {
                case AuthenticationState.LoggedIn:
                    SetStatusMessage("Logged in");
                    break;
                case AuthenticationState.LoggingIn:
                    SetStatusMessage("Logging in...");
                    break;
                // case AuthenticationState.LoggedOut:
                //     SetStatusMessage("Logged out");
                //     break;
                case AuthenticationState.PendingAsk:
                    SetStatusMessage("Initiating remote login...");
                    break;
                case AuthenticationState.PendingAskApproval:
                    SetStatusMessage("Waiting for remote approval...");
                    break;
                case AuthenticationState.PendingRefresh:
                    SetStatusMessage("Refreshing login...");
                    break;
            }
        }

        private async void Login() {
            if (Authentication.Instance.AuthState == AuthenticationState.LoggedIn
                || Authentication.Instance.AuthState == AuthenticationState.LoggingIn
                || Authentication.Instance.AuthState == AuthenticationState.PendingAsk
                || Authentication.Instance.AuthState == AuthenticationState.PendingRefresh) {
                return;
            }

            if (Authentication.Instance.AuthState == AuthenticationState.LoggedOut) {
                bool initRemoteLogin = GUILayout.Button("Request login");
                _stayLoggedIn = EditorGUILayout.Toggle("Stay logged in", _stayLoggedIn);
                if (!initRemoteLogin) {
                    return;
                }
                selectedArtKey = "";
                GUI.FocusControl(null);

                await Login(_stayLoggedIn);
            } else if (Authentication.Instance.AuthState == AuthenticationState.PendingAskApproval) {
                EditorGUILayout.LabelField("Remote Login Code");
                EditorGUILayout.LabelField(Authentication.Instance.RemoteLoginPin, EditorStyles.largeLabel);

                if (GUILayout.Button("Go to remote login page")) {
                    GUI.FocusControl(null);
                    Application.OpenURL(Authentication.Instance.RemoteLoginUrl);
                }
            }

        }

        private async void AutoRefreshArts() {
            if (artsAndBundleStatus == null || artsAndBundleStatus.Bundles == null ||
                artsAndBundleStatus.Bundles.Count == 0) {
                return;
            }

            if (_isRefreshing) {
                return;
            }

            if (_lastGuiTime == DateTime.MinValue) {
                _lastGuiTime = DateTime.Now;
            }

            double seconds = (DateTime.Now - _lastGuiTime).TotalSeconds;
            _refreshTimer += seconds;
            if (_refreshTimer > RefreshTime) {
                _isRefreshing = true;
                await RefreshArtsAndBundleStatus();
                _isRefreshing = false;
                _refreshTimer = 0;
            }

            _lastGuiTime = DateTime.Now;
        }

        private async Task RefreshArtsAndBundleStatus() {
            if (!Authentication.Instance.IsLoggedIn) {
                return;
            }
            this.artsAndBundleStatus = new ArtsAndBundleStatus();
            this.artsAndBundleStatus = await Backend.Instance.GetArtsAndBundleStatus();
            this.Repaint();
        }

        private bool _showPrivCollection = true;
        private void PrivateCollection() {
            if (!String.IsNullOrEmpty(SceneManager.GetActiveScene().name)) {
                EditorGUILayout.LabelField("Choose the Filta upload to update:", EditorStyles.boldLabel);
                bool newClicked = GUILayout.Button("CREATE NEW FILTA UPLOAD");
                EditorGUILayout.Space();
                if (newClicked) {
                    selectedArtTitle = SceneManager.GetActiveScene().name;
                    selectedArtKey = TempSelectedArtKey;
                }
                if (artsAndBundleStatus == null || artsAndBundleStatus.ArtMetas.Count < 1) { return; }

                _showPrivCollection = EditorGUILayout.Foldout(_showPrivCollection, "Private Filta Collection");
                if (!_showPrivCollection)
                    return;
                foreach (var item in artsAndBundleStatus.ArtMetas) {
                    bool clicked = GUILayout.Button(item.Value.title);
                    if (clicked) {
                        selectedArtTitle = item.Value.title;
                        selectedArtKey = item.Key;
                    }
                }
            }
        }

        private void GoToPublishingPage() {
            if (selectedArtKey == TempSelectedArtKey) {
                return;
            }
            if (GUILayout.Button("Go to publishing page")) {
                Application.OpenURL($"{PublishPageLink}?id={selectedArtKey}");
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
            if (!await EnsureUnexpiredLogin()) {
                return;
            }
            SetStatusMessage("Deleting...");
            try {
                string response = await Backend.Instance.DeletePrivateArt(artId);
                if (response == null) {
                    SetStatusMessage("Error Deleting. Check console for details.", true);
                    return;
                }
                SetStatusMessage($"Delete: {response}");
            } finally {
                artsAndBundleStatus.ArtMetas.Remove(selectedArtKey);
                selectedArtKey = "";
            }
        }

        private void SelectedArt() {
            if (GUILayout.Button("Back")) {
                selectedArtKey = "";
                EditorGUI.FocusTextInControl(null);
                return;
            }

            EditorGUILayout.Space();
            selectedArtTitle = (string)EditorGUILayout.TextField("Title", selectedArtTitle);
            EditorGUILayout.Space();
            GenerateAndUploadAssetBundle();
            GoToPublishingPage();
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
}
#endif