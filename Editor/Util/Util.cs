#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Filta {
    public static class Util
    {
        public static GameObject GetFilterObject() {
            GameObject filterObject;
            FusionSimulator fusionSimulator = Object.FindObjectOfType<FusionSimulator>();
            if (fusionSimulator != null) {
                filterObject = new GameObject("FilterFusion");
                fusionSimulator.bodySimulator._filterObject.SetParent(filterObject.transform);
                fusionSimulator.faceSimulator._filterObject.SetParent(filterObject.transform);
            } else {
                SimulatorBase simulator = Object.FindObjectOfType<SimulatorBase>();
                filterObject = simulator._filterObject.gameObject;
            }
            return filterObject;
        }
        
        public static bool GenerateFilterPrefab(GameObject filterObject, string savePath) {
            GameObject filterDuplicate = Object.Instantiate(filterObject);
            filterDuplicate.name = "Filter";
            PrefabUtility.SaveAsPrefabAsset(filterDuplicate, savePath, out bool success);
            Object.DestroyImmediate(filterDuplicate);
            FusionSimulator fusionSimulator = Object.FindObjectOfType<FusionSimulator>();
            if (fusionSimulator != null) {
                fusionSimulator.bodySimulator._filterObject.SetParent(null);
                fusionSimulator.faceSimulator._filterObject.SetParent(null);
                fusionSimulator.faceSimulator._filterObject.SetAsLastSibling();
                fusionSimulator.bodySimulator._filterObject.SetAsLastSibling();
                Object.DestroyImmediate(filterObject);
            }

            return success;
        }
        

        [InitializeOnLoadMethod]
        public static void ChangeActiveInputHandling() {
            SerializedObject projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty activeInputHandling = projectSettings.FindProperty("activeInputHandler");
            if (activeInputHandling.intValue is 0 or 2) {
                return;
            }
            activeInputHandling.intValue = 2;
            projectSettings.ApplyModifiedProperties();
        }
    }
}
#endif
