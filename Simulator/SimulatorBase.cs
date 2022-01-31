using System.IO;
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
    
    protected string _filePath{ get; set; }

    public Transform _filterObject;

    public virtual bool IsSetUpProperly(){
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

    protected virtual void OnEnable(){
        _filePath = Path.GetFullPath("Packages/com.getfilta.artist-unityplug");
    }

    protected virtual void Update(){

    }

    public virtual void TryAutomaticSetup(){
        
    }
}
