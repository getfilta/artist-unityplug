using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BodySimulator : SimulatorBase
{
    public override SimulatorType _simulatorType => SimulatorType.Body;

    [SerializeField]
    private Transform _bodyTracker;

    [SerializeField]
    private Transform _wristTracker;

    protected override bool IsSetUpProperly(){
        return _filterObject != null;
    }

    protected override void EnforceObjectStructure(){
        _filterObject.name = "FilterBody";
        _bodyTracker.name = "BodyTracker";
        _wristTracker.name = "WristTracker";
    }

    private void OnRenderObject(){
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying){
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
    protected override void Update(){
        EnforceObjectStructure();
    }
}
