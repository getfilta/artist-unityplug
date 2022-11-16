using System;
using UnityEngine;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;

[ExecuteAlways]
#endif
public class Beauty : MonoBehaviour {
#if UNITY_EDITOR

    #region Variable Definitions
    
    [SerializeField]
    private Simulator simulator;

    private bool _initialized;

    #region Eyelash Variables
    
    [SerializeField]
    private GameObject eyelashPrefab;
    
    private AnimationCurve _leftCurve;
    private AnimationCurve _rightCurve;
    
    public int resolution = 10;
    public bool leftEyelashActive;
    public bool rightEyelashActive;
    
    public Vector3[] leftEyeVertices;
    public Vector3[] rightEyeVertices;

    private GameObject _leftEyelash;
    private GameObject _rightEyelash;
    
    private MeshFilter _leftMeshFilter;
    private MeshFilter _rightMeshFilter;
    private const string LeftEyelashName = "LeftEyelash";
    private const string RightEyelashName = "RightEyelash";
    
    private float _leftAngle;
    private float _rightAngle;
    private Variables _leftVariables;
    private Variables _rightVariables;

    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Vector2[] _uv;
    private int[] _indices;

    private int _sideSize;

    private int _vertCount;
    private int _indicesCount;

    AnimationCurve _curve;
    Vector3[] _verts;
    MeshFilter _filter;
    private float _angle;

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

    public float LeftAngle {
        get {
            if (_leftVariables != null) {
                _leftAngle = _leftVariables.declarations.Get<float>("Angle");
            }

            return _leftAngle;
        }
        set {
            _leftAngle = value;
            if (_leftVariables != null) {
                _leftVariables.declarations.Set("Angle", _leftAngle);
            }
        }
    }

    public float RightAngle {
        get {
            if (_rightVariables != null) {
                _rightAngle = _rightVariables.declarations.Get<float>("Angle");
            }

            return _rightAngle;
        }
        set {
            _rightAngle = value;
            if (_rightVariables != null) {
                _rightVariables.declarations.Set("Angle", _rightAngle);
            }
        }
    }

    #endregion

    #region Lip Variables
    private static readonly int LipTextureProperty = Shader.PropertyToID("_LipTexture");
    private static readonly int GlossProperty = Shader.PropertyToID("_Gloss");
    private static readonly int TintProperty = Shader.PropertyToID("_Tint");
    private static readonly int TilingProperty = Shader.PropertyToID("_Tiling");
    private static readonly int OffsetProperty = Shader.PropertyToID("_Offset");
    public bool lipsActive;

    [SerializeField]
    private Material lipsMaterial;
    
    private GameObject _lips;

    [NonSerialized]
    public SkinnedMeshRenderer lipsMeshRenderer;

    
    private Variables _lipsVariable;

    private Texture _lipsTexture;
    
    private float _glossiness = 0.5f;
    private Color _tint = Color.white;
    private Vector2 _tiling = new(1, 1);
    private Vector2 _offset = new(0, 0);

    
    private const string LipsName = "Lips";
    public Texture LipsTexture {
        get {
            if (_lipsVariable != null) {
                _lipsTexture = _lipsVariable.declarations.Get<Texture>("Texture");
            }

            return _lipsTexture;
        }
        set {
            _lipsTexture = value;
            if (_lipsVariable != null) {
                _lipsVariable.declarations.Set("Texture", _lipsTexture);
            }
        }
    }
    
    public float Glossiness {
        get {
            if (_lipsVariable != null) {
                _glossiness = _lipsVariable.declarations.Get<float>("Glossiness");
            }

            return _glossiness;
        }
        set {
            _glossiness = value;
            if (_lipsVariable != null) {
                _lipsVariable.declarations.Set("Glossiness", _glossiness);
            }
        }
    }
    
    public Color Tint {
        get {
            if (_lipsVariable != null) {
                _tint = _lipsVariable.declarations.Get<Color>("Tint");
            }

            return _tint;
        }
        set {
            _tint = value;
            if (_lipsVariable != null) {
                _lipsVariable.declarations.Set("Tint", _tint);
            }
        }
    }
    
    public Vector2 Tiling {
        get {
            if (_lipsVariable != null) {
                _tiling = _lipsVariable.declarations.Get<Vector2>("Tiling");
            }

            return _tiling;
        }
        set {
            _tiling = value;
            if (_lipsVariable != null) {
                _lipsVariable.declarations.Set("Tiling", _tiling);
            }
        }
    }
    
    public Vector2 Offset {
        get {
            if (_lipsVariable != null) {
                _offset = _lipsVariable.declarations.Get<Vector2>("Offset");
            }

            return _offset;
        }
        set {
            _offset = value;
            if (_lipsVariable != null) {
                _lipsVariable.declarations.Set("Offset", _offset);
            }
        }
    }

    #endregion
    

    #endregion
    

    public void Initialize() {
        Transform left = simulator.leftEyelashHolder.Find(LeftEyelashName);
        Transform right = simulator.rightEyelashHolder.Find(RightEyelashName);
        Transform lips = simulator.lipsHolder.Find(LipsName);
        if (left != null) {
            _leftEyelash = left.gameObject;
            _leftVariables = _leftEyelash.GetComponent<Variables>();
            _leftMeshFilter = _leftEyelash.GetComponent<MeshFilter>();
            leftEyelashActive = true;
        }

        if (right != null) {
            _rightEyelash = right.gameObject;
            _rightVariables = _rightEyelash.GetComponent<Variables>();
            _rightMeshFilter = _rightEyelash.GetComponent<MeshFilter>();
            rightEyelashActive = true;
        }

        if (lips != null) {
            _lips = lips.gameObject;
            _lipsVariable = _lips.GetComponent<Variables>();
            lipsMeshRenderer = _lips.GetComponent<SkinnedMeshRenderer>();
            lipsActive = true;
        }

        _sideSize = Simulator.EyelashVertexCount;
        int vertexLength = (((resolution * 2) * (_sideSize - 1)) + (2 * (_sideSize - 1))) * 2;
        int indexLength = 6 * resolution * (_sideSize - 1) * 2;
        _vertices = new Vector3[vertexLength];
        _normals = new Vector3[vertexLength];
        _uv = new Vector2[vertexLength];
        _indices = new int[indexLength];

        _initialized = true;
    }

    public void HandlePlayback(Vector3[] leftVertices, Vector3[] rightVertices) {
        leftEyeVertices = leftVertices;
        rightEyeVertices = rightVertices;
        HandlePlayback();
    }

    public void HandlePlayback() {
        if (!_initialized) {
            return;
        }

        if (simulator == null) {
            simulator = GetComponent<Simulator>();
        }

        if (simulator == null || !simulator.IsSetUpProperly())
            return;
        HandleEyelashToggling();
        HandleLipsToggling();
        if (_lips != null) {
            _lips.name = LipsName;
            lipsMeshRenderer.sharedMaterial = lipsMaterial;
            lipsMeshRenderer.sharedMaterial.SetTexture(LipTextureProperty, LipsTexture);
            lipsMeshRenderer.sharedMaterial.SetFloat(GlossProperty, Glossiness);
            lipsMeshRenderer.sharedMaterial.SetColor(TintProperty, Tint);
            lipsMeshRenderer.sharedMaterial.SetVector(TilingProperty, Tiling);
            lipsMeshRenderer.sharedMaterial.SetVector(OffsetProperty, Offset);
        }
        
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

    #region Eyelash Functions

    private void HandleEyelashToggling() {
        if (leftEyelashActive && _leftEyelash == null) {
            GenerateEyelashObject(Eye.Left);
        } else if (!leftEyelashActive && _leftEyelash != null) {
            DestroyImmediate(_leftEyelash);
            _leftEyelash = null;
#if UNITY_EDITOR
            EditorSceneManager.MarkAllScenesDirty();
#endif
        }

        if (rightEyelashActive && _rightEyelash == null) {
            GenerateEyelashObject(Eye.Right);
        } else if (!rightEyelashActive && _rightEyelash != null) {
            DestroyImmediate(_rightEyelash);
            _rightEyelash = null;
#if UNITY_EDITOR
            EditorSceneManager.MarkAllScenesDirty();
#endif
        }
    }

    private void GenerateEyelashObject(Eye eye) {
        if (eye == Eye.Left) {
            _leftEyelash = Instantiate(eyelashPrefab, simulator.leftEyelashHolder);
#if UNITY_EDITOR
            Selection.activeGameObject = _leftEyelash;
#endif
            _leftEyelash.name = LeftEyelashName;
            _leftVariables = _leftEyelash.GetComponent<Variables>();
            _leftMeshFilter = _leftEyelash.GetComponent<MeshFilter>();
        } else {
            _rightEyelash = Instantiate(eyelashPrefab, simulator.rightEyelashHolder);
#if UNITY_EDITOR
            Selection.activeGameObject = _rightEyelash;
#endif
            _rightEyelash.name = RightEyelashName;
            _rightVariables = _rightEyelash.GetComponent<Variables>();
            _rightMeshFilter = _rightEyelash.GetComponent<MeshFilter>();
        }
#if UNITY_EDITOR
        EditorSceneManager.MarkAllScenesDirty();
#endif
    }
    
        private void GenerateMesh(Eye eye) {
        if (eye == Eye.Left) {
            _curve = LeftCurve;
            _angle = LeftAngle;
            _verts = leftEyeVertices;
            _filter = _leftMeshFilter;
        } else {
            _curve = RightCurve;
            _angle = -RightAngle;
            _verts = rightEyeVertices;
            _filter = _rightMeshFilter;
        }

        float increments = _curve.keys[^1].time / resolution;
        Mesh mesh = _filter.sharedMesh;
        if (mesh == null) {
            mesh = new Mesh();
            _filter.sharedMesh = mesh;
        }

        mesh.Clear();
        float uvLength = resolution * increments;
        int indCount = 0;
        Vector3 totalDifference = Vector3.zero;
        float fullWidth = _verts[^1].x - _verts[0].x;
        _vertCount = 0;
        _indicesCount = 0;
        for (int j = 0; j < _sideSize - 1; j++) {
            Vector3 difference = _verts[j + 1] - _verts[j];
            _vertices[_vertCount] = totalDifference;
            _uv[_vertCount] = Vector3.right * totalDifference.x / fullWidth;
            _normals[_vertCount] = Vector3.up;
            _vertCount++;

            _vertices[_vertCount] = totalDifference + difference;
            _uv[_vertCount] = Vector3.right * ((totalDifference.x + difference.x)) / fullWidth;
            _normals[_vertCount] = Vector3.up;
            _vertCount++;

            for (int i = 1; i < resolution + 1; i++) {
                float movement = i * increments;
                float height = _curve.Evaluate(movement);

                float internalRadius = Mathf.Lerp(_angle, -_angle, 1 - (float)j / (_sideSize - 1));
                float radialCurve = -movement * Mathf.Tan(Mathf.Deg2Rad * internalRadius);
                _vertices[_vertCount] = new Vector3(totalDifference.x - radialCurve, height + totalDifference.y,
                    -movement + totalDifference.z);
                _uv[_vertCount] = new Vector2(totalDifference.x / fullWidth, movement / uvLength);
                _vertCount++;

                float internalRadius2 = Mathf.Lerp(_angle, -_angle, 1 - ((float)j + 1) / (_sideSize - 1));
                float radialCurve2 = -movement * Mathf.Tan(Mathf.Deg2Rad * internalRadius2);
                _vertices[_vertCount] = new Vector3((totalDifference.x + difference.x - radialCurve2),
                    height + totalDifference.y + difference.y, -movement + totalDifference.z + difference.z);
                _uv[_vertCount] = new Vector2(((totalDifference.x + difference.x)) / fullWidth, movement / uvLength);
                _vertCount++;

                Vector3 a = _vertices[i * 2 - 2] - _vertices[i * 2];
                Vector3 b = _vertices[i * 2 - 1] - _vertices[i * 2];
                Vector3 normal = Vector3.Cross(b, a);
                normal.Normalize();
                _normals[_vertCount - 2] = normal;
                _normals[_vertCount - 1] = normal;

                //first set of triangles
                _indices[_indicesCount] = (indCount + (i * 2 - 2));
                _indicesCount++;
                _indices[_indicesCount] = (indCount + (i * 2));
                _indicesCount++;
                _indices[_indicesCount] = (indCount + (i * 2 - 1));
                _indicesCount++;

                //second set of triangles
                _indices[_indicesCount] = (indCount + (i * 2));
                _indicesCount++;
                _indices[_indicesCount] = (indCount + (i * 2 + 1));
                _indicesCount++;
                _indices[_indicesCount] = (indCount + (i * 2 - 1));
                _indicesCount++;
            }

            totalDifference += difference;
            indCount = _vertCount;
        }

        mesh.name = "BillieEyelash";
        mesh.vertices = _vertices;
        mesh.normals = _normals;
        mesh.triangles = _indices;
        mesh.uv = _uv;
        DuplicateBackface(mesh);
    }

    private void DuplicateBackface(Mesh mesh) {
        int vertLength = _vertCount;

        for (var j = 0; j < vertLength; j++) {
            // duplicate vertices and uvs:
            _vertices[j + vertLength] = _vertices[j];
            _uv[j + vertLength] = _uv[j];
            // copy the original normals...
            // and revert the new ones
            _normals[j + vertLength] = -_normals[j];
        }

        int triLength = _indicesCount;
        for (var i = 0; i < triLength; i += 3) {
            var j = i + triLength;
            _indices[j] = _indices[i] + vertLength;
            _indices[j + 2] = _indices[i + 1] + vertLength;
            _indices[j + 1] = _indices[i + 2] + vertLength;
        }

        mesh.vertices = _vertices;
        mesh.uv = _uv;
        mesh.normals = _normals;
        mesh.triangles = _indices; // assign triangles last!
    }

    #endregion

    #region Lip Functions

    private void HandleLipsToggling() {
        if (lipsActive && _lips == null) {
            GenerateLip();
        } else if (!lipsActive && _lips != null) {
            DestroyImmediate(_lips);
            _lips = null;
#if UNITY_EDITOR
            EditorSceneManager.MarkAllScenesDirty();
#endif
        }
    }

    private void GenerateLip() {
        if (simulator != null) {
            _lips = simulator.SpawnNewFaceMesh();
            _lips.transform.SetParent(simulator.lipsHolder);
            _lipsVariable = _lips.AddComponent<Variables>();
            _lipsVariable.declarations.Set("Glossiness", _glossiness);
            _lipsVariable.declarations.Set("Texture", _lipsTexture);
            _lipsVariable.declarations.Set("Tint", _tint);
            _lipsVariable.declarations.Set("Tiling", _tiling);
            _lipsVariable.declarations.Set("Offset", _offset);
            lipsMeshRenderer = _lips.GetComponent<SkinnedMeshRenderer>();
            lipsMeshRenderer.sharedMaterial = lipsMaterial;
#if UNITY_EDITOR
            Selection.activeGameObject = _lips;
            _lips.hideFlags = HideFlags.NotEditable;
            EditorSceneManager.MarkAllScenesDirty();
#endif
        }
    }

    #endregion
#endif
}
