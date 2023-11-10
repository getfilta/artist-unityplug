using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Collections;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Simulator : SimulatorBase {
    private static readonly int[] LeftEyeVerts = {
        1101, 1102, 1100, 1103, 1099, 1104, 1098, 1105, 1097, 1106, 1096, 1107, 1095, 1108, 1094, 1085, 1093, 1086,
        1092, 1087, 1091, 1088, 1090, 1089
    };

    private static readonly int[] RightEyeVerts = {
        1081, 1082, 1080, 1083, 1079, 1084, 1078, 1061, 1077, 1062, 1076, 1063, 1075, 1064, 1074, 1065, 1073, 1066,
        1072, 1067, 1071, 1068, 1070, 1069
    };

    private static readonly int[] MouthVerts = {
        249, 404, 393, 305, 250, 248, 251, 247, 252, 275, 253, 290, 254, 274, 255, 265, 256, 25, 24, 700, 691, 709, 690,
        725, 689, 710, 688, 682, 687, 683, 686, 740, 685, 834, 823, 684
    };

    private List<int> _faceIndicesWithCovering;
     
    public EventHandler<UpdateBlendShapeWeightEventArgs> updateBlendShapeWeightEvent = delegate { };
    public EventHandler<FaceData.Trans> onFaceUpdate;
    public EventHandler onFaceAdd;
    public EventHandler onFaceRemove;
    public Animator zooPalAnimator;

#if UNITY_EDITOR
    public override SimulatorType _simulatorType => SimulatorType.Face;

    private long _recordingLength;

    private FaceRecording _faceRecording;

    [FormerlySerializedAs("faceMeshVisualiser"), SerializeField]
    private GameObject _faceMeshVisualiser;

    [SerializeField]
    private GameObject _faceSampler;

    private MeshFilter _meshFilter;
    private MeshFilter _sampleMeshFilter;

    [SerializeField]
    private Transform nonArObject;
    [SerializeField]
    private Transform arObject;

    [SerializeField]
    private Transform twister;

    [SerializeField]
    private GameObject background;

    private Vector3 initTwisterPos;
    private Quaternion initTwisterRot;

    [FormerlySerializedAs("visualiserOffset"), SerializeField]
    private float _visualiserOffset;

    [FormerlySerializedAs("faceTracker"), Header("Face Trackers")]
    [SerializeField]
    private Transform _faceTracker;

    [FormerlySerializedAs("leftEyeTracker"), SerializeField]
    private Transform _leftEyeTracker;

    [FormerlySerializedAs("rightEyeTracker"), SerializeField]
    private Transform _rightEyeTracker;

    [FormerlySerializedAs("noseBridgeTracker"), SerializeField]
    private Transform _noseBridgeTracker;

    [FormerlySerializedAs("faceMaskHolder"), SerializeField]
    private Transform _faceMaskHolder;

    [FormerlySerializedAs("facesHolder"), SerializeField]
    private Transform _facesHolder;
    
    public Transform leftEyelashHolder;
    
    public Transform rightEyelashHolder;

    public Transform lipsHolder;

    [SerializeField]
    private Transform _vertices;
    //public SkinnedMeshRenderer faceMask;

    [SerializeField]
    private Mesh bounds;

    [NonSerialized]
    public bool showVertexNumbers;

    [NonSerialized]
    public bool showFaceMeshVisualiser;

    [NonSerialized]
    public bool fillFaceMesh;
    
    [NonSerialized]
    public RenderTexture _faceTexture;

    private long _prevTime;
    private int _previousFrame;
    private bool _skipFaceSimulator;
    private bool _skipFaceRecording;

    private FaceData.FaceMesh _faceMesh;

    private DataSender _dataSender;
    private readonly float _coefficientScale = 100f;
    
    private Texture2D _stencilTex;

    private Cloth[] _cloths;
    private bool _clearedInitialTransform;
    private Material _screenSamplerMat;
    
    private static readonly int CameraMatrix = Shader.PropertyToID("_Matrix");
    private static readonly int ScreenSize = Shader.PropertyToID("_Screen");

    private bool _restarted;
    public Beauty beauty;
    
    private Vector3[] _leftEyeVertices;
    private Vector3[] _rightEyeVertices;
    
    public const int EyelashVertexCount = 11;

    private const int LeftEyelashStart = 1182;
    private const int RightEyelashStart = 1179;
    [NonSerialized]
    public bool isAr;

    protected override void Awake() {
        base.Awake();
        mesh = new Mesh();
        TryAutomaticSetup();
        GetSkinnedMeshRenderers();
        _faceMeshes = _facesHolder.GetComponentsInChildren<MeshFilter>().ToList();
        _skinnedFaceMeshes = new List<SkinnedMeshRenderer>(_faceMeshes.Count);
        for (int i = 0; i < _faceMeshes.Count; i++) {
            _skinnedFaceMeshes.Add(_faceMeshes[i].GetComponent<SkinnedMeshRenderer>());
        }

        _leftEyeVertices = new Vector3[EyelashVertexCount];
        _rightEyeVertices = new Vector3[EyelashVertexCount];
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (EditorApplication.isPlaying) {
            if (_filterObject != null) {
                _cloths = _filterObject.GetComponentsInChildren<Cloth>();
            }
        }
        PopulateVertexTrackers();
        EditorApplication.hierarchyChanged += GetSkinnedMeshRenderers;
        EditorApplication.hierarchyChanged += GetFaceMeshFilters;
        TryGetPetObjects();
    }

    protected override void OnDisable() {
        base.OnDisable();
        EditorApplication.hierarchyChanged -= GetSkinnedMeshRenderers;
        EditorApplication.hierarchyChanged -= GetFaceMeshFilters;
    }

    public override void Disable() {
        base.Disable();
        previousVisStatus = showFaceMeshVisualiser;
        showFaceMeshVisualiser = false;
        PauseSimulator();
    }

    public override void Enable() {
        base.Enable();
        showFaceMeshVisualiser = previousVisStatus;
        ResumeSimulator();
    }

    public override void ResumeSimulator() {
        base.ResumeSimulator();
        isAr = true;
    }

    #region Pets

    private void TryGetPetObjects() {
        if (_filterObject == null) {
            _filterObject = GameObject.Find("Filter").transform;
        }

        if (nonArObject == null) {
            nonArObject = GameObject.Find("NonArObject").transform;
        }
        
        if (arObject == null) {
            arObject = GameObject.Find("ArObject").transform;
        }

        if (twister == null && _filterObject) {
            twister = GameObject.Find("Twister").transform;
        }

        if (twister != null) {
            zooPalAnimator = twister.GetComponentInChildren<Animator>();
        }

        if (_filterObject != null) {
            Canvas[] canvases = _filterObject.GetComponentsInChildren<Canvas>();
            if (canvases is {Length: > 0}) {
                background = canvases[0].gameObject;
            }
        }
    }
    
    public void ToggleAr(bool forceNonAr = false) {
        if (twister == null) {
            Debug.Log("No Twister Object");
            return;
        }
        if (forceNonAr) {
            isAr = true;
        }
        if (isAr) {
            StopSimulator();
            mainCamera.transform.position = Vector3.back;
            mainCamera.transform.rotation = Quaternion.identity;
            if (nonArObject != null && nonArObject.childCount > 0) {
                twister.SetParent(nonArObject.GetChild(0), false);
            }

            if (background != null) {
                background.SetActive(true);
            }

            mainCamera.fieldOfView = NonArCameraFov;
            isAr = false;
        } else {
            isAr = true;
            if (arObject != null && arObject.childCount > 0) {
                twister.SetParent(arObject.GetChild(0), false);
            }
            if (background != null) {
                background.SetActive(false);
            }
            mainCamera.fieldOfView = ArCameraFov;
            ResumeSimulator();
        }
    }

    #endregion
    

    public override void TryAutomaticSetup() {
        if (IsSetUpProperly()) {
            beauty.Initialize();
            SetFlags();
            return;
        }

        SetFlags(true);
        if (beauty == null) {
            beauty = GetComponent<Beauty>();
        }
        if (_videoFeed != null) {
            _canvas = _videoFeed.GetComponentInParent<Canvas>();
            _canvas.worldCamera = mainCamera;
        }

        if (_faceMeshVisualiser == null) {
            _faceMeshVisualiser = transform.Find("FaceVisualiser").gameObject;
        }

        if (_faceMeshVisualiser != null) {
            _meshFilter = _faceMeshVisualiser.GetComponent<MeshFilter>();
        }
        
        if (_faceSampler == null) {
            _faceSampler = transform.Find("FaceSampler").gameObject;
        }

        if (_faceSampler != null) {
            _sampleMeshFilter = _faceSampler.GetComponent<MeshFilter>();
        }

        if (_filterObject == null) {
            _filterObject = GameObject.Find("Filter").transform;
        }

        if (_filterObject != null) {
            if (_faceTracker == null) {
                _faceTracker = _filterObject.Find("FaceTracker");
            }
            mainTracker = _faceTracker;
        }

        if (_faceTracker != null) {
            if (_rightEyeTracker == null) {
                _rightEyeTracker = _faceTracker.Find("RightEyeTracker");
            }

            if (_leftEyeTracker == null) {
                _leftEyeTracker = _faceTracker.Find("LeftEyeTracker");
            }

            if (_noseBridgeTracker == null) {
                _noseBridgeTracker = _faceTracker.Find("NoseBridgeTracker");
            }

            if (_facesHolder == null) {
                _facesHolder = _faceTracker.Find("Faces");
            }

            if (_facesHolder == null) {
                _facesHolder = _faceTracker.Find("FaceMeshes");
            }

            if (_faceMaskHolder == null) {
                _faceMaskHolder = _faceTracker.Find("FaceMasks");
            }
            
            if (_faceMaskHolder == null) {
                _faceMaskHolder = _faceTracker.Find("FaceBlendshapes");
            }

            if (_vertices == null) {
                _vertices = _faceTracker.Find("Vertices");
            }

            if (leftEyelashHolder == null) {
                leftEyelashHolder = _faceTracker.Find("LeftEyelashHolder");
            }

            if (rightEyelashHolder == null) {
                rightEyelashHolder = _faceTracker.Find("RightEyelashHolder");
            }

            if (lipsHolder == null) {
                lipsHolder = _faceTracker.Find("LipsHolder");
            }
            beauty.Initialize();
        }

        SetFlags();
        if (IsSetUpProperly()) {
            _skipFaceSimulator = false;
            Debug.Log("Successfully Set up");
        } else {
            _skipFaceSimulator = true;
            Debug.LogError("Failed to set up simulator");
        }
    }

    public override bool IsSetUpProperly() {
        return _filterObject != null && mainTracker != null && _faceMeshVisualiser != null && _meshFilter != null && _faceSampler != null && _sampleMeshFilter != null && _faceTracker != null &&
               _leftEyeTracker != null &&
               _rightEyeTracker != null && _noseBridgeTracker != null && _faceMaskHolder != null &&
               _facesHolder != null && _vertices != null && leftEyelashHolder != null && rightEyelashHolder != null && lipsHolder != null && _canvas != null && _canvas.worldCamera != null && beauty != null;
    }

    private bool HasRecordingData() {
        return _faceRecording.faceDatas != null && _faceRecording.faceDatas.Count != 0 && _cameraFeed != null &&
               _faceTexture != null && _stencilRT != null;
    }

    //Update function is used here to ensure the simulator runs every frame in Edit mode. if not, an alternate method that avoids the use of Update would have been used.
    protected override void Update() {
        if (_skipFaceSimulator) {
            return;
        }

        if (!IsSetUpProperly()) {
            Debug.LogError(
                "The simulator object is not set up properly. Try clicking the Automatically Set Up button in the Dev Panel");
            _skipFaceSimulator = true;
            return;
        }

        UpdateSamplerMatrix();
        _faceMeshVisualiser.SetActive(showFaceMeshVisualiser);
        defaultLight.SetActive(dynamicLightOn);
        EnforceObjectStructure();
        if (!HasRecordingData() && !_skipFaceRecording) {
            try {
                GetRecordingData();
            }
            catch (Exception e) {
                Debug.LogError($"Could not get recorded face data. {e.Message}");
                _skipFaceRecording = true;
            }
        }

        if (!isPlaying) {
            _startTime = DateTime.Now;
            beauty.HandlePlayback();
            return;
        }

        if (!EditorApplication.isPlaying) {
            long time = (long)(DateTime.Now - _startTime).TotalMilliseconds + _pauseTime;
            Playback(time);
            return;
        }

        if (!NetworkClient.isConnected) {
            long time = (long)(DateTime.Now - _startTime).TotalMilliseconds + _pauseTime;
            Playback(time);
        } else {
            if (_dataSender == null) {
                _dataSender = FindObjectOfType<DataSender>();
            } else {
                _dataSender.SendSimulatorType((int)_simulatorType);
                PlaybackFromRemote();
            }
        }
    }

    private void GetRecordingData() {
        byte[] data = File.ReadAllBytes(Path.Combine(_filePath, "Editor/Simulator/FaceRecording"));
        string faceData = Encoding.ASCII.GetString(data);
        _faceRecording = JsonConvert.DeserializeObject<FaceRecording>(faceData);
        _recordingLength = _faceRecording.faceDatas[^1].timestamp;
        _tex = new Texture2D(_faceRecording.videoWidth, _faceRecording.videoHeight, TextureFormat.ARGB32, false);
        _stencilTex = new Texture2D(_faceRecording.stencilWidth, _faceRecording.stencilHeight, TextureFormat.RGBA32,
            false);
        _stencilRT = AssetDatabase.LoadAssetAtPath<RenderTexture>($"{PackagePath}/Assets/Textures/BodySegmentationStencil.renderTexture");
        _cameraFeed = AssetDatabase.LoadAssetAtPath<RenderTexture>($"{PackagePath}/Assets/Textures/CameraFeed.renderTexture");
        _faceTexture =
            AssetDatabase.LoadAssetAtPath<RenderTexture>($"{PackagePath}/Assets/Textures/FaceTexture.renderTexture");
        _screenSamplerMat = AssetDatabase.LoadAssetAtPath<Material>($"{PackagePath}/Core/materials/ScreenSpace.mat");
        VideoSender._cameraFeed = _cameraFeed;
    }

    //added Y-offset because text labels are rendered below the actual point specified.
    //seems to be a Unity 2021.2 issue/change
    private float _offsetY = -45;

    private void OnDrawGizmos() {
        if (showVertexNumbers) {
            GUIStyle handleStyle = new GUIStyle();
            handleStyle.alignment = TextAnchor.MiddleCenter;
            handleStyle.normal.textColor = Color.white;
            //Y offset is only needed for OSX
#if UNITY_EDITOR_OSX
            handleStyle.contentOffset = new Vector2(0, _offsetY);
#endif
            Handles.matrix = _faceMeshVisualiser.transform.localToWorldMatrix;
            for (int i = 0; i < _faceMesh.vertices.Count; i++) {
                Handles.Label(_faceMesh.vertices[i], i.ToString(), handleStyle);
            }
        }
    }

    void UpdateSamplerMatrix() {
        if (_screenSamplerMat == null || mainCamera == null) {
            return;
        }

        Vector2 gameView = GameViewUtils.GetMainGameViewSize();
        Vector4 screenSize = new Vector4(gameView.x, gameView.y, 1 + 1 / gameView.x, 1 + 1 / gameView.y);
        Matrix4x4 vp = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;
        Matrix4x4 matrix = vp * _faceSampler.transform.localToWorldMatrix;
        _screenSamplerMat.SetMatrix(CameraMatrix, matrix);
        _screenSamplerMat.SetVector(ScreenSize, screenSize);
    }

    void PositionTrackers(FaceData faceData) {
        onFaceUpdate?.Invoke(this, faceData.face);
        _faceTracker.localPosition = faceData.face.localPosition;
        _faceTracker.localEulerAngles = faceData.face.localRotation;
        _leftEyeTracker.localPosition = faceData.leftEye.localPosition;
        _leftEyeTracker.localEulerAngles = faceData.leftEye.localRotation;
        _rightEyeTracker.localPosition = faceData.rightEye.localPosition;
        _rightEyeTracker.localEulerAngles = faceData.rightEye.localRotation;
        Vector3 noseBridgePosition = _leftEyeTracker.localPosition +
                                     (_rightEyeTracker.localPosition - _leftEyeTracker.localPosition) / 2;
        _noseBridgeTracker.localPosition = noseBridgePosition;
        _noseBridgeTracker.localEulerAngles = faceData.face.localRotation;
        mainCamera.transform.position = faceData.camera.position;
        mainCamera.transform.eulerAngles = faceData.camera.rotation;
    }
    
    void PositionTrackers(DataSender.FaceData faceData) {
        onFaceUpdate?.Invoke(this,
            new FaceData.Trans {localPosition = faceData.facePosition, localRotation = faceData.faceRotation});
        _faceTracker.localPosition = faceData.facePosition;
        _faceTracker.localEulerAngles = faceData.faceRotation;
        _leftEyeTracker.localPosition = faceData.leftEyePosition;
        _leftEyeTracker.localEulerAngles = faceData.leftEyeRotation;
        _rightEyeTracker.localPosition = faceData.rightEyePosition;
        _rightEyeTracker.localEulerAngles = faceData.rightEyeRotation;
        Vector3 noseBridgePosition = _leftEyeTracker.localPosition +
                                     (_rightEyeTracker.localPosition - _leftEyeTracker.localPosition) / 2;
        _noseBridgeTracker.localPosition = noseBridgePosition;
        _noseBridgeTracker.localEulerAngles = faceData.faceRotation;
        mainCamera.transform.position = faceData.cameraPosition;
        mainCamera.transform.eulerAngles = faceData.cameraRotation;
        
        //Clear cloth transform motion when artist remote finds face.
        //Eventually artist remote might send events to better handle this.
        if (!_clearedInitialTransform && faceData.facePosition != Vector3.zero) {
            if (_cloths is {Length: > 0}) {
                for (int i = 0; i < _cloths.Length; i++) {
                    _cloths[i].ClearTransformMotion();
                }
            }
            _clearedInitialTransform = true;
        }
    }

    protected override void EnforceObjectStructure() {
        _faceMeshVisualiser.name = "FaceVisualiser";
        _faceSampler.name = "FaceSampler";
        _faceTracker.name = "FaceTracker";
        _leftEyeTracker.name = "LeftEyeTracker";
        _rightEyeTracker.name = "RightEyeTracker";
        _noseBridgeTracker.name = "NoseBridgeTracker";
        _faceMaskHolder.name = "FaceBlendshapes";
        _facesHolder.name = "FaceMeshes";
        _vertices.name = "Vertices";
        leftEyelashHolder.name = "LeftEyelashHolder";
        rightEyelashHolder.name = "RightEyelashHolder";
        lipsHolder.name = "LipsHolder";
        gameObject.name = "Simulator";
        _filterObject.name = "Filter";
        _filterObject.position = Vector3.zero;
        _filterObject.rotation = Quaternion.identity;
        _filterObject.localScale = Vector3.one;
    }

    public override void ResetSimulator() {
        base.ResetSimulator();
        Playback(0);
    }

    void PlaybackFromRemote() {
        _faceMeshVisualiser.transform.localPosition = _dataSender._data.facePosition;
        _faceMeshVisualiser.transform.localEulerAngles = _dataSender._data.faceRotation;
        _faceMeshVisualiser.transform.position -= _faceMeshVisualiser.transform.forward * _visualiserOffset;
            
        _faceSampler.transform.localPosition = _dataSender._data.facePosition;
        _faceSampler.transform.localEulerAngles = _dataSender._data.faceRotation;
        PositionTrackers(_dataSender._data);
        SetMeshTopology(_dataSender._data);
        UpdateMasks(_dataSender._data);
        HandleVertexPairing(_dataSender._data.vertices.ToList());
    }

    protected override void Playback(long currentTime) {
        if (_recordingLength <= 0) {
            return;
        }

        if (_restarted && currentTime > 500f) {
            _restarted = false;
            onFaceAdd?.Invoke(this, null);
        }

        if (currentTime > _recordingLength) {
            _restarted = true;
            onFaceRemove?.Invoke(this, null);
            _startTime = DateTime.Now;
            _pauseTime = 0;
            return;
        }

        if (_prevTime > currentTime) {
            _previousFrame = 0;
        }

        _prevTime = currentTime;

        for (int i = _previousFrame; i < _faceRecording.faceDatas.Count; i++) {
            FaceData faceData = _faceRecording.faceDatas[i];
            _faceMesh = faceData.faceMesh;
            long nextTimeStamp = faceData.timestamp;
            List<ARKitBlendShapeCoefficient> nextBlendShape = faceData.blendshapeData;

            //we want to find the timestamp in the future so we can walk back a frame and interpolate
            if (nextTimeStamp < currentTime) {
                if (i == _faceRecording.faceDatas.Count - 1) {
                    i = 0;
                    break;
                }

                //we haven't found the future yet. try the next one.
                continue;
            }

            var frameOffset = 1;

            if (i == 0) {
                frameOffset = 0;
            }

            if (_videoFeed != null) {
                _tex.LoadImage(faceData.video);
                _tex.Apply();
                _videoFeed.texture = _tex;
                if (_cameraFeed != null) {
                    RenderTexture.active = _cameraFeed;
                    Graphics.Blit(_tex, _cameraFeed);
                }
            }

            if (_stencilRT != null) {
                _stencilTex.LoadImage(faceData.humanSegStencil);
                _stencilTex.Apply();
                RenderTexture.active = _stencilRT;
                Graphics.Blit(_stencilTex, _stencilRT);
            }

            _faceMeshVisualiser.transform.localPosition = faceData.face.localPosition;
            _faceMeshVisualiser.transform.localEulerAngles = faceData.face.localRotation;
            _faceMeshVisualiser.transform.position -= _faceMeshVisualiser.transform.forward * _visualiserOffset;
            
            _faceSampler.transform.localPosition = faceData.face.localPosition;
            _faceSampler.transform.localEulerAngles = faceData.face.localRotation;
            
            SetMeshTopology();
            PositionTrackers(faceData);
            HandleVertexPairing();
            FaceData prevFaceData = _faceRecording.faceDatas[i - frameOffset];
            UpdateMasks(faceData, prevFaceData, currentTime);

            //Logic to implement blendshape manipulation

            /*FaceRecordingData.FaceData prevFaceData = faceRecordingData.faceRecording.faceDatas[i - 1];
            long prevTimeStamp = prevFaceData.timestamp;
            float[] prevBlendShape = prevFaceData.blendshapeData;
            float nextWeight = (float) (currentTime - prevTimeStamp) / (nextTimeStamp - prevTimeStamp);
            float prevWeight = 1f - nextWeight;
            faceMask.transform.localPosition = faceData.face.localPosition;
            faceMask.transform.localEulerAngles = faceData.face.localRotation;
            faceMask.transform.position -= faceMask.transform.forward * visualiserOffset;
            
            //now to grab the blendshape values of the prev and next frame and lerp + assign them
            for (int j = 0; j < prevBlendShape.Length - 2; j++){
                var nowValue = (prevBlendShape[j] * prevWeight) + (nextBlendShape[j] * nextWeight);
                faceMask.SetBlendShapeWeight(j, nowValue);
            }*/

            _previousFrame = i;
            break;
        }
    }



    #region Face Mask Control

    private List<SkinnedMeshRenderer> _faceMasks;
    private List<FaceBlendShapeSet> _faceBlendShapeSets;

    private int _maskCount;

    private void GetSkinnedMeshRenderers() {
        if (_maskCount == _faceMaskHolder.childCount) {
            return;
        }

        _faceMasks = _faceMaskHolder.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        string[] arKitNames = Enum.GetNames(typeof(ARKitBlendShapeLocation));
        _faceBlendShapeSets = new List<FaceBlendShapeSet>();
        for (int i = 0; i < _faceMasks.Count; i++) {
            Dictionary<string, int> indices = new Dictionary<string, int>();
            Mesh sharedMesh = _faceMasks[i].sharedMesh;
            for (int j = 0; j < sharedMesh.blendShapeCount; j++) {
                string blendShapeName = sharedMesh.GetBlendShapeName(j);
                for (int k = 0; k < arKitNames.Length; k++) {
                    if (blendShapeName.ToLower().Contains(arKitNames[k].ToLower())) {
                        indices.Add(arKitNames[k], j);
                        break;
                    }
                }
            }
            _faceBlendShapeSets.Add(new FaceBlendShapeSet{smr = _faceMasks[i], indices = indices});
        }
        _maskCount = _faceMaskHolder.childCount;
    }

    private void UpdateMasks(DataSender.FaceData faceData) {
        if (faceData.blendshapeData == null || faceData.blendshapeData.Count <= 0) {
            return;
        }

        for (int j = 0; j < faceData.blendshapeData.Count - 2; j++) {
            float nowValue = faceData.blendshapeData[j].coefficient * _coefficientScale;
            updateBlendShapeWeightEvent(this,
                new UpdateBlendShapeWeightEventArgs(faceData.blendshapeData[j].blendShapeLocation, nowValue));
            if (_faceMasks == null || _faceMasks.Count == 0) {
                continue;
            }

            for (int i = 0; i < _faceBlendShapeSets.Count; i++) {
                if (_faceBlendShapeSets[i] != null) {
                    if (_faceBlendShapeSets[i].indices
                        .TryGetValue(faceData.blendshapeData[j].blendShapeLocation.ToString(), out int index)) {
                        _faceBlendShapeSets[i].smr.SetBlendShapeWeight(index, faceData.blendshapeData[j].coefficient * _coefficientScale);
                    }
                }

            }
        }
    }

    private void UpdateMasks(FaceData faceData, FaceData prevFaceData, long currentTime) {
        long nextTimeStamp = faceData.timestamp;
        List<ARKitBlendShapeCoefficient> nextBlendShape = faceData.blendshapeData;
        long prevTimeStamp = prevFaceData.timestamp;
        List<ARKitBlendShapeCoefficient> prevBlendShape = prevFaceData.blendshapeData;
        float nextWeight = (float)(currentTime - prevTimeStamp) / (nextTimeStamp - prevTimeStamp);
        float prevWeight = 1f - nextWeight;

        //now to grab the blendshape values of the prev and next frame and lerp + assign them
        for (int j = 0; j < prevBlendShape.Count - 2; j++) {
            float nowValue = (prevBlendShape[j].coefficient * prevWeight) +
                             (nextBlendShape[j].coefficient * nextWeight);
            nowValue *= _coefficientScale;
            updateBlendShapeWeightEvent(this,
                new UpdateBlendShapeWeightEventArgs(prevBlendShape[j].blendShapeLocation, nowValue));
            if (_faceMasks == null || _faceMasks.Count == 0) {
                continue;
            }
            
            for (int i = 0; i < _faceBlendShapeSets.Count; i++) {
                if (_faceBlendShapeSets[i] != null) {
                    if (_faceBlendShapeSets[i].indices
                        .TryGetValue(prevBlendShape[j].blendShapeLocation.ToString(), out int index)) {
                        _faceBlendShapeSets[i].smr.SetBlendShapeWeight(index, nowValue);
                    }
                }

            }
        }
    }

    #endregion

    #region Face Mesh Control

    private List<MeshFilter> _faceMeshes;
    private List<SkinnedMeshRenderer> _skinnedFaceMeshes;

    private int _faceCount;

    public GameObject SpawnNewFaceMesh() {
        GameObject newFace = GameObject.CreatePrimitive(PrimitiveType.Plane);
        newFace.name = "FaceMesh";
        Collider col = newFace.GetComponent<Collider>();
        DestroyImmediate(col);
        newFace.transform.parent = _facesHolder;
        newFace.transform.localPosition = Vector3.zero;
        newFace.transform.localRotation = Quaternion.identity;
        newFace.GetComponent<MeshFilter>().sharedMesh = mesh;
        newFace.AddComponent<SkinnedMeshRenderer>();
        GetFaceMeshFilters();
        SetMeshTopology();
        return newFace;
    }

    private void GetFaceMeshFilters() {
        if (_faceCount == _facesHolder.childCount) {
            return;
        }
        
        _faceMeshes = _facesHolder.GetComponentsInChildren<MeshFilter>().ToList();
        _skinnedFaceMeshes = new List<SkinnedMeshRenderer>(_faceMeshes.Count);
        for (int i = 0; i < _faceMeshes.Count; i++) {
            _skinnedFaceMeshes.Add(_faceMeshes[i].GetComponent<SkinnedMeshRenderer>());
        }

        _faceCount = _facesHolder.childCount;
    }

    public Mesh mesh { get; private set; }

    void SetMeshTopology() {
        SetMeshTopology(FaceData.Vector3Converter(_faceMesh.vertices), FaceData.Vector3Converter(_faceMesh.normals),
            FaceData.Vector2Converter(_faceMesh.uvs), _faceMesh.indices);
    }

    void SetMeshTopology(DataSender.FaceData data) {
        if (data.vertices == null || data.vertices.Length <= 0) {
            return;
        }

        SetMeshTopology(data.vertices.ToList(), data.normals.ToList(), data.uvs.ToList(), data.indices.ToList());
    }

    void SetMeshTopology(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices) {
        if (mesh == null) {
            return;
        }

        mesh.Clear();
        if (vertices.Count > 0 && indices.Count > 0) {
            mesh.SetVertices(vertices);
            GetIndicesWithCoverings(indices);
            mesh.SetIndices(fillFaceMesh ? _faceIndicesWithCovering : indices , MeshTopology.Triangles, 0, false);
            mesh.RecalculateBounds();
            if (normals.Count == vertices.Count) {
                mesh.SetNormals(normals);
            } else {
                mesh.RecalculateNormals();
            }

            if (uvs.Count > 0) {
                mesh.SetUVs(0, uvs);
            }
            
            //Adding default blendshape to ensure skinned mesh renderer doesn't show warning notice.
            mesh.AddBlendShapeFrame("Default", 0, vertices.ToArray(), null, null);

            
            if (_meshFilter != null) {
                _meshFilter.sharedMesh = mesh;
            }

            if (_sampleMeshFilter != null) {
                _sampleMeshFilter.sharedMesh = mesh;
            }

            for (int i = 0; i < _faceMeshes.Count; i++) {
                if (_faceMeshes[i] != null) {
                    if (_faceMeshes[i].sharedMesh == null || String.IsNullOrEmpty(_faceMeshes[i].sharedMesh.name)) {
                        _faceMeshes[i].sharedMesh = mesh;
                    }
                }
                if (_skinnedFaceMeshes[i] != null) {
                    if (_faceMeshes[i].sharedMesh == null || String.IsNullOrEmpty(_faceMeshes[i].sharedMesh.name)) {
                        _skinnedFaceMeshes[i].sharedMesh = mesh;
                    }
                }
            }

            if (beauty != null && beauty.lipsActive && beauty.lipsMeshRenderer != null) {
                beauty.lipsMeshRenderer.sharedMesh = mesh;
            }
        }
    }
    
    
    private void GetIndicesWithCoverings(List<int> indices) {
        if (_faceIndicesWithCovering != null && _faceIndicesWithCovering.Count > 0) {
            return;
        }
        int capacity = (LeftEyeVerts.Length - 2) * 3 + (RightEyeVerts.Length - 2) * 3 + (MouthVerts.Length - 2) * 3;
        _faceIndicesWithCovering = new List<int>(capacity + indices.Count);
        List<int> addedIndices = new List<int>(capacity);
        FormTriangles(LeftEyeVerts, addedIndices);
        FormTriangles(RightEyeVerts, addedIndices);
        FormTriangles(MouthVerts, addedIndices);
        _faceIndicesWithCovering.AddRange(indices);
        _faceIndicesWithCovering.AddRange(addedIndices);
    }

    private void FormTriangles(int[] vertexIndices, List<int> addedIndices) {
        bool clock = true;
        for (int i = 2; i < vertexIndices.Length; i++) {
            addedIndices.Add(vertexIndices[i]);
            if (clock) {
                addedIndices.Add(vertexIndices[i - 1]);
                addedIndices.Add(vertexIndices[i - 2]);
            } else {
                addedIndices.Add(vertexIndices[i - 2]);
                addedIndices.Add(vertexIndices[i - 1]);
            }
            clock = !clock;
        }
    }

    #endregion


    #region Vertex Pairing

    [NonSerialized]
    public List<VertexTracker> vertexTrackers;

    void HandleVertexPairing() {
        HandleVertexPairing(FaceData.Vector3Converter(_faceMesh.vertices));
    }

    private void HandleVertexPairing(List<Vector3> vertices) {
        HandleEyelash(vertices);
        if (vertexTrackers == null) {
            return;
        }

        if (vertices == null || vertices.Count <= 0) {
            return;
        }

        for (int i = 0; i < vertexTrackers.Count; i++) {
            VertexTracker vertexTracker = vertexTrackers[i];
            if (vertexTracker.holder == null) {
                vertexTrackers.Remove(vertexTracker);
                // if a vertexTracker is removed, we break out of the loop to avoid throwing an exception.
                // since this loop runs every frame, there is no negative impact.
                break;
            }

            vertexTracker.holder.transform.SetParent(_vertices);
            vertexTracker.holder.name = $"VertexTrackerIndex_{vertexTracker.vertexIndex}";
            if (vertexTracker.vertexIndex < vertices.Count) {
                vertexTracker.holder.transform.localPosition = vertices[vertexTracker.vertexIndex];
            }
        }
    }

    private void HandleEyelash(List<Vector3> vertices) {
        if (vertices == null || vertices.Count <= LeftEyelashStart + EyelashVertexCount + 1) {
            return;
        }

        if (_leftEyeVertices == null || _leftEyeVertices.Length != EyelashVertexCount) {
            _leftEyeVertices = new Vector3[EyelashVertexCount];
        }

        if (_rightEyeVertices == null || _rightEyeVertices.Length != EyelashVertexCount) {
            _rightEyeVertices = new Vector3[EyelashVertexCount];
        }

        //This is used to selected the vertices of the face mesh with which the eyelashes would be generated from
        for (int i = 0; i < EyelashVertexCount; i++) {
            _leftEyeVertices[i] = vertices[LeftEyelashStart + i];
            _rightEyeVertices[i] = vertices[RightEyelashStart - i];
        }
        
        beauty.HandlePlayback(_leftEyeVertices, _rightEyeVertices);
    }

    public GameObject GenerateVertexTracker(int index) {
        GameObject vertex = new GameObject();
        MeshFilter boundsFilter = vertex.AddComponent<MeshFilter>();
        boundsFilter.mesh = bounds;
        VertexTracker vertexTracker = new VertexTracker {vertexIndex = index, holder = vertex};
        vertexTrackers.Add(vertexTracker);
        HandleVertexPairing();
        return vertex;
    }

    private void PopulateVertexTrackers() {
        vertexTrackers = new List<VertexTracker>();
        for (int i = 0; i < _vertices.childCount; i++) {
            string componentName = _vertices.GetChild(i).name;
            if (!componentName.Contains("VertexTrackerIndex_")) {
                continue;
            }
            int ind = componentName.IndexOf('_');
            if (ind == -1) {
                continue;
            }

            string indexString = componentName.Substring(ind + 1);
            if (String.IsNullOrEmpty(indexString)) {
                continue;
            }

            int index = Convert.ToInt32(indexString);
            vertexTrackers.Add(new VertexTracker {vertexIndex = index, holder = _vertices.GetChild(i).gameObject});
        }
    }


    #endregion

#endif

    #region Class/Struct Definition

    public class VertexTracker {
        public int vertexIndex;
        public GameObject holder;
    }

    public class FaceBlendShapeSet {
        public SkinnedMeshRenderer smr;
        public Dictionary<string, int> indices;
    }

    [Serializable]
    public struct FaceData {
        public long timestamp;
        public List<ARKitBlendShapeCoefficient> blendshapeData;
        public FaceMesh faceMesh;
        public Trans face;
        public Trans leftEye;
        public Trans rightEye;
        public Trans camera;
        public byte[] video;
        public byte[] humanSegStencil;

        [Serializable]
        public struct FaceMesh {
            public List<Trans.Vector3Json> vertices;
            public List<Trans.Vector3Json> normals;
            public List<int> indices;
            public List<Trans.Vector2Json> uvs;
        }

        [Serializable]
        public struct Trans {
            public Vector3Json position;
            public Vector3Json rotation;
            public Vector3Json localPosition;
            public Vector3Json localRotation;

            public static implicit operator Trans(Transform trans) {
                return new Trans {
                    position = trans.position,
                    rotation = trans.eulerAngles,
                    localPosition = trans.localPosition,
                    localRotation = trans.localEulerAngles
                };
            }

            [Serializable]
            public struct Vector3Json {
                public float x, y, z;

                public static implicit operator Vector3Json(Vector3 vector) {
                    return new Vector3Json {x = vector.x, y = vector.y, z = vector.z};
                }

                public static implicit operator Vector3(Vector3Json vector) {
                    return new Vector3 {x = vector.x, y = vector.y, z = vector.z};
                }
            }

            [Serializable]
            public struct Vector2Json {
                public float x, y;

                public static implicit operator Vector2Json(Vector2 vector) {
                    return new Vector2Json {x = vector.x, y = vector.y};
                }

                public static implicit operator Vector2(Vector2Json vector) {
                    return new Vector2 {x = vector.x, y = vector.y};
                }
            }
        }

        public static List<Trans.Vector3Json> Vector3Converter(NativeArray<Vector3> nativeArray) {
            List<Trans.Vector3Json> vector3Jsons = new List<Trans.Vector3Json>(nativeArray.Length);
            foreach (Vector3 vector in nativeArray) {
                vector3Jsons.Add(vector);
            }

            return vector3Jsons;
        }

        public static List<Vector3> Vector3Converter(List<Trans.Vector3Json> nativeArray) {
            List<Vector3> vector3 = new List<Vector3>(nativeArray.Count);
            foreach (Trans.Vector3Json vector in nativeArray) {
                vector3.Add(vector);
            }

            return vector3;
        }

        public static List<Trans.Vector2Json> Vector2Converter(NativeArray<Vector2> nativeArray) {
            List<Trans.Vector2Json> vector2Jsons = new List<Trans.Vector2Json>(nativeArray.Length);
            foreach (Vector2 vector in nativeArray) {
                vector2Jsons.Add(vector);
            }

            return vector2Jsons;
        }

        public static List<Vector2> Vector2Converter(List<Trans.Vector2Json> nativeArray) {
            List<Vector2> vector2 = new List<Vector2>(nativeArray.Count);
            foreach (Trans.Vector2Json vector in nativeArray) {
                vector2.Add(vector);
            }

            return vector2;
        }
    }

    [Serializable]
    public struct FaceRecording {
        public List<FaceData> faceDatas;
        public int videoWidth;
        public int videoHeight;
        public int stencilWidth;
        public int stencilHeight;
    }

    [Serializable]
    public struct ARKitBlendShapeCoefficient {
        public ARKitBlendShapeLocation blendShapeLocation;
        public float coefficient;
    }

    public enum ARKitBlendShapeLocation {
        BrowDownLeft,
        BrowDownRight,
        BrowInnerUp,
        BrowOuterUpLeft,
        BrowOuterUpRight,
        CheekPuff,
        CheekSquintLeft,
        CheekSquintRight,
        EyeBlinkLeft,
        EyeBlinkRight,
        EyeLookDownLeft,
        EyeLookDownRight,
        EyeLookInLeft,
        EyeLookInRight,
        EyeLookOutLeft,
        EyeLookOutRight,
        EyeLookUpLeft,
        EyeLookUpRight,
        EyeSquintLeft,
        EyeSquintRight,
        EyeWideLeft,
        EyeWideRight,
        JawForward,
        JawLeft,
        JawOpen,
        JawRight,
        MouthClose,
        MouthDimpleLeft,
        MouthDimpleRight,
        MouthFrownLeft,
        MouthFrownRight,
        MouthFunnel,
        MouthLeft,
        MouthLowerDownLeft,
        MouthLowerDownRight,
        MouthPressLeft,
        MouthPressRight,
        MouthPucker,
        MouthRight,
        MouthRollLower,
        MouthRollUpper,
        MouthShrugLower,
        MouthShrugUpper,
        MouthSmileLeft,
        MouthSmileRight,
        MouthStretchLeft,
        MouthStretchRight,
        MouthUpperUpLeft,
        MouthUpperUpRight,
        NoseSneerLeft,
        NoseSneerRight,
        TongueOut,
    }

    public class UpdateBlendShapeWeightEventArgs : EventArgs {
        public ARKitBlendShapeLocation Location { get; }
        public float Weight { get; }

        public UpdateBlendShapeWeightEventArgs(ARKitBlendShapeLocation location, float weight) {
            Location = location;
            Weight = weight;
        }

        public UpdateBlendShapeWeightEventArgs(DataSender.FaceData.ARKitBlendShapeLocation location, float weight) {
            Location = (ARKitBlendShapeLocation)location;
            Weight = weight;
        }
    }

    #endregion
}
