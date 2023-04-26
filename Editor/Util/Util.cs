#if UNITY_EDITOR
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Task = System.Threading.Tasks.Task;

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
            GameObject variableHolder = new GameObject("VariableHolder");
            variableHolder.transform.SetParent(filter.transform);
            Variables variables = variableHolder.AddComponent<Variables>();
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
        public static async void SetRenderPipeline() {
            RenderPipelineAsset pluginRP =
                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                    $"{PackagePath}/Core/template/UniversalRenderPipelineAsset.asset");
            GraphicsSettings.defaultRenderPipeline = pluginRP;
            //Set a delay cos it wasn't being effected immediately on load.
            await Task.Delay(1000);
            SetMkRendererFeature();
            //Clear console warnings that are logged because of render pipeline setting.
            Debug.ClearDeveloperConsole();
        }
        
        private static void SetMkRendererFeature() {
            UniversalRendererData mkRenderer =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/internal/mkRenderer.asset");
            if (mkRenderer == null) {
                return;
            }
            ScriptableRendererFeature mkFeature = mkRenderer.rendererFeatures[0];
            if (mkFeature == null) {
                return;
            }
            UniversalRendererData renderer =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    $"{PackagePath}/Core/template/UniversalRenderPipelineAsset_Renderer.asset");
            if (!renderer.rendererFeatures.Contains(mkFeature)) {
                renderer.rendererFeatures.Add(mkFeature);
                renderer.SetDirty();
            }
        }
        
        public static void AutoAddPostProcessing(string sceneName) {
            bool success = AssetDatabase.CopyAsset("Assets/internal/FiltaDefaultPostProcess.asset",
                $"Assets/Filters/{sceneName}PostProcess.asset");
            if (!success) {
                return;
            }
            VolumeProfile postProcessData =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>($"Assets/Filters/{sceneName}PostProcess.asset");
            if (postProcessData == null) {
                return;
            }
            GameObject filter = GetFilterObject();
            if (filter != null) {
                GameObject pp = new GameObject("PostProcess");
                pp.transform.SetParent(filter.transform);
                Volume volume = pp.AddComponent<Volume>();
                volume.sharedProfile = postProcessData;
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
        }

        [InitializeOnLoadMethod]
        public static void AddExtraGameViews() {
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone11.x, GameViewUtils.IPhone11.y, "iPhone 11 - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone11Pro.x, GameViewUtils.IPhone11Pro.y, "iPhone 11 Pro - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone11ProMax.x, GameViewUtils.IPhone11ProMax.y, "iPhone 11 Pro Max - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone12Mini.x, GameViewUtils.IPhone12Mini.y, "iPhone 12 Mini - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone12Pro.x, GameViewUtils.IPhone12Pro.y, "iPhone 12 Pro - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhone12ProMax.x, GameViewUtils.IPhone12ProMax.y, "iPhone 12 Pro Max - Filta", false);
            GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewUtils.IPhoneSe.x, GameViewUtils.IPhoneSe.y, "iPhone SE - Filta", false);
        }
    }
}
#endif
