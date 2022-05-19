#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Filta {
    public class FilterSizeWindow : EditorWindow
    {
        public const long UploadLimit = 100000000;
        private const string FileName = "sizeCheck.unitypackage";
        private const string VariantTempSave = "Assets/SizeCheckFilter.prefab";
        private readonly string[] PackagePaths = { "Assets/pluginInfo.json", VariantTempSave };

        private string _result;
        
        [MenuItem("Filta/Asset Size Summary", false, 2)]
        static void InitFloating() {
            FilterSizeWindow window = (FilterSizeWindow)GetWindow(typeof(FilterSizeWindow), false, "Asset Size Summary");
            window.Show();
        }

        private void OnGUI() {
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(_result, textStyle);
            if (GUILayout.Button("Check Filter Size")) {
                RefreshCheck();
            }
        }

        private void RefreshCheck() {
            GameObject filterObject = Util.GetFilterObject();
            bool success = Util.GenerateFilterPrefab(filterObject, VariantTempSave);
            if (!success) {
                Debug.LogError("Failed to generate filter prefab.");
                _result = "Could not check Asset size.";
                return;
            }
            AssetDatabase.ExportPackage(PackagePaths, FileName,
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), FileName);
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > UploadLimit) {
                string readout = CheckForOversizeFiles(PackagePaths);
                //Your filter is {size}MB. This is over the {uploadlimit}MB limit. Please reduce the size. {readout}
                _result =
                    $"Your Filter is {fileInfo.Length / 1000000f:#.##}MB. This is over the {UploadLimit / 1000000}MB limit. {readout}";
            } else {
                
                _result = $"Your Filter is {fileInfo.Length / 1000000f:#.##}MB.";
            }
            File.Delete(pathToPackage);
            AssetDatabase.DeleteAsset(VariantTempSave);
        }

        public static string CheckForOversizeFiles(string[] path) {
            string[] pathNames = AssetDatabase.GetDependencies(path);
            string readout = "";
            Dictionary<string, long> fileSizes = new();
            for (int i = 0; i < pathNames.Length; i++) {
                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), pathNames[i]);
                FileInfo fileInfo = new(fullPath);
                fileSizes.Add(pathNames[i], fileInfo.Length);
            }
            fileSizes = new Dictionary<string, long>(fileSizes.OrderByDescending(pair => pair.Value));
            long limit = 0;
            foreach (KeyValuePair<string, long> file in fileSizes) {
                readout += $"\n {file.Key} - {file.Value / 1000000f:#.##}MB";
                limit += file.Value;
                if (limit > UploadLimit && file.Value < UploadLimit) {
                    break;
                }
            }
            return readout;
        }
    }
}
#endif
