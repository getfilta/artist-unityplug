using System;
using UnityEngine;

public class FusionSimulator : MonoBehaviour {
    [NonSerialized]
    public SimulatorBase.SimulatorType activeType;
    
    public Simulator faceSimulator;
    public BodySimulator bodySimulator;

    private bool _developerMode;
    
    [ContextMenu("Toggle Visibility")]
    protected void ContextMenu() {
        _developerMode = !_developerMode;
        gameObject.hideFlags = _developerMode? HideFlags.None: HideFlags.NotEditable;
    }
}
