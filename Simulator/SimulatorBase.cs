using System;
using UnityEditor;
using UnityEngine;

public abstract class SimulatorBase : MonoBehaviour
{
    public enum SimulatorType
    {
        Face,
        Body
    }

    [NonSerialized]
    public SimulatorType _simulatorType;

    public Transform _filterObject;

    protected virtual bool IsSetUpProperly(){
        return false;
    }

    protected virtual void EnforceObjectStructure(){

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

    protected virtual void Update(){

    }
}
