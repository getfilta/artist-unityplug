using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class HideSimulator : MonoBehaviour
{
    public List<Component> targets;
    public HideFlags customHideFlags;
    public bool showInEditor = false;

    public enum Mode
    {
        GameObject,
        Component
    }

    public Mode setOn = Mode.GameObject;

    private void Start() {
        SetFlags();
    }

    private void Awake() {
        SetFlags();
    }

    [ContextMenu("Set Flags")]
    private void SetFlags()
    {
        if(showInEditor)
        {
            foreach(var target in targets)
            {
                target.gameObject.hideFlags = HideFlags.None;
            }
            return;
        }
        if (setOn == Mode.GameObject)
        {
            foreach(var target in targets)
            {
                target.gameObject.hideFlags = customHideFlags;
            }
        }
        else if (setOn == Mode.Component)
        {
            foreach (var target in targets)
            {
                target.hideFlags = customHideFlags;
            }
        }
    }
}