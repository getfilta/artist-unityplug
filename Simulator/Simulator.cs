using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


[ExecuteAlways]
public class Simulator : MonoBehaviour
{
    private string _filePath;
    
    private long _recordingLength;
    
    private FaceRecording _faceRecording;

    [FormerlySerializedAs("filterObject"),SerializeField]
    private Transform _filterObject;
    [FormerlySerializedAs("faceMeshVisualiser"),SerializeField]
    private GameObject _faceMeshVisualiser;
    [FormerlySerializedAs("visualiserOffset"),SerializeField]
    private float _visualiserOffset;

    [FormerlySerializedAs("faceTracker"),Header("Face Trackers")]
    [SerializeField]
    private Transform _faceTracker;
    [FormerlySerializedAs("leftEyeTracker"),SerializeField]
    private Transform _leftEyeTracker;
    [FormerlySerializedAs("rightEyeTracker"),SerializeField]
    private Transform _rightEyeTracker;
    [FormerlySerializedAs("noseBridgeTracker"),SerializeField]
    private Transform _noseBridgeTracker;
    [FormerlySerializedAs("faceMaskHolder"),SerializeField]
    private Transform _faceMaskHolder;
    [FormerlySerializedAs("facesHolder"),SerializeField]
    private Transform _facesHolder;
    //public SkinnedMeshRenderer faceMask;

    [NonSerialized]
    public bool isPlaying;

    [SerializeField]
    private RawImage _videoFeed;


    private long _prevTime;
    private int _previousFrame;


    private DateTime _startTime;

    private FaceData.FaceMesh _faceMesh;
    
    private void Awake(){
        _faceMasks = _faceMaskHolder.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        _faceMeshes = _facesHolder.GetComponentsInChildren<MeshFilter>().ToList();
        mesh = new Mesh();
        isPlaying = true;
        _startTime = DateTime.Now;
        Debug.Log("Starting playback");
        TryAutomaticSetup();
    }
    
#if UNITY_EDITOR
    private void OnEnable(){
        _filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug");
        //_filePath = Application.dataPath;
        EditorApplication.hierarchyChanged += GetSkinnedMeshRenderers;
        EditorApplication.hierarchyChanged += GetFaceMeshFilters;
    }

    private void OnDisable(){
        EditorApplication.hierarchyChanged -= GetSkinnedMeshRenderers;
        EditorApplication.hierarchyChanged -= GetFaceMeshFilters;
    }
#endif

    private void TryAutomaticSetup(){
        if (IsSetUpProperly())
            return;
        if (_faceMeshVisualiser == null){
            _faceMeshVisualiser = transform.GetChild(0).gameObject;
        }
        if (_filterObject == null){
            _filterObject = GameObject.Find("Filter").transform;
        }

        if (_filterObject != null){
            if (_faceTracker == null)
                _faceTracker = _filterObject.Find("FaceTracker");
        }

        if (_faceTracker != null){
            if (_rightEyeTracker == null)
                _rightEyeTracker = _faceTracker.Find("RightEyeTracker");
            if (_leftEyeTracker == null)
                _leftEyeTracker = _faceTracker.Find("LeftEyeTracker");
            if (_noseBridgeTracker == null)
                _noseBridgeTracker = _faceTracker.Find("NoseBridgeTracker");
            if (_facesHolder == null)
                _facesHolder = _faceTracker.Find("Faces");
            if (_faceMaskHolder == null)
                _faceMaskHolder = _faceTracker.Find("FaceMasks");
        }
        if (IsSetUpProperly())
            Debug.Log("Successfully Set up");
        else{
            Debug.LogError("Failed to set up simulator");
        }
        
    }
    private bool IsSetUpProperly(){
        return _filterObject != null && _faceMeshVisualiser != null && _faceTracker != null && _leftEyeTracker != null &&
               _rightEyeTracker != null && _noseBridgeTracker != null && _faceMaskHolder != null && _facesHolder != null;
    }

    //Update function is used here to ensure the simulator runs every frame in Edit mode. if not, an alternate method that avoids the use of Update would have been used.
    private void Update(){
        if (!IsSetUpProperly()){
            Debug.LogError("The simulator object is not set up properly. Try clicking the Automatically Set Up button in the Simulator Inspector!");
            return;
        }
        EnforceObjectStructure();
        if (_faceRecording.faceDatas == null || _faceRecording.faceDatas.Count == 0){
            try{
                GetRecordingData();
            }
            catch (Exception e){
                Debug.Log($"Could not get recorded face data. {e.Message}");
                throw;
            }

        }
        
        if (!isPlaying){
            _startTime = DateTime.Now;
            return;
        }
        long time = (long) (DateTime.Now - _startTime).TotalMilliseconds;
        Playback(time);
    }
    
    private void GetRecordingData(){
        Debug.Log("Deserializing file");
        byte[] data = File.ReadAllBytes(Path.Combine(_filePath, "Simulator/FaceRecording"));
        string faceData = Encoding.ASCII.GetString(data);
        _faceRecording = JsonConvert.DeserializeObject<FaceRecording>(faceData);
        _recordingLength = _faceRecording.faceDatas[_faceRecording.faceDatas.Count - 1].timestamp;
        _frames = new List<Texture>();
        try{
            GetVideo();
        }
        catch (Exception e){
            Debug.Log($"Could not get video data. {e.Message}");
        }

    }
    
    private List<Texture> _frames;
    private void GetVideo(){
        string[] textureFiles = Directory.GetFiles($"{_filePath}/Simulator/Recordings", "*.png", SearchOption.AllDirectories);
        foreach(string textFile in textureFiles){
            string prefix = _filePath == Application.dataPath ? "Assets" : "Packages/com.getfilta.artist-unityplug";
            string assetPath = prefix + textFile.Replace(_filePath, "").Replace('\\', '/');
            Texture sourceText = (Texture)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture));
            _frames.Add(sourceText);
        }
        _frames = _frames.OrderBy((texture => Convert.ToInt64(texture.name))).ToList();
    }

    void Replay(){
        _startTime = DateTime.Now;
    }
    

    private void OnDrawGizmos(){
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying){
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    void PositionTrackers(FaceData faceData){
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
        Camera.main.transform.position = faceData.camera.position;
        Camera.main.transform.eulerAngles = faceData.camera.rotation;
    }

    void EnforceObjectStructure(){
        _faceTracker.name = "FaceTracker";
        _leftEyeTracker.name = "LeftEyeTracker";
        _rightEyeTracker.name = "RightEyeTracker";
        _noseBridgeTracker.name = "NoseBridgeTracker";
        _faceMaskHolder.name = "FaceMasks";
        _facesHolder.name = "Faces";
        gameObject.name = "Simulator";
        _filterObject.name = "Filter";
        _filterObject.position = Vector3.zero;
        _filterObject.rotation = Quaternion.identity;
        _filterObject.localScale = Vector3.one;
    }

    private void Playback(long currentTime){
        if (_recordingLength <= 0){
            return;
        }

        if (currentTime > _recordingLength){
            Replay();
            return;
        }

        if (_prevTime > currentTime){
            _previousFrame = 0;
        }

        _prevTime = currentTime;

        for (int i = _previousFrame; i < _faceRecording.faceDatas.Count; i++){
            FaceData faceData = _faceRecording.faceDatas[i];
            _faceMesh = faceData.faceMesh;
            long nextTimeStamp = faceData.timestamp;
            float[] nextBlendShape = faceData.blendshapeData;

            //we want to find the timestamp in the future so we can walk back a frame and interpolate
            if (nextTimeStamp < currentTime){
                if (i == _faceRecording.faceDatas.Count - 1){
                    i = 0;
                    break;
                }

                //we haven't found the future yet. try the next one.
                continue;
            }

            if (i == 0){
                break;
            }

            if (_videoFeed != null && _frames.Count > i)
                _videoFeed.texture = _frames[i];
            _faceMeshVisualiser.transform.localPosition = faceData.face.localPosition;
            _faceMeshVisualiser.transform.localEulerAngles = faceData.face.localRotation;
            _faceMeshVisualiser.transform.position -= _faceMeshVisualiser.transform.forward * _visualiserOffset;
            SetMeshTopology();
            PositionTrackers(faceData);
            FaceData prevFaceData = _faceRecording.faceDatas[i - 1];
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
    
    private int _maskCount;
    private void GetSkinnedMeshRenderers(){
        if (_maskCount == _faceMaskHolder.childCount) return;
        _faceMasks = _faceMaskHolder.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        _maskCount = _faceMaskHolder.childCount;
    }

    private void UpdateMasks(FaceData faceData, FaceData prevFaceData, long currentTime){
        long nextTimeStamp = faceData.timestamp;
        float[] nextBlendShape = faceData.blendshapeData;
        long prevTimeStamp = prevFaceData.timestamp;
        float[] prevBlendShape = prevFaceData.blendshapeData;
        float nextWeight = (float) (currentTime - prevTimeStamp) / (nextTimeStamp - prevTimeStamp);
        float prevWeight = 1f - nextWeight;

        //now to grab the blendshape values of the prev and next frame and lerp + assign them
        for (int j = 0; j < prevBlendShape.Length - 2; j++){
            var nowValue = (prevBlendShape[j] * prevWeight) + (nextBlendShape[j] * nextWeight);
            for (int i = 0; i < _faceMasks.Count; i++){
                if (_faceMasks[i] != null){
                    _faceMasks[i].SetBlendShapeWeight(j, nowValue);
                }
                
            }
        }
    }

    #endregion

    #region Face Mesh Control
    
    private List<MeshFilter> _faceMeshes;
    
    private int _faceCount;

    private void GetFaceMeshFilters(){
        if (_faceCount == _facesHolder.childCount) return;
        _faceMeshes = _facesHolder.GetComponentsInChildren<MeshFilter>().ToList();
        _faceCount = _facesHolder.childCount;
    }

    public Mesh mesh { get; private set; }

    void SetMeshTopology()
    {
        if (mesh == null)
        {
            return;
        }

        mesh.Clear();
        if (_faceMesh.vertices.Count > 0 && _faceMesh.indices.Count > 0){
            mesh.SetVertices(FaceData.Vector3Converter(_faceMesh.vertices));
            mesh.SetIndices(_faceMesh.indices, MeshTopology.Triangles, 0, false);
            mesh.RecalculateBounds();
            if (_faceMesh.normals.Count == _faceMesh.vertices.Count){
                mesh.SetNormals(FaceData.Vector3Converter(_faceMesh.normals));
            }
            else{
                mesh.RecalculateNormals();
            }

            if (_faceMesh.uvs.Count > 0){
                mesh.SetUVs(0, FaceData.Vector2Converter(_faceMesh.uvs));
            }
            
            var meshFilter = _faceMeshVisualiser.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
            }

            for (int i = 0; i < _faceMeshes.Count; i++){
                if (_faceMeshes[i] != null){
                    _faceMeshes[i].sharedMesh = mesh;
                }
            }
        }
    }

    #endregion

    #region Class/Struct Definition
    
        [Serializable]
        public struct FaceData
        {
            public long timestamp;
            public float[] blendshapeData;
            public FaceMesh faceMesh;
            public Trans face;
            public Trans leftEye;
            public Trans rightEye;
            public Trans camera;
    
            [Serializable]
            public struct FaceMesh
            {
                public List<Trans.Vector3Json> vertices;
                public List<Trans.Vector3Json> normals;
                public List<int> indices;
                public List<Trans.Vector2Json> uvs;
            }
    
            [Serializable]
            public struct Trans
            {
                public Vector3Json position;
                public Vector3Json rotation;
                public Vector3Json localPosition;
                public Vector3Json localRotation;
    
                public static implicit operator Trans(Transform trans){
                    return new Trans{
                        position = trans.position, rotation = trans.eulerAngles, localPosition = trans.localPosition,
                        localRotation = trans.localEulerAngles
                    };
                }
    
                [Serializable]
                public struct Vector3Json
                {
                    public float x, y, z;
    
                    public static implicit operator Vector3Json(Vector3 vector){
                        return new Vector3Json{x = vector.x, y = vector.y, z = vector.z};
                    }
    
                    public static implicit operator Vector3(Vector3Json vector){
                        return new Vector3{x = vector.x, y = vector.y, z = vector.z};
                    }
                }
    
                [Serializable]
                public struct Vector2Json
                {
                    public float x, y;
    
                    public static implicit operator Vector2Json(Vector2 vector){
                        return new Vector2Json{x = vector.x, y = vector.y};
                    }
    
                    public static implicit operator Vector2(Vector2Json vector){
                        return new Vector2{x = vector.x, y = vector.y};
                    }
                }
            }
    
            public static List<Trans.Vector3Json> Vector3Converter(NativeArray<Vector3> nativeArray){
                List<Trans.Vector3Json> vector3Jsons = new List<Trans.Vector3Json>(nativeArray.Length);
                foreach (Vector3 vector in nativeArray){
                    vector3Jsons.Add(vector);
                }
    
                return vector3Jsons;
            }
                
            public static List<Vector3> Vector3Converter(List<Trans.Vector3Json> nativeArray){
                List<Vector3> vector3 = new List<Vector3>(nativeArray.Count);
                foreach (Trans.Vector3Json vector in nativeArray){
                    vector3.Add(vector);
                }
    
                return vector3;
            }
                    
            public static List<Trans.Vector2Json> Vector2Converter(NativeArray<Vector2> nativeArray){
                List<Trans.Vector2Json> vector2Jsons = new List<Trans.Vector2Json>(nativeArray.Length);
                foreach (Vector2 vector in nativeArray){
                    vector2Jsons.Add(vector);
                }
    
                return vector2Jsons;
            }
                
            public static List<Vector2> Vector2Converter(List<Trans.Vector2Json> nativeArray){
                List<Vector2> vector2 = new List<Vector2>(nativeArray.Count);
                foreach (Trans.Vector2Json vector in nativeArray){
                    vector2.Add(vector);
                }
    
                return vector2;
            }
        }
    
        [Serializable]
        public struct FaceRecording
        {
            public List<FaceData> faceDatas;
        }
    
        #endregion
        
    #region Editor 
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Simulator))]
    public class SimulatorEditor : Editor
    {
        public override void OnInspectorGUI(){
            DrawDefaultInspector();
            EditorGUILayout.Separator();
            Simulator sim = (Simulator) target;
            if (sim.isPlaying){
                if (GUILayout.Button("Stop")){
                    sim.isPlaying = false;
                }
            }
            else{
                if (GUILayout.Button("Play")){
                    sim.isPlaying = true;
                }
            }
            if (!sim.IsSetUpProperly()){
                if (GUILayout.Button("Automatically set up")){
                    sim.TryAutomaticSetup();
                }
            }
            

        }
    }
    
    
    #endif
    
    #endregion

}
