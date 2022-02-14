using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
    private bool _developerMode = false;
    [SerializeField]
    private HideFlags _customHideFlags;
    private Transform[] _objectsToHide;

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

    [ContextMenu("Set Flags")]
    protected void ContextMenu() {
        SetFlags();
    }

    protected void SetFlags(bool initialSetup = false) {
        if (_developerMode || initialSetup) {
            foreach (var target in _objectsToHide) {
                target.gameObject.hideFlags = HideFlags.None;
            }
            return;
        }
        //we start at 1 because 0 is the parent
        for (int i = 1; i < _objectsToHide.Length; i++) {
            _objectsToHide[i].gameObject.hideFlags = _customHideFlags;
        }
    }
}
