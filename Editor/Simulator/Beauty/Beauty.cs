using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class Beauty : MonoBehaviour {
    public int resolution = 10;
    public bool leftEyelashActive;
    public bool rightEyelashActive;

    [SerializeField]
    private GameObject eyelashPrefab;
    
    public Vector3[] leftEyeVertices;
    public Vector3[] rightEyeVertices;
    
    private GameObject _leftEyelash;
    private GameObject _rightEyelash;

    private MeshFilter _leftMeshFilter;
    private MeshFilter _rightMeshFilter;

    private Variables _leftVariables;
    private Variables _rightVariables;

    private AnimationCurve _leftCurve;
    private AnimationCurve _rightCurve;

    private const string LeftEyelashName = "LeftEyelash";
    private const string RightEyelashName = "RightEyelash";

    public enum Eye {
        Left,
        Right
    }
    public AnimationCurve LeftCurve {
        get {
            _leftCurve ??= new AnimationCurve();
            if (_leftVariables != null) {
                _leftCurve = _leftVariables.declarations.Get<AnimationCurve>("EyelashCurve");
            }

            return _leftCurve;
        }
        set {
            _leftCurve = value;
            if (_leftVariables != null) {
                _leftVariables.declarations.Set("EyelashCurve", _leftCurve);
            }
        }
    }
    
    public AnimationCurve RightCurve {
        get {
            _rightCurve ??= new AnimationCurve();
            if (_rightVariables != null) {
                _rightCurve = _rightVariables.declarations.Get<AnimationCurve>("EyelashCurve");
            }

            return _rightCurve;
        }
        set {
            _rightCurve = value;
            if (_rightVariables != null) {
                _rightVariables.declarations.Set("EyelashCurve", _rightCurve);
            }
        }
    }

    [SerializeField]
    private Simulator simulator;

    public void HandlePlayback(Vector3[] leftVertices, Vector3[] rightVertices) {
        leftEyeVertices = leftVertices;
        rightEyeVertices = rightVertices;
        HandlePlayback();
    }
    
    public void HandlePlayback() {
        if (simulator == null) {
            simulator = GetComponent<Simulator>();
        }
        if (simulator == null || !simulator.IsSetUpProperly())
            return;
        HandleEyelashToggling();
        if (_leftEyelash != null) {
            _leftEyelash.name = LeftEyelashName;
            simulator.leftEyelashHolder.localPosition = leftEyeVertices[0];
            simulator.leftEyelashHolder.localRotation = Quaternion.identity;
            GenerateMesh(Eye.Left);
        }

        if (_rightEyelash != null) {
            _rightEyelash.name = RightEyelashName;
            simulator.rightEyelashHolder.localPosition = rightEyeVertices[0];
            simulator.rightEyelashHolder.localRotation = Quaternion.identity;
            GenerateMesh(Eye.Right);
        }
    }

    private void HandleEyelashToggling() {
        if (leftEyelashActive && _leftEyelash == null) {
            Transform left = simulator.leftEyelashHolder.Find(LeftEyelashName);
            if (left == null) {
                GenerateEyelash(Eye.Left);
            } else {
                _leftEyelash = left.gameObject;
            }
        } else if (!leftEyelashActive && _leftEyelash != null) {
            DestroyImmediate(_leftEyelash);
            _leftEyelash = null;
        }

        if (rightEyelashActive && _rightEyelash == null) {
            Transform right = simulator.rightEyelashHolder.Find(RightEyelashName);
            if (right == null) {
                GenerateEyelash(Eye.Right);
            } else {
                _rightEyelash = right.gameObject;
            }
        } else if (!rightEyelashActive && _rightEyelash != null) {
            DestroyImmediate(_rightEyelash);
            _rightEyelash = null;
        }
    }

    private void GenerateEyelash(Eye eye) {
        if (eye == Eye.Left) {
            _leftEyelash = Instantiate(eyelashPrefab, simulator.leftEyelashHolder);
            _leftEyelash.name = LeftEyelashName;
            _leftVariables = _leftEyelash.GetComponent<Variables>();
            _leftMeshFilter = _leftEyelash.GetComponent<MeshFilter>();
        } else {
            _rightEyelash = Instantiate(eyelashPrefab, simulator.rightEyelashHolder);
            _rightEyelash.name = RightEyelashName;
            _rightVariables = _rightEyelash.GetComponent<Variables>();
            _rightMeshFilter = _rightEyelash.GetComponent<MeshFilter>();
        }
    }

    private void GenerateMesh(Eye eye) {
        AnimationCurve curve;
        Vector3[] verts;
        MeshFilter filter;
        if (eye == Eye.Left) {
            curve = _leftCurve;
            verts = leftEyeVertices;
            filter = _leftMeshFilter;
        } else {
            curve = _rightCurve;
            verts = rightEyeVertices;
            filter = _rightMeshFilter;
        }
        
        float increments = curve.keys[^1].time / resolution;
        int sideSize = verts.Length;
        Mesh mesh = filter.sharedMesh;
        if (mesh == null) {
            mesh = new Mesh();
            filter.sharedMesh = mesh;
        }
        mesh.Clear();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        float uvLength = resolution * increments;
        int indCount = 0;
        Vector3 totalDifference = Vector3.zero;
        float fullWidth = verts[^1].x - verts[0].x;
        for (int j = 0; j < sideSize - 1; j++) {
            Vector3 difference = verts[j + 1] - verts[j];
            vertices.Add(totalDifference);
            vertices.Add(totalDifference + difference);
            uv.Add(Vector3.right * totalDifference.x/fullWidth);
            uv.Add(Vector3.right * ((totalDifference.x + difference.x))/fullWidth);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            for (int i = 1; i < resolution + 1; i++) {
                float movement = i * increments;
                float height = curve.Evaluate(movement);
                vertices.Add(new Vector3(totalDifference.x, height + totalDifference.y, -movement + totalDifference.z));
                vertices.Add(new Vector3((totalDifference.x + difference.x), height + totalDifference.y + difference.y, -movement + totalDifference.z + difference.z));
                uv.Add(new Vector2(totalDifference.x/fullWidth, movement/uvLength));
                uv.Add(new Vector2(((totalDifference.x + difference.x))/fullWidth, movement/uvLength));
                Vector3 a = vertices[i * 2 - 2] - vertices[i * 2];
                Vector3 b = vertices[i * 2 - 1] - vertices[i * 2];
                Vector3 normal = Vector3.Cross(b, a);
                normal.Normalize();
                normals.Add(normal);
                normals.Add(normal);
                //first set of triangles
                indices.Add(indCount + (i * 2 - 2));
                indices.Add(indCount + (i * 2));
                indices.Add(indCount + (i * 2 - 1));
                //second set of triangles
                indices.Add(indCount + (i * 2));
                indices.Add(indCount + (i * 2 + 1));
                indices.Add(indCount + (i * 2 - 1));
            }

            totalDifference += difference;
            indCount = vertices.Count;
        }
        mesh.name = "BillieEyelash";
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.uv = uv.ToArray();
        DuplicateBackface(mesh);
    }

    private void DuplicateBackface(Mesh mesh) {
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        Vector3[] normals = mesh.normals;
        int vertLength = vertices.Length;
        Vector3[] newVerts = new Vector3[vertLength * 2];
        Vector2[] newUv = new Vector2[vertLength * 2];
        Vector3[] newNorms = new Vector3[vertLength * 2];
        for (var j = 0; j < vertLength; j++) {
            // duplicate vertices and uvs:
            newVerts[j] = newVerts[j + vertLength] = vertices[j];
            newUv[j] = newUv[j + vertLength] = uv[j];
            // copy the original normals...
            newNorms[j] = normals[j];
            // and revert the new ones
            newNorms[j + vertLength] = -normals[j];
        }

        int[] triangles = mesh.triangles;
        int triLength = triangles.Length;
        int[] newTris = new int[triLength * 2]; // double the triangles
        for (var i = 0; i < triLength; i += 3) {
            // copy the original triangle
            newTris[i] = triangles[i];
            newTris[i + 1] = triangles[i + 1];
            newTris[i + 2] = triangles[i + 2];
            // save the new reversed triangle
            var j = i + triLength;
            newTris[j] = triangles[i] + vertLength;
            newTris[j + 2] = triangles[i + 1] + vertLength;
            newTris[j + 1] = triangles[i + 2] + vertLength;
        }

        mesh.vertices = newVerts;
        mesh.uv = newUv;
        mesh.normals = newNorms;
        mesh.triangles = newTris; // assign triangles last!
    }
}
