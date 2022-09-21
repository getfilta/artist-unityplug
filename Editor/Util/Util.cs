#if UNITY_EDITOR
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Filta {
    public static class Util
    {
        private const string PackagePath = "Packages/com.getfilta.artist-unityplug";
        private const string FaceTexturePath = "Packages/com.getfilta.artist-unityplug/Assets/Textures/FaceTexture.renderTexture";
        private const string CameraFeedPath = "Packages/com.getfilta.artist-unityplug/Assets/Textures/CameraFeed.renderTexture";
        private const string BodySegPath = "Packages/com.getfilta.artist-unityplug/Assets/Textures/BodySegmentationStencil.renderTexture";
        private const string MockPath = "Assets/TextureCheckerFilter.prefab";
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
            SimulatorBase simulator = Object.FindObjectOfType<SimulatorBase>();
            GameObject filterDuplicate = Object.Instantiate(filterObject);
            filterDuplicate.name = "Filter";
            HandleTextures(filterDuplicate, simulator);
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

        private static void HandleTextures(GameObject filter, SimulatorBase simulator) {
            Variables variables = filter.AddComponent<Variables>();
            int componentCount = filter.GetComponents<Component>().Length;
            PrefabUtility.SaveAsPrefabAsset(filter, MockPath, out bool success);
            string[] paths = AssetDatabase.GetDependencies(MockPath, true);
            if (paths.Contains(FaceTexturePath)) {
                if (simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
                    Simulator sim = simulator.gameObject.GetComponent<Simulator>();
                    variables.declarations.Set("FaceTexture", sim._faceTexture);
                    variables.declarations.Set("CameraFeed", simulator._cameraFeed);
                    variables.declarations.Set("BodySegmentation", simulator._stencilRT);
                }
            } else {
                if (paths.Contains(CameraFeedPath)) {
                    variables.declarations.Set("CameraFeed", simulator._cameraFeed);
                }
                if (paths.Contains(BodySegPath)) {
                    variables.declarations.Set("BodySegmentation", simulator._stencilRT);
                }
            }
            for (int i = 0; i < componentCount; i++) {
                bool top = UnityEditorInternal.ComponentUtility.MoveComponentUp(variables);
                if (!top) {
                    break;
                }
            }
            AssetDatabase.DeleteAsset(MockPath);
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

        [InitializeOnLoadMethod]
        public static void SetRenderPipeline() {
            RenderPipelineAsset pluginRP =
                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                    $"{PackagePath}/Core/template/UniversalRenderPipelineAsset.asset");
            GraphicsSettings.defaultRenderPipeline = pluginRP;
        }
    }
}
#endif
