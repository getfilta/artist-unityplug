using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteAlways]
#endif
public abstract class SimulatorBase : MonoBehaviour {
    #if UNITY_EDITOR
    public enum SimulatorType {
        Face,
        Body,
        Fusion
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
        // Ensure continuous Update calls.
        if (!Application.isPlaying) {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }

    protected virtual void OnEnable() {
        _filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug");
    }

    protected virtual void Update() {

    }

    public virtual void TryAutomaticSetup() {

    }

    public virtual void Disable() {
        
    }

    public virtual void Enable() {
        
    }
    
    protected void SetFilterLayers() {
        if (_filterObject == null || !EditorApplication.isPlayingOrWillChangePlaymode) {
            return;
        }
        Transform[] children = _filterObject.GetComponentsInChildren<Transform>(true);
        foreach ( Transform child in children) {
            child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }
    
    [ContextMenu("Toggle Visibility")]
    protected void ContextMenu() {
        _developerMode = !_developerMode;
        SetFlags(_developerMode);
    }

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
    #endif
}
