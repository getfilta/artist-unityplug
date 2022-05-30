using System;
using UnityEngine;

public class FusionSimulator : MonoBehaviour {
    #if UNITY_EDITOR
    [NonSerialized]
    public SimulatorBase.SimulatorType activeType;
    
    public Simulator faceSimulator;
    public BodySimulator bodySimulator;

    private bool _developerMode;

    public SimulatorBase GetActiveSimulator() {
        switch (activeType) {
            case SimulatorBase.SimulatorType.Body:
                return bodySimulator;
            case SimulatorBase.SimulatorType.Face:
                return faceSimulator;
            default: return faceSimulator;
        }
    }

    [ContextMenu("Toggle Visibility")]
    protected void ContextMenu() {
        _developerMode = !_developerMode;
        gameObject.hideFlags = _developerMode? HideFlags.None: HideFlags.NotEditable;
    }
    #endif
}
