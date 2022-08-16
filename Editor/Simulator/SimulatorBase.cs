using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;

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
    public Transform mainTracker;

    [SerializeField]
    private HideFlags _customHideFlags;
    private Transform[] _objectsToHide;
    private bool _developerMode = false;
    
    [SerializeField]
    protected RawImage _remoteFeed;
    
    [SerializeField]
    protected Camera mainCamera;

    [SerializeField]
    protected GameObject defaultLight;
    
    [NonSerialized]
    public bool isPlaying;

    [NonSerialized]
    public bool dynamicLightOn;
    
    [NonSerialized]
    public RenderTexture _stencilRT;
    [NonSerialized]
    public RenderTexture _cameraFeed;
    
    protected long _pauseTime;
    protected DateTime _startTime;
    
    protected bool previousVisStatus;
    protected bool previousLightStatus;

    protected const string PackagePath = "Packages/com.getfilta.artist-unityplug";

    public virtual bool IsSetUpProperly() {
        return false;
    }

    protected virtual void EnforceObjectStructure() {

    }

    protected virtual void Awake() {
        _objectsToHide = GetComponentsInChildren<Transform>(true);
    }

    private void Start() {
        RemoveLegacyCamera();
    }

    private void OnRenderObject() {
        // Ensure continuous Update calls.
        if (!Application.isPlaying) {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }

    //This is used to handle filters created with legacy plugin versions which
    //had the camera outside of the simulator prefab.
    private void RemoveLegacyCamera() {
        Scene scene = SceneManager.GetActiveScene();
        List<GameObject> rootObjects = new(scene.rootCount);
        scene.GetRootGameObjects(rootObjects);
        for (int i = 0; i < rootObjects.Count; i++) {
            if (rootObjects[i].TryGetComponent(out Camera cam)) {
                if (rootObjects[i].CompareTag("MainCamera")) {
                    DestroyImmediate(rootObjects[i]);
                }
            }
        }
    }

    protected virtual void OnEnable() {
        dynamicLightOn = defaultLight.activeSelf;
        _filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug");
        if (!EditorApplication.isPlaying) {
            _remoteFeed.gameObject.SetActive(false);
        }
    }

    protected virtual void Update() {

    }

    public virtual void TryAutomaticSetup() {

    }

    public virtual void Disable() {
        mainCamera.gameObject.SetActive(false);
        _filterObject.gameObject.SetActive(false);
        previousLightStatus = dynamicLightOn;
        dynamicLightOn = false;
    }

    public virtual void Enable() {
        mainCamera.gameObject.SetActive(true);
        _filterObject.gameObject.SetActive(true);
        dynamicLightOn = previousLightStatus;
    }

    protected virtual void Playback(long currentTime) {
        
    }

    public virtual void PauseSimulator() {
        _pauseTime = (long)(DateTime.Now - _startTime).TotalMilliseconds + _pauseTime;
        isPlaying = false;
    }

    public virtual void ResumeSimulator() {
        isPlaying = true;
    }

    public virtual void ResetSimulator() {
        _startTime = DateTime.Now;
        _pauseTime = 0;
    }

    public virtual void StopSimulator() {
        ResetSimulator();
        PauseSimulator();
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

        mainCamera.gameObject.hideFlags = HideFlags.NotEditable;
    }
    #endif
}
