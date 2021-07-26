#if UNITY_EDITOR
using System;
using UnityEngine;

[ExecuteAlways]
public class FilterObjectManager : MonoBehaviour
{
    //Used to ensure the filter object is appropriately named
    private void Update(){
        gameObject.name = "Filter";
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
#endif
