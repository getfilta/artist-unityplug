#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.IO;
using Filta.Datatypes;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Filta {
    public class DevPanel : EditorWindow {
        #region Variable Declarations

        private bool _stayLoggedIn;

        private int _selectedTab = 0;
        private string[] _toolbarTitles = { "Simulator", "Uploader"};
        private string[] _adminToolbarTitles = { "Simulator", "Uploader", "Admin" };
        private int _selectedSimulator;
        private const string packagePath = "Packages/com.getfilta.artist-unityplug";
        private const string variantTempSave = "Assets/Filter.prefab";
        private readonly string[] packagePaths = { "Assets/pluginInfo.json", variantTempSave };
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
        private const int DefaultGameViewWidth = 1170;
        private const int DefaultGameViewHeight = 2532;

        private bool _isRefreshing;
        private double _refreshTimer;
        private DateTime _lastGuiTime;

        private const string KnowledgeBaseLink =
            "https://filta.notion.site/Artist-Knowledge-Base-2-0-bea6981130894902aa1c70f0adaa4112";
        private const string PublishPageLink = "https://www.getfilta.com/mint";

        #endregion
        
        #region Menus
        
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
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, DefaultGameViewWidth, DefaultGameViewHeight, "DefaultFiltaView");
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
        
        #endregion

        void OnGUI() {
            Login();
            AutoRefreshArts();
            if (Authentication.Instance.IsLoggedIn) {
                _selectedTab = GUILayout.Toolbar(_selectedTab, Authentication.IsAdmin ? _adminToolbarTitles: _toolbarTitles );
                switch (_selectedTab) {
                    case 0:
                        DrawSimulator();
                        break;
                    case 1:
                        DrawUploader();
                        break;
                    case 2:
                        DrawAdminTools();
                        break;
                }
            } else if (Authentication.Instance.AuthState == AuthenticationState.LoggedOut) {
                DrawSimulator();
            } else {
                GUILayout.FlexibleSpace();
                HandleNewPluginVersion();
            }
            DrawUILine(Color.gray);

            EditorGUILayout.LabelField(statusBar, s);
        }

        #region Simulator

        private SimulatorBase.SimulatorType _simulatorType;
        private FusionSimulator _fusionSimulator;
        private SimulatorBase _simulator;
        private Simulator _faceSimulator;
        private BodySimulator _bodySimulator;
        private bool _activeSimulator;
        private int _vertexNumber;

        private int _simulatorIndex;
        private bool _resetOnRecord;

        private readonly string[] _simulatorOptions = { "Face", "Body", "Face + Body" };

        public static string GetVersionNumber() {
            ReleaseInfo releaseInfo = GetLocalReleaseInfo();
            return $"v{releaseInfo.version.pluginAppVersion}.{releaseInfo.version.pluginMajorVersion}.{releaseInfo.version.pluginMinorVersion}";
        }

        private async void OnEnable() {
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>($"{packagePath}/Editor/icon.png");
            titleContent = new GUIContent($"Filta: Artist Panel - {GetVersionNumber()}", icon);
            s = new GUIStyle();
            _normalBackgroundColor = GUI.backgroundColor;
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
            if (_fusionSimulator == null || (_simulator != null && _fusionSimulator.activeType == type)) {
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
                _tabIndex = (int)SimulatorBase.SimulatorType.Body;
                _activeSimulator = true;
                _simulatorIndex = (int)SimulatorBase.SimulatorType.Fusion;
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
                        _simulatorIndex = (int)SimulatorBase.SimulatorType.Face;
                        _tabIndex = (int)SimulatorBase.SimulatorType.Face;
                    } else {
                        _bodySimulator = simulatorObject.GetComponent<BodySimulator>();
                        _simulatorType = SimulatorBase.SimulatorType.Body;
                        _simulatorIndex = (int)SimulatorBase.SimulatorType.Body;
                        _tabIndex = (int)SimulatorBase.SimulatorType.Body;
                    }
                }
            }
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
            _resetOnRecord = EditorGUILayout.Toggle("Reset filter when user starts recording", _resetOnRecord);
            EditorGUIUtility.labelWidth = originalValue;
            DrawUILine(Color.gray);
            GUILayout.FlexibleSpace();
            HandleNewPluginVersion();
            GUILayout.EndScrollView();
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
            _pluginInfo = new PluginInfo { version = _localReleaseInfo.version.pluginAppVersion, filterType = filterType, resetOnRecord = _resetOnRecord };
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

        private int _tabIndex;

        private void ShowSimulatorTabs() {
            GUILayout.BeginHorizontal();
            {
                GUI.backgroundColor = _tabIndex == (int)SimulatorBase.SimulatorType.Face ? Color.green : _normalBackgroundColor;
                GUI.enabled = _simulatorType != SimulatorBase.SimulatorType.Body;
                if (GUILayout.Button(_simulatorOptions[(int)SimulatorBase.SimulatorType.Face], EditorStyles.miniButtonLeft, GUILayout.ExpandHeight(true))) {
                    _tabIndex = (int)SimulatorBase.SimulatorType.Face;
                }

                GUI.backgroundColor = _tabIndex == (int)SimulatorBase.SimulatorType.Body ? Color.green : _normalBackgroundColor;
                GUI.enabled = _simulatorType != SimulatorBase.SimulatorType.Face;
                if (GUILayout.Button(_simulatorOptions[(int)SimulatorBase.SimulatorType.Body], EditorStyles.miniButtonRight, GUILayout.ExpandHeight(true))) {
                    _tabIndex = (int)SimulatorBase.SimulatorType.Body;
                }

                GUI.enabled = true;
                GUI.backgroundColor = _normalBackgroundColor;
            }
            GUILayout.EndHorizontal();
        }

        private void HandleSimulator() {
            ShowSimulatorTabs();
            switch (_tabIndex) {
                case 0:
                    SetSimulator(SimulatorBase.SimulatorType.Face);
                    break;
                case 1:
                    SetSimulator(SimulatorBase.SimulatorType.Body);
                    break;
            }
            HandleFilterTypeSwitching();
            DrawUILine(Color.gray);
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
                EditorGUILayout.BeginHorizontal();
                if (_bodySimulator.isPlaying) {
                    if (GUILayout.Button("Pause")) {
                        _bodySimulator.PauseSimulator();
                    }
                } else {
                    if (GUILayout.Button("Play")) {
                        _bodySimulator.ResumeSimulator();
                    }
                }
                if (GUILayout.Button("Reset")) {
                    _bodySimulator.ResetSimulator();
                }
                EditorGUILayout.EndHorizontal();
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

        void HandleFilterTypeSwitching() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            EditorGUILayout.LabelField("Choose Filter type", EditorStyles.boldLabel);
            _simulatorIndex = EditorGUILayout.Popup(_simulatorIndex, _simulatorOptions);
            switch (_simulatorType) {
                case SimulatorBase.SimulatorType.Face:
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Face)
                        return;
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Body) {
                        bool answer = EditorUtility.DisplayDialog("Warning",
                            $"You are switching from a face filter to a body filter. Any face filter work done will be lost. \nDo you wish to proceed?",
                            "Continue", "Cancel");
                        if (!answer) {
                            _simulatorIndex = (int)SimulatorBase.SimulatorType.Face;
                            return;
                        }
                        DestroyImmediate(_simulator._filterObject.gameObject);
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Body, SimulatorBase.SimulatorType.Face);
                    } else if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Fusion) {
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Fusion, SimulatorBase.SimulatorType.Face);
                    }

                    break;
                case SimulatorBase.SimulatorType.Body:
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Body)
                        return;
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Face) {
                        bool answer = EditorUtility.DisplayDialog("Warning",
                            $"You are switching from a body filter to a face filter. Any body filter work done will be lost. \nDo you wish to proceed?",
                            "Continue", "Cancel");
                        if (!answer) {
                            _simulatorIndex = (int)SimulatorBase.SimulatorType.Body;
                            return;
                        }
                        DestroyImmediate(_simulator._filterObject.gameObject);
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Face, SimulatorBase.SimulatorType.Body);
                    } else if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Fusion) {
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Fusion, SimulatorBase.SimulatorType.Body);
                    }

                    break;
                case SimulatorBase.SimulatorType.Fusion:
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Fusion)
                        return;
                    if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Body) {
                        bool answer = EditorUtility.DisplayDialog("Warning",
                            $"You are switching from a face + body filter to just a body filter. Any face filter work done will be lost. \nDo you wish to proceed?",
                            "Continue", "Cancel");
                        if (!answer) {
                            _simulatorIndex = (int)SimulatorBase.SimulatorType.Fusion;
                            return;
                        }
                        SetSimulator(SimulatorBase.SimulatorType.Body);
                        DestroyImmediate(_fusionSimulator.gameObject);
                        DestroyImmediate(_faceSimulator._filterObject.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Body, SimulatorBase.SimulatorType.Fusion);
                    } else if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Face) {
                        bool answer = EditorUtility.DisplayDialog("Warning",
                            $"You are switching from a face + body filter to just a face filter. Any body filter work done will be lost. \nDo you wish to proceed?",
                            "Continue", "Cancel");
                        if (!answer) {
                            _simulatorIndex = (int)SimulatorBase.SimulatorType.Fusion;
                            return;
                        }
                        SetSimulator(SimulatorBase.SimulatorType.Face);
                        DestroyImmediate(_fusionSimulator.gameObject);
                        DestroyImmediate(_bodySimulator._filterObject.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Face, SimulatorBase.SimulatorType.Fusion);
                    }

                    break;
            }

            FindSimulator(PlayModeStateChange.EnteredEditMode);
        }

        void SpawnNewFilterType(SimulatorBase.SimulatorType newSimType, SimulatorBase.SimulatorType oldSimType) {
            string simPath, filterPath;
            switch (newSimType) {
                case SimulatorBase.SimulatorType.Face:
                    simPath = $"{packagePath}/Editor/Simulator/Simulator.prefab";
                    filterPath = $"{packagePath}/Core/Filter.prefab";
                    break;
                case SimulatorBase.SimulatorType.Body:
                    simPath = $"{packagePath}/Editor/Simulator/BodyTracking/BodySimulator.prefab";
                    filterPath = $"{packagePath}/Core/FilterBody.prefab";
                    break;
                case SimulatorBase.SimulatorType.Fusion:
                    simPath = $"{packagePath}/Editor/Simulator/FusionSimulator.prefab";
                    filterPath = oldSimType == SimulatorBase.SimulatorType.Face
                        ? $"{packagePath}/Core/FilterBody.prefab"
                        : $"{packagePath}/Core/Filter.prefab";
                    break;
                default:
                    simPath = $"{packagePath}/Editor/Simulator/Simulator.prefab";
                    filterPath = $"{packagePath}/Core/Filter.prefab";
                    break;
            }

            GameObject localFilter = null;
            if (oldSimType != SimulatorBase.SimulatorType.Fusion) {
                GameObject filter = AssetDatabase.LoadAssetAtPath<GameObject>(filterPath);
                localFilter = PrefabUtility.InstantiatePrefab(filter) as GameObject;
            }

            GameObject sim = AssetDatabase.LoadAssetAtPath<GameObject>(simPath);
            GameObject localSim = PrefabUtility.InstantiatePrefab(sim) as GameObject;
            if (localSim is not null) {
                localSim.transform.SetAsFirstSibling();
            }

            if (localFilter is not null) {
                localFilter.transform.SetAsLastSibling();
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
                    GUILayout.Label("Error during processing. Try again");
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

        #region Admin
        
        private string _artistUid;
        private string _artistWallet;
        private string _loadingText;

        private ArtMeta[] _artMetas;
        
        private void DrawAdminTools() {
            if (!Authentication.IsAdmin) {
                EditorGUILayout.LabelField("You are not an admin.");
                return;
            }
            if (!String.IsNullOrEmpty(_loadingText)) {
                EditorGUILayout.LabelField(_loadingText);
                return;
            }
            EditorGUILayout.LabelField("Artist Uid");
            _artistUid = GUILayout.TextField(_artistUid);
            EditorGUILayout.LabelField("Artist Wallet Address");
            _artistWallet = GUILayout.TextField(_artistWallet);
            if (GUILayout.Button("Get Priv Collection")) {
                GetUserPrivCollection();
            }

            if (_artMetas == null || _artMetas.Length == 0) {
                return;
            }
            DrawUILine(Color.grey);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Private Collection");
            foreach (ArtMeta item in _artMetas) {
                bool clicked = GUILayout.Button(item.title);
                if (clicked) {
                    GetUnityPackageUrl(item);
                }
            }
        }

        private async void GetUnityPackageUrl(ArtMeta art) {
            _loadingText = "Getting signed url...";
            GetPrivCollectionUnityPackageResponse response;
            try {
                response = await Backend.Instance.GetUserPrivUnityPackage(art.artId);
            }
            catch (Exception e) {
                _loadingText = null;
                Debug.LogError($"Error getting signed url. {e.Message}");
                throw new Exception(e.Message);
            }
            GetUnityPackage(response.signedUrl, art);
        }

        private async void GetUnityPackage(string url, ArtMeta art) {
            _loadingText = "Downloading unity package...";
            try {
                byte[] data = await Backend.Instance.GetUnityPackage(url);
                string path = $"{Application.dataPath}/{art.title}.unitypackage";
                File.WriteAllBytes(path, data);
                AssetDatabase.ImportPackage(path, true);
            }
            catch (Exception e) {
                _loadingText = null;
                Debug.LogError($"Error downloading package. {e.Message}");
                throw new Exception(e.Message);
            }
            SetStatusMessage("Successfully loaded unitypackage");
            _loadingText = null;
        }

        private async void GetUserPrivCollection() {
            _loadingText = "Getting private collection...";
            GetPrivCollectionResponse response;
            try {
                response = await Backend.Instance.GetUserPrivCollection(_artistUid, _artistWallet);
            }
            catch (Exception e) {
                _loadingText = null;
                Debug.LogError($"Error loading private collection. {e.Message}");
                throw new Exception(e.Message);
            }
            
            _loadingText = null;
            _artMetas = response.collection;
        }
        
        private async void GetAdminStatus() {
            try {
                GetAccessResponse response = await Backend.Instance.GetAccess();
                Authentication.IsAdmin = response.isAdmin;
            }
            catch (Exception e) {
                Authentication.IsAdmin = false;
                Debug.LogError("Failed to check admin status" + e.Message);
            }
        }

        #endregion
        
        #region Uploader
        
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

            GameObject filterObject = Util.GetFilterObject();

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
                bool success = Util.GenerateFilterPrefab(filterObject, variantTempSave);
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
            SetPluginInfo();

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


            AssetDatabase.ExportPackage(packagePaths, "asset.unitypackage",
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), "asset.unitypackage");
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > FilterSizeWindow.UploadLimit) {
                string readout = FilterSizeWindow.CheckForFileSizes(packagePaths);
                EditorUtility.DisplayDialog("Error",
                    $"Your Filter is {fileInfo.Length / 1000000f:#.##}MB. This is over the {FilterSizeWindow.UploadLimit / 1000000}MB limit.\n{readout}",
                    "Ok");
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
        
        
        #endregion
        
        #region Util
        
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
        
        private static ReleaseInfo GetLocalReleaseInfo() {
            string data = File.ReadAllText($"{packagePath}/releaseLogs.json");
            return JsonConvert.DeserializeObject<List<ReleaseInfo>>(data)[^1];
        }
        
        private void SetStatusMessage(string message, bool isError = false) {
            s.normal.textColor = isError ? Color.red : Color.white;
            statusBar = message;
        }
        
        #endregion

        #region Filter/Scene Functionality

        private string _sceneName;
        void CreateNewScene() {
            EditorGUILayout.LabelField("Create new filter scene", EditorStyles.boldLabel);
            _sceneName = (string)EditorGUILayout.TextField("Filter scene filename:", _sceneName);
            GUI.enabled = !String.IsNullOrWhiteSpace(_sceneName);
            //EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Filter")) {
                CreateScene();
                _sceneName = "";
                GUI.FocusControl(null);
            }

            GUI.enabled = true;

        }

        void CreateScene() {
            string scenePath = $"{packagePath}/Core/templateScene.unity";
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
        
        private async Task RefreshExternalDatasources() {
            await RefreshArtsAndBundleStatus();
            Backend.Instance.ListenToQueue();
            _masterReleaseInfo = await Backend.Instance.GetMasterReleaseInfo();
        }
        
        private bool CheckObjectsOutsideFilter() {
            Scene scene = SceneManager.GetActiveScene();
            List<GameObject> rootObjects = new(scene.rootCount);
            scene.GetRootGameObjects(rootObjects);
            string extraObjects = "";
            for (int i = 0; i < rootObjects.Count; i++) {
                //Check if it's the simulator, filter, main camera or it's inactive.
                //This works under the assumption that artists would still want to be warned even if object is disabled.
                GameObject sim, filter;
                if (_simulatorType == SimulatorBase.SimulatorType.Fusion) {
                    sim = _fusionSimulator.gameObject;
                    filter = _simulator._filterObject.parent.gameObject;
                } else {
                    sim = _simulator.gameObject;
                    filter = _simulator._filterObject.gameObject;
                }
                if (rootObjects[i] == sim || rootObjects[i] == filter ||
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

        #endregion

        #region Auth
        
        public async Task<bool> EnsureUnexpiredLogin() {
            if (Authentication.Instance.IsLoginExpired) {
                Authentication.Instance.LogOut(false);
                return await LoginAutomatic();
            }
            return true;
        }
        
        private async Task<bool> Login(bool stayLoggedIn) {
            Authentication.IsAdmin = false;
            LoginResult result = await Authentication.Instance.Login(stayLoggedIn);
            if (result == LoginResult.Success) {
                try {
                    await RefreshExternalDatasources();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
                GetAdminStatus();
            }
            return result == LoginResult.Success;
        }

        private async Task<bool> LoginAutomatic() {
            Authentication.IsAdmin = false;
            LoginResult result = await Authentication.Instance.LoginAutomatic();
            if (result == LoginResult.Success) {
                try {
                    await RefreshExternalDatasources();
                } catch (Exception e) {
                    SetStatusMessage("Error downloading collection. Try again. Check console for more information.", true);
                    Debug.LogError("Error downloading: " + e.Message);
                }
                GetAdminStatus();
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
            if (Authentication.Instance.AuthState == AuthenticationState.LoggedIn) {
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
                return;
            }

            if (Authentication.Instance.AuthState == AuthenticationState.Cancelling) {
                EditorGUILayout.LabelField("Cancelling log in");
                return;
            }

            if (GUILayout.Button("Cancel")) {
                Authentication.Instance.CancelLogin();
            }
            
            if (Authentication.Instance.AuthState == AuthenticationState.PendingAskApproval) {
                EditorGUILayout.LabelField("Remote Login Code");
                EditorGUILayout.LabelField(Authentication.Instance.RemoteLoginPin, EditorStyles.largeLabel);

                if (GUILayout.Button("Go to remote login page")) {
                    GUI.FocusControl(null);
                    Application.OpenURL(Authentication.Instance.RemoteLoginUrl);
                }
            }

        }
        
        #endregion
        
    }
}
#endif