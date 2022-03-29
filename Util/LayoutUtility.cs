using UnityEditor;
using System.IO;
using System.Reflection;
using UnityEngine;
using Type = System.Type;

public static class LayoutUtility {

	private static readonly MethodInfo LoadWindowLayout;

	static LayoutUtility() {
		Type windowLayout = Type.GetType("UnityEditor.WindowLayout,UnityEditor");
		if (windowLayout != null) {
			LoadWindowLayout = windowLayout.GetMethod("LoadWindowLayout",
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null,
				new Type[] {typeof(string), typeof(bool)}, null);
		}
	}

	// Load window layout from asset file. `assetPath` must be relative to project directory.
	public static bool LoadLayoutFromAsset(string assetPath) {
		if (LoadWindowLayout == null) {
			return false;
		}
		LoadWindowLayout.Invoke(null, new object[] {assetPath, true});
		return true;
	}
}