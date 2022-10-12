#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.IO;
using Filta.Datatypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Filta {
    public class DevPanel : EditorWindow {
        #region Variable Declarations
        
        private const string PackagePath = "Packages/com.getfilta.artist-unityplug";
        private const string VariantTempSave = "Assets/Filter.prefab";
        private const string KnowledgeBaseLink =
            "https://filta.notion.site/Artist-Knowledge-Base-2-0-bea6981130894902aa1c70f0adaa4112";
        private const string PublishPageLink = "https://www.getfilta.com/mint";
        private const string RunLocallyMenuName = "Filta/(ADVANCED) Use local firebase host";
        private const string TestEnvirMenuName = "Filta/(ADVANCED) Use test environment (forces a logout)";

        private const string RegistryName = "Filta Artist Suite";
        private const string RegistryUrl = "https://registry.npmjs.org";
        private const string RegistryScope = "com.getfilta.artist-unityplug";
        
        private const int Uploading = -1;
        private const int Limbo = 999;
        private const string TempSelectedArtKey = "temp";
        private const float RefreshTime = 15;
        private const int DefaultGameViewWidth = 1170;
        private const int DefaultGameViewHeight = 2532;
        
        private readonly string[] _toolbarTitles = { "Simulator","Beauty", "Uploader"};
        private readonly string[] _adminToolbarTitles = { "Simulator","Beauty", "Uploader", "Admin" };
        private readonly string[] _packagePaths = { "Assets/pluginInfo.json", VariantTempSave };
        private readonly string[] _simulatorOptions = { "Face", "Body", "Face + Body" };

        private bool _stayLoggedIn;
        private int _selectedTab = 0;
        private int _selectedSimulator;
        private string _statusBar = "";
        private string _selectedArtTitle = "";
        private string _selectedArtKey = "";
        private Vector2 _simulatorScrollPosition;
        private Vector2 _uploaderScrollPosition;
        private Vector2 _adminScrollPosition;
        private ArtsAndBundleStatus _artsAndBundleStatus = new();
        private PluginInfo _pluginInfo;
        private bool _watchingQueue;
        private GUIStyle _s;
        private Color _normalBackgroundColor;
        private static List<ReleaseInfo> _masterReleaseInfo;
        private static ReleaseInfo _localReleaseInfo;
        private AddRequest _addRequest;
        private bool _isRefreshing;
        private double _refreshTimer;
        private DateTime _lastGuiTime;
        
        private SimulatorBase.SimulatorType _simulatorType;
        private FusionSimulator _fusionSimulator;
        private SimulatorBase _simulator;
        private Simulator _faceSimulator;
        private BodySimulator _bodySimulator;
        private bool _activeSimulator;
        private int _vertexNumber;
        private int _simulatorIndex;
        private bool _resetOnRecord;
        private bool _dynamicLightOn;
        private int _tabIndex;
        
        private string _artistUid;
        private string _artistWallet;
        private string _loadingText;
        private ArtMeta[] _artMetas;
        
        private string _sceneName;
        private bool _showPrivCollection = true;
        private bool _recording;

        private string StatusBar {
            get => _statusBar;
            set {
                _statusBar = value;
                Repaint();
            }
        }
        
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
            string path = Path.GetFullPath($"{PackagePath}/Core/FiltaLayout.wlt");
            LayoutUtility.LoadLayoutFromAsset(path);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, DefaultGameViewWidth, DefaultGameViewHeight, "DefaultFiltaView");
            HotKeys.FocusSceneViewCamera();
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

        [MenuItem("Filta/Force Update Plugin", false, 7)]
        static void ForceUpdate() {
            Version version = _masterReleaseInfo[^1].version;
            string versionString =
                $"{version.pluginAppVersion}.{version.pluginMajorVersion}.{version.pluginMinorVersion}";
            UpdatePanel(versionString);
        }

        [MenuItem(RunLocallyMenuName, false, 30)]
        private static void ToggleRunLocally() {
            Backend.RunLocally = !Backend.RunLocally;
        }

        [MenuItem(RunLocallyMenuName, true, 30)]
        private static bool ToggleRunLocallyValidate() {
            Menu.SetChecked(RunLocallyMenuName, Backend.RunLocally);
            return true;
        }

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

        private async void OnEnable() {
            if (GraphicsSettings.defaultRenderPipeline == null) {
                Util.SetRenderPipeline();
            }
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>($"{PackagePath}/Editor/icon.png");
            titleContent = new GUIContent($"Filta: Artist Panel - {GetVersionNumber()}", icon);
            _s = new GUIStyle();
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
        
        private void OnDisable() {
            EditorApplication.playModeStateChanged -= FindSimulator;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChange;
            Global.StatusChange -= HandleStatusChange;
            Backend.Instance.BundleQueue -= OnBundleQueueUpdate;
            Authentication.Instance.AuthStateChanged -= HandleAuthStateChange;
            DisposeQueue();
        }

        void OnGUI() {
            Login();
            AutoRefreshArts();
            if (!_activeSimulator) {
                HandleNewPluginVersion();
                CreateNewScene();
                EditorGUILayout.LabelField("Not a Filter scene. Create a new Filter above", EditorStyles.boldLabel);
                DrawUILine(Color.gray);
                return;
            }
            if (Authentication.Instance.IsLoggedIn) {
                _selectedTab = GUILayout.Toolbar(_selectedTab, Authentication.IsAdmin ? _adminToolbarTitles: _toolbarTitles );
                switch (_selectedTab) {
                    case 0:
                        DrawSimulator();
                        break;
                    case 1:
                        DrawBeauty();
                        break;
                    case 2:
                        DrawUploader();
                        break;
                    case 3:
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

            EditorGUILayout.LabelField(StatusBar, _s);
        }

        #region Simulator

        private SimulatorBase.SimulatorType _activeSimulatorType;
        private void SetSimulator(SimulatorBase.SimulatorType type) {
            if (_fusionSimulator == null || (_simulator != null && _fusionSimulator.activeType == type)) {
                return;
            }
            _fusionSimulator.activeType = type;
            _activeSimulatorType = type;
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

            _fusionSimulator.faceSimulator.dynamicLightOn = false;
            _fusionSimulator.bodySimulator.dynamicLightOn = false;
        }

        private void FindSimulator(PlayModeStateChange stateChange) {
            _simulator = null;
            _activeSimulator = false;
            _fusionSimulator = FindObjectOfType<FusionSimulator>();
            if (_fusionSimulator != null) {
                _simulatorType = SimulatorBase.SimulatorType.Fusion;
                _faceSimulator = _fusionSimulator.faceSimulator;
                _bodySimulator = _fusionSimulator.bodySimulator;
                SetSimulator(_activeSimulatorType);
                _tabIndex = (int)_activeSimulatorType;
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
            _simulatorScrollPosition = GUILayout.BeginScrollView(_simulatorScrollPosition);
            HandleNewPluginVersion();
            CreateNewScene();
            DrawUILine(Color.gray);
            HandleSimulator();
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Extra settings", EditorStyles.boldLabel);
            float originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 215;
            _resetOnRecord = EditorGUILayout.Toggle("Reset filter when user starts recording", _resetOnRecord);
            HandleDynamicLighting();
            EditorGUIUtility.labelWidth = originalValue;
            DrawUILine(Color.gray);
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
        }

        private void HandleDynamicLighting() {
            if (_simulatorType != SimulatorBase.SimulatorType.Fusion) {
                _simulator.dynamicLightOn =
                    EditorGUILayout.Toggle("Environmental Lighting Estimation", _simulator.dynamicLightOn);
                //Used to mark scene as dirty when toggle value is changed
                if (_dynamicLightOn != _simulator.dynamicLightOn && !EditorApplication.isPlaying) {
                    EditorSceneManager.MarkAllScenesDirty();
                }
                _dynamicLightOn = _simulator.dynamicLightOn;
            } else {
                _fusionSimulator.DynamicLightOn =
                    EditorGUILayout.Toggle("Environmental Lighting Estimation", _fusionSimulator.DynamicLightOn);
                //Used to mark scene as dirty when toggle value is changed
                if (_dynamicLightOn != _fusionSimulator.DynamicLightOn && !EditorApplication.isPlaying) {
                    EditorSceneManager.MarkAllScenesDirty();
                }

                _dynamicLightOn = _fusionSimulator.DynamicLightOn;
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
            _pluginInfo = new PluginInfo { version = _localReleaseInfo.version.pluginAppVersion, pluginVersion = _localReleaseInfo.version, filterType = filterType, resetOnRecord = _resetOnRecord, dynamicLightOn = _simulator.dynamicLightOn};
        }

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
            HandleRecordingSimulation();
        }

        private void HandleRecordingSimulation() {
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Simulate User Actions", EditorStyles.boldLabel);
            if (!_recording) {
                if (GUILayout.Button("Start Recording")) {
                    _simulator.HandleStartRecording();
                    _recording = !_recording;
                }
            } else {
                SetButtonColor(true);
                if (GUILayout.Button("Stop Recording")) {
                    _simulator.HandleStopRecording();
                    _recording = !_recording;
                }
                ResetButtonColor();
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
                if (GUILayout.Button("Stop")) {
                    _bodySimulator.StopSimulator();
                }
                if (GUILayout.Button("Reset")) {
                    _bodySimulator.ResetSimulator();
                }
                EditorGUILayout.EndHorizontal();
                string visualizerButtonTitle = _bodySimulator.showBodyVisualiser ? "Hide Visualiser" : "Show Visualiser";
                SetButtonColor(_bodySimulator.showBodyVisualiser);
                if (GUILayout.Button(visualizerButtonTitle)) {
                    _bodySimulator.showBodyVisualiser = !_bodySimulator.showBodyVisualiser;
                }
                ResetButtonColor();
            } else {
                if (GUILayout.Button("Show Simulated Visualiser")) {
                    _bodySimulator.ToggleVisualiser(false);
                }
            }
            DrawUILine(Color.gray);
            EditorGUILayout.LabelField("Create Body Occluder", EditorStyles.boldLabel);
            if (GUILayout.Button("Create")) {
                GameObject newOccluder = _bodySimulator.SpawnNewBodyOccluder();
                Selection.activeGameObject = newOccluder;
            }
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

            if (GUILayout.Button("Stop")) {
                _faceSimulator.StopSimulator();
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
                        EditorSceneManager.MarkAllScenesDirty();
                    } else if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Fusion) {
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Fusion, SimulatorBase.SimulatorType.Face);
                        EditorSceneManager.MarkAllScenesDirty();
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
                        EditorSceneManager.MarkAllScenesDirty();
                    } else if (_simulatorIndex == (int)SimulatorBase.SimulatorType.Fusion) {
                        DestroyImmediate(_simulator.gameObject);
                        SpawnNewFilterType(SimulatorBase.SimulatorType.Fusion, SimulatorBase.SimulatorType.Body);
                        EditorSceneManager.MarkAllScenesDirty();
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
                        EditorSceneManager.MarkAllScenesDirty();
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
                        EditorSceneManager.MarkAllScenesDirty();
                    }

                    break;
            }
            FindSimulator(PlayModeStateChange.EnteredEditMode);
        }

        void SpawnNewFilterType(SimulatorBase.SimulatorType newSimType, SimulatorBase.SimulatorType oldSimType) {
            string simPath, filterPath;
            switch (newSimType) {
                case SimulatorBase.SimulatorType.Face:
                    simPath = $"{PackagePath}/Editor/Simulator/Simulator.prefab";
                    filterPath = $"{PackagePath}/Core/Filter.prefab";
                    break;
                case SimulatorBase.SimulatorType.Body:
                    simPath = $"{PackagePath}/Editor/Simulator/BodyTracking/BodySimulator.prefab";
                    filterPath = $"{PackagePath}/Core/FilterBody.prefab";
                    break;
                case SimulatorBase.SimulatorType.Fusion:
                    simPath = $"{PackagePath}/Editor/Simulator/FusionSimulator.prefab";
                    filterPath = oldSimType == SimulatorBase.SimulatorType.Face
                        ? $"{PackagePath}/Core/FilterBody.prefab"
                        : $"{PackagePath}/Core/Filter.prefab";
                    break;
                default:
                    simPath = $"{PackagePath}/Editor/Simulator/Simulator.prefab";
                    filterPath = $"{PackagePath}/Core/Filter.prefab";
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

        #region Beauty

        private bool _leftEyelashActive;
        private bool _rightEyelashActive;

        private void DrawBeauty() {
            Beauty beauty = _faceSimulator.beauty;
            if (beauty == null) {
                return;
            }
            EditorGUILayout.LabelField("Eyelashes", EditorStyles.boldLabel);
            beauty.leftEyelashActive = EditorGUILayout.Toggle("Left Eyelash Active", beauty.leftEyelashActive);
            beauty.LeftCurve = EditorGUILayout.CurveField("Left Eyelash Curve", beauty.LeftCurve);
            beauty.LeftAngle = EditorGUILayout.Slider("Left Angle of Curvature", beauty.LeftAngle, 0f, 75f);
            if (GUILayout.Button("Copy right eyelash")) {
                beauty.LeftCurve.keys = beauty.RightCurve.keys;
            }
            EditorGUILayout.Space();
            beauty.rightEyelashActive = EditorGUILayout.Toggle("Right Eyelash Active", beauty.rightEyelashActive);
            beauty.RightCurve = EditorGUILayout.CurveField("Right Eyelash Curve", beauty.RightCurve);
            beauty.RightAngle = EditorGUILayout.Slider("Right Angle of Curvature", beauty.RightAngle, 0f, 75f);
            if (GUILayout.Button("Copy left eyelash")) {
                beauty.RightCurve.keys = beauty.LeftCurve.keys;
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
            if (_artsAndBundleStatus.Bundles.Count <= 0)
                return;
            GUILayout.Label("Filters being processed", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter name");
            GUILayout.Label("Queue number");
            GUILayout.EndHorizontal();
            foreach (KeyValuePair<string, Bundle> bundle in _artsAndBundleStatus.Bundles) {
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
            _adminScrollPosition = GUILayout.BeginScrollView(_adminScrollPosition);
            foreach (ArtMeta item in _artMetas) {
                bool clicked = GUILayout.Button($"{item.title} v{item.version}");
                if (clicked) {
                    GetUnityPackageUrl(item);
                }
            }
            GUILayout.EndScrollView();
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
            _uploaderScrollPosition = GUILayout.BeginScrollView(_uploaderScrollPosition);

            if (Authentication.Instance.IsLoggedIn) {
                if (_selectedArtKey != "") {
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
            if (String.IsNullOrEmpty(_selectedArtKey)) {
                Debug.LogError("Error uploading! selectedArtKey is empty. Please report this bug");
                return;
            }
            string buttonTitle = _selectedArtKey == TempSelectedArtKey ? "Upload new filter to Filta" : "Update your filta";
            bool assetBundleButton = GUILayout.Button(buttonTitle);
            if (!assetBundleButton) { return; }
            if (!await EnsureUnexpiredLogin()) {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Error", "You cannot complete this task while in Play Mode. Please leave Play Mode", "Ok");
                return;
            }

            if (_selectedArtKey != TempSelectedArtKey && _artsAndBundleStatus.Bundles.ContainsKey(_selectedArtKey)) {
                //Only allows uploading of filter if a version is NOT currently being bundled or if it is in Limbo
                //Limbo is when the filter has been successfully uploaded to cloud storage, but something has stopped it from being processed by assetbundler
                var bundle = _artsAndBundleStatus.Bundles[_selectedArtKey];
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
                
                bool success = Util.GenerateFilterPrefab(filterObject, VariantTempSave);
                if (success) {
                    AssetImporter.GetAtPath(VariantTempSave).assetBundleName =
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


            AssetDatabase.ExportPackage(_packagePaths, "asset.unitypackage",
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), "asset.unitypackage");
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > FilterSizeWindow.UploadLimit) {
                string readout = FilterSizeWindow.CheckForFileSizes(_packagePaths);
                EditorUtility.DisplayDialog("Error",
                    $"Your Filter is {fileInfo.Length / 1000000f:#.##}MB. This is over the {FilterSizeWindow.UploadLimit / 1000000}MB limit.\n{readout}",
                    "Ok");
                return;
            }

            SetStatusMessage("Connecting... (2/5)");
            byte[] bytes = File.ReadAllBytes(pathToPackage);
            Hash128 hash = Hash128.Compute(bytes);
            string uploadResultKey = await Backend.Instance.Upload(_selectedArtKey, _selectedArtTitle, hash, bytes);
            await RefreshArtsAndBundleStatus();
            if (uploadResultKey != null) {
                _selectedArtKey = uploadResultKey;
                SetStatusMessage("Upload successful. Processing... (4/5)");
            }
            AssetDatabase.DeleteAsset(VariantTempSave);
            Backend.Instance.LogAnalyticsEvent("upload_filta", new AnalyticsEventParam() { name = "art_id", value = _selectedArtKey } );
        }
        
        
        #endregion
        
        #region Util
        
        private async void HandleSceneChange(Scene oldScene, Scene newScene) {
            FindSimulator(PlayModeStateChange.EnteredEditMode);
            SetPluginInfo();
            //Wait a second to ensure scene is set up properly.
            await Task.Delay(1000);
            HotKeys.FocusSceneViewCamera();
        }
        
        private void HandleStatusChange(object sender, StatusChangeEventArgs e) {
            SetStatusMessage(e.Message, e.IsError);
        }
        
        private static string GetVersionNumber() {
            ReleaseInfo releaseInfo = GetLocalReleaseInfo();
            return $"v{releaseInfo.version.pluginAppVersion}.{releaseInfo.version.pluginMajorVersion}.{releaseInfo.version.pluginMinorVersion}";
        }

        private static void UpdatePanel(string version) {
            ScopedRegistry filtaRegistry = new ScopedRegistry {
                name = RegistryName,
                url = RegistryUrl,
                scopes = new[] {
                    RegistryScope
                }
            };
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages/manifest.json");
            string manifestJson = File.ReadAllText(manifestPath);
            ManifestJson manifest = JsonConvert.DeserializeObject<ManifestJson>(manifestJson);
            if (manifest.scopedRegistries.FindIndex((registry => registry.url == RegistryUrl)) == -1) {
                manifest.scopedRegistries.Add(filtaRegistry);
            }

            if (manifest.dependencies.ContainsKey(RegistryScope)) {
                manifest.dependencies[RegistryScope] = version;
            }
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            UnityEditor.PackageManager.Client.Resolve();
            GetLocalReleaseInfo();
        }
        
        public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        
        private void SetButtonColor(bool isRed) {
            _normalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = isRed ? Color.magenta : Color.green;
        }

        private void ResetButtonColor() {
            GUI.backgroundColor = _normalBackgroundColor;
        }

        private bool _isUpdating;
        
        private void HandleNewPluginVersion() {
            if (_masterReleaseInfo == null || _masterReleaseInfo.Count == 0 || _localReleaseInfo == null) {
                return;
            }

            if (_masterReleaseInfo[^1].version.ToInt() > _localReleaseInfo.version.ToInt()) {
                DisplayReleaseNotes();
                if (_isUpdating) {
                    GUI.enabled = false;
                }
                Version version = _masterReleaseInfo[^1].version;
                string versionString =
                    $"{version.pluginAppVersion}.{version.pluginMajorVersion}.{version.pluginMinorVersion}";
                if (GUILayout.Button("Get latest plugin version")) {
                    _isUpdating = true;
                    SetStatusMessage("Updating plugin! Please wait a while");
                    UpdatePanel(versionString);
                    _isUpdating = false;
                    SetStatusMessage("Plugin Update Complete!");
                }

                GUI.enabled = true;
                DrawUILine(Color.grey);
                EditorGUILayout.Space();
            }
        }

        Vector2 _scrollPos;
        void DisplayReleaseNotes() {
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(100));
            for (int i = _masterReleaseInfo.Count - 1; i >= 0; i--) {
                ReleaseInfo masterInfo = _masterReleaseInfo[i];
                Version masterVersion = masterInfo.version;
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
            string data = File.ReadAllText($"{PackagePath}/package.json");
            JObject jsonResult = JObject.Parse(data);
            JToken? releaseLogs = jsonResult["release"];
            if (releaseLogs == null) {
                Debug.LogError("Could not find release logs");
                return null;
            }

            ReleaseInfo localInfo = releaseLogs.ToObject<ReleaseInfo>();
            return localInfo;
        }
        
        private void SetStatusMessage(string message, bool isError = false) {
            _s.normal.textColor = isError ? Color.red : Color.white;
            StatusBar = message;
        }
        
        #endregion

        #region Filter/Scene Functionality
        
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
            string scenePath = $"{PackagePath}/Core/templateScene.unity";
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
                if (rootObjects[i] == sim || rootObjects[i] == filter) {
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
            if (_artsAndBundleStatus == null || _artsAndBundleStatus.Bundles == null ||
                _artsAndBundleStatus.Bundles.Count == 0) {
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
            _artsAndBundleStatus = new ArtsAndBundleStatus();
            _artsAndBundleStatus = await Backend.Instance.GetArtsAndBundleStatus();
            Repaint();
        }
        
        private void PrivateCollection() {
            if (!String.IsNullOrEmpty(SceneManager.GetActiveScene().name)) {
                EditorGUILayout.LabelField("Choose the Filta upload to update:", EditorStyles.boldLabel);
                bool newClicked = GUILayout.Button("CREATE NEW FILTA UPLOAD");
                EditorGUILayout.Space();
                if (newClicked) {
                    _selectedArtTitle = SceneManager.GetActiveScene().name;
                    _selectedArtKey = TempSelectedArtKey;
                }
                if (_artsAndBundleStatus == null || _artsAndBundleStatus.ArtMetas.Count < 1) { return; }

                _showPrivCollection = EditorGUILayout.Foldout(_showPrivCollection, "Private Filta Collection");
                if (!_showPrivCollection)
                    return;
                foreach (var item in _artsAndBundleStatus.ArtMetas) {
                    bool clicked = GUILayout.Button(item.Value.title);
                    if (clicked) {
                        _selectedArtTitle = item.Value.title;
                        _selectedArtKey = item.Key;
                    }
                }
            }
        }

        private void GoToPublishingPage() {
            if (_selectedArtKey == TempSelectedArtKey) {
                return;
            }
            if (GUILayout.Button("Go to publishing page")) {
                Application.OpenURL($"{PublishPageLink}?id={_selectedArtKey}");
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
                _artsAndBundleStatus.ArtMetas.Remove(_selectedArtKey);
                _selectedArtKey = "";
            }
        }

        private void SelectedArt() {
            if (GUILayout.Button("Back")) {
                _selectedArtKey = "";
                EditorGUI.FocusTextInControl(null);
                return;
            }

            EditorGUILayout.Space();
            _selectedArtTitle = (string)EditorGUILayout.TextField("Title", _selectedArtTitle);
            EditorGUILayout.Space();
            GenerateAndUploadAssetBundle();
            GoToPublishingPage();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DeletePrivArt(_selectedArtKey);
        }
        
        private bool CheckForUnreadableMeshes(GameObject filterParent) {
            bool result = false;
            string dialog = "All meshes used with SkinnedMeshRenderers must be marked as readable. Select the mesh(es) and set Read/Write to true in the Inspector. \n \n List of affected gameObjects: ";
            SkinnedMeshRenderer[] skinnedMeshRenderers = filterParent.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++) {
                if (skinnedMeshRenderers[i] == null || skinnedMeshRenderers[i].sharedMesh == null) {
                    continue;
                }
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
                _selectedArtKey = "";
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