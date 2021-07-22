using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;


[ExecuteAlways]
public class Simulator : MonoBehaviour
{
    public string filePath;
    
    private long recordingLength;
    
    private FaceRecording faceRecording;

    public GameObject faceMeshVisualiser;
    public float visualiserOffset;

    [Header("Face Trackers")]
    public Transform faceTracker;
    public Transform leftEyeTracker;
    public Transform rightEyeTracker;
    public Transform noseBridgeTracker;
    //public SkinnedMeshRenderer faceMask;

    [NonSerialized]
    public bool isPlaying;
    
    
    private long prevTime;
    private int previousFrame;


    private DateTime startTime;

    private FaceData.FaceMesh faceMesh;

    private bool IsSetUpProperly(){
        return faceMeshVisualiser != null && faceTracker != null && leftEyeTracker != null && rightEyeTracker != null && noseBridgeTracker != null;
    }

    private void Update(){
        if (!IsSetUpProperly()){
            Debug.LogError("The simulator object is not set up properly. Consider deleting the simulator object and re-activating it from the artist panel!");
            return;
        }
            
        if (faceRecording.faceDatas == null || faceRecording.faceDatas.Count == 0){
            try{
                GetRecordingData();
            }
            catch (Exception e){
                Debug.Log($"Could not get recorded face data. {e.Message}");
                throw;
            }

        }
        
        if (!isPlaying){
            startTime = DateTime.Now;
            return;
        }
        long time = (long) (DateTime.Now - startTime).TotalMilliseconds;
        Playback(time);
    }
    
    private void GetRecordingData(){
        Debug.Log("Deserializing file");
        byte[] data = File.ReadAllBytes(filePath);
        string faceData = Encoding.ASCII.GetString(data);
        faceRecording = JsonConvert.DeserializeObject<FaceRecording>(faceData);
        recordingLength = faceRecording.faceDatas[faceRecording.faceDatas.Count - 1].timestamp;
    }

    void Replay(){
        startTime = DateTime.Now;
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
        faceTracker.localPosition = faceData.face.localPosition;
        faceTracker.localEulerAngles = faceData.face.localRotation;
        leftEyeTracker.localPosition = faceData.leftEye.position;
        leftEyeTracker.localEulerAngles = faceData.leftEye.rotation;
        rightEyeTracker.localPosition = faceData.rightEye.position;
        rightEyeTracker.localEulerAngles = faceData.rightEye.rotation;
        Vector3 noseBridgePosition = leftEyeTracker.localPosition +
                                     (rightEyeTracker.localPosition - leftEyeTracker.localPosition) / 2;
        noseBridgeTracker.localPosition = noseBridgePosition;
        noseBridgeTracker.localEulerAngles = faceData.face.localRotation;
    }

    private void Playback(long currentTime){
        if (recordingLength <= 0){
            return;
        }

        if (currentTime > recordingLength){
            Replay();
            return;
        }

        if (prevTime > currentTime){
            previousFrame = 0;
        }

        prevTime = currentTime;

        for (int i = previousFrame; i < faceRecording.faceDatas.Count; i++){
            FaceData faceData = faceRecording.faceDatas[i];
            faceMesh = faceData.faceMesh;
            long nextTimeStamp = faceData.timestamp;
            float[] nextBlendShape = faceData.blendshapeData;

            //we want to find the timestamp in the future so we can walk back a frame and interpolate
            if (nextTimeStamp < currentTime){
                if (i == faceRecording.faceDatas.Count - 1){
                    i = 0;
                    break;
                }

                //we haven't found the future yet. try the next one.
                continue;
            }

            if (i == 0){
                break;
            }
            
            faceMeshVisualiser.transform.localPosition = faceData.face.localPosition;
            faceMeshVisualiser.transform.localEulerAngles = faceData.face.localRotation;
            faceMeshVisualiser.transform.position -= faceMeshVisualiser.transform.forward * visualiserOffset;
            SetMeshTopology();
            PositionTrackers(faceData);

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

            previousFrame = i;
            break;
        }
    }

    #region Face Mesh Generation

    public Mesh mesh { get; private set; }

    private void Awake(){
        mesh = new Mesh();
        isPlaying = true;
        startTime = DateTime.Now;
        filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug/Simulator/FaceRecording");
        Debug.Log("Starting playback");
    }

    void SetMeshTopology()
    {
        if (mesh == null)
        {
            return;
        }

        mesh.Clear();
        if (faceMesh.vertices.Count > 0 && faceMesh.indices.Count > 0){
            mesh.SetVertices(FaceData.Vector3Converter(faceMesh.vertices));
            mesh.SetIndices(faceMesh.indices, MeshTopology.Triangles, 0, false);
            mesh.RecalculateBounds();
            if (faceMesh.normals.Count == faceMesh.vertices.Count){
                mesh.SetNormals(FaceData.Vector3Converter(faceMesh.normals));
            }
            else{
                mesh.RecalculateNormals();
            }

            if (faceMesh.uvs.Count > 0){
                mesh.SetUVs(0, FaceData.Vector2Converter(faceMesh.uvs));
            }
            
            var meshFilter = faceMeshVisualiser.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
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

}
