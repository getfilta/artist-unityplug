//WARNING ENSURE ALL EDITOR FUNCTIONS ARE WRAPPED IN UNITY_EDITOR DIRECTIVE
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteAlways]
public abstract class SimulatorBase : MonoBehaviour {
    public enum SimulatorType {
        Face,
        Body
    }

    public virtual SimulatorType _simulatorType { get; }

    protected string _filePath { get; set; }

    public Transform _filterObject;

    [SerializeField]
    private HideFlags _customHideFlags;
    private Transform[] _objectsToHide;
    private bool _developerMode = false;

    public virtual bool IsSetUpProperly() {
        return false;
    }

    protected virtual void EnforceObjectStructure() {

    }

    protected virtual void Awake() {
        _objectsToHide = GetComponentsInChildren<Transform>(true);
    }

    private void OnRenderObject() {
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying) {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    protected virtual void OnEnable() {
        _filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug");
    }

    protected virtual void Update() {

    }

    public virtual void TryAutomaticSetup() {

    }

#if UNITY_EDITOR
    [ContextMenu("Toggle Visibility")]
    protected void ContextMenu() {
        _developerMode = !_developerMode;
        SetFlags(_developerMode);
    }
#endif

    protected void SetFlags(bool forceVisibility = false) {
        if (forceVisibility) {
            foreach (var target in _objectsToHide) {
                target.gameObject.hideFlags = HideFlags.None;
            }
            return;
        }
        _objectsToHide[0].gameObject.hideFlags = HideFlags.NotEditable;
        //we start at 1 because 0 is the parent
        for (int i = 1; i < _objectsToHide.Length; i++) {
            _objectsToHide[i].gameObject.hideFlags = _customHideFlags;
        }
    }
}
