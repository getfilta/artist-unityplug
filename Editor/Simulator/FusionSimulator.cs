using System;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class FusionSimulator : MonoBehaviour {
    #if UNITY_EDITOR
    [NonSerialized]
    public SimulatorBase.SimulatorType activeType;
    
    public Simulator faceSimulator;
    public BodySimulator bodySimulator;

    public GameObject defaultLight;

    private bool _developerMode;
    
    public bool DynamicLightOn {
        get => defaultLight.activeSelf;
        set => defaultLight.SetActive(value);
    }

    public SimulatorBase GetActiveSimulator() {
        switch (activeType) {
            case SimulatorBase.SimulatorType.Body:
                return bodySimulator;
            case SimulatorBase.SimulatorType.Face:
                return faceSimulator;
            default: return faceSimulator;
        }
    }

    private void OnEnable() {
        SetFlags();
    }

    [ContextMenu("Toggle Visibility")]
    protected void ContextMenu() {
        _developerMode = !_developerMode;
        SetFlags(_developerMode);
    }

    private void SetFlags(bool forceVisibility = false) {
        gameObject.hideFlags = forceVisibility? HideFlags.None: HideFlags.NotEditable;
        defaultLight.hideFlags = forceVisibility ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.NotEditable;
    }
    #endif
}
