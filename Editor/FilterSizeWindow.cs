#if UNITY_EDITOR
using System;
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
        
        [MenuItem("Filta/Size Window", false, 2)]
        static void InitFloating() {
            FilterSizeWindow window = (FilterSizeWindow)GetWindow(typeof(FilterSizeWindow), true, $"Filta Size Window");
            window.ShowUtility();
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
            DevPanel devPanel = (DevPanel)GetWindow(typeof(DevPanel), false, $"Filta: Artist Panel - {DevPanel.GetVersionNumber()}");
            GameObject filterObject = devPanel.GetFilterObject();
            devPanel.GenerateFilterPrefab(filterObject, VariantTempSave);
            AssetDatabase.ExportPackage(PackagePaths, FileName,
                ExportPackageOptions.IncludeDependencies);
            string pathToPackage = Path.Combine(Path.GetDirectoryName(Application.dataPath), FileName);
            FileInfo fileInfo = new FileInfo(pathToPackage);
            if (fileInfo.Length > UploadLimit) {
                string readout = CheckForOversizeFiles(PackagePaths);
                _result =
                    $"Your Filter is over {UploadLimit / 1000000}MB, please reduce the size. These are the files that might be causing this. {readout}";
            } else {
                _result = $"Your Filter is {fileInfo.Length / 1000000f:#.##}MB. This is within the upload limit";
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
