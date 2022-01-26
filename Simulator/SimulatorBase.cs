using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public abstract class SimulatorBase : MonoBehaviour
{
    public enum SimulatorType
    {
        Face,
        Body
    }
    
    public virtual SimulatorType _simulatorType{ get;}

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

    protected virtual void TryAutomaticSetup(){
        
    }
}
