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
    private List<Component> _objectsToHide;
    [SerializeField]
    private HideFlags _customHideFlags;

    public virtual bool IsSetUpProperly() {
        return false;
    }

    protected virtual void EnforceObjectStructure() {

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
        Debug.Log("SetFlags");
        if (_developerMode || initialSetup) {
            Debug.Log("dev or initial setup");

            foreach (var target in _objectsToHide) {
                target.gameObject.hideFlags = HideFlags.None;
            }
            return;
        }
        foreach (var target in _objectsToHide) {
            target.gameObject.hideFlags = _customHideFlags;
        }
    }
}
