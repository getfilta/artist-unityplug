using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BodySimulator : SimulatorBase
{
    
    [SerializeField]
    private Transform _bodyTracker;

    [SerializeField]
    private Transform _wristTracker;

    private void OnEnable(){
        _simulatorType = SimulatorType.Body;
    }

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
