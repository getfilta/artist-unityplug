#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

public class GameViewUtils {
    static object gameViewSizesInstance;
    static MethodInfo getGroup;

    static GameViewUtils() {
        // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public enum GameViewSizeType {
        AspectRatio,
        FixedResolution
    }

    private static void SetSize(int index) {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gvWnd = EditorWindow.GetWindow(gvWndType);
        MethodInfo SizeSelectionCallback = gvWndType.GetMethod("SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        SizeSelectionCallback.Invoke(gvWnd, new object[] {index, null});
    }

    static object GetGroup(GameViewSizeGroupType type) {
        return getGroup.Invoke(gameViewSizesInstance, new object[] {(int) type});
    }

    private static string[] GetViewListSize(GameViewSizeGroupType sizeGroupType) {
        var group = GetGroup(sizeGroupType);
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        string[] texts = getDisplayTexts.Invoke(group, null) as string[];
        return texts;
    }

    private static void RemoveCustomSize(int index, GameViewSizeGroupType sizeGroupType) {
        Object group = GetGroup(sizeGroupType);
        MethodInfo removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
        removeCustomSize.Invoke(group, new object[] {index});
    }

    private static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width,
        int height, string text) {
        Object group = GetGroup(sizeGroupType);
        MethodInfo addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        // gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        string assemblyName = "UnityEditor.dll";
        Assembly assembly = Assembly.Load(assemblyName);
        Type gameViewSize = assembly.GetType("UnityEditor.GameViewSize");
        Type gameViewSizeType = assembly.GetType("UnityEditor.GameViewSizeType");
        ConstructorInfo ctor = gameViewSize.GetConstructor(new Type[] {
            gameViewSizeType,
            typeof(int),
            typeof(int),
            typeof(string)
        });
        var newSize = ctor.Invoke(new object[] {(int) viewSizeType, width, height, text});
        addCustomSize.Invoke(group, new object[] {newSize});
    }

    public static void SetGameView(GameViewSizeType viewSizeType, int width,
        int height, string text) {
        GameViewSizeGroupType sizeGroupType = GetGroupType();
        List<string> existingSizes = GetViewListSize(sizeGroupType).ToList();
        int index = existingSizes.FindIndex((s => s.Contains(text)));
        if ( index != -1) {
            RemoveCustomSize(index, sizeGroupType);
        }
        AddCustomSize(viewSizeType, sizeGroupType, width, height, text);
        SetSize(GetViewListSize(sizeGroupType).Length - 1);
    }
    
    public static Vector2 GetMainGameViewSize()
    {
        Type T = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (T != null) {
            MethodInfo getSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",BindingFlags.NonPublic | BindingFlags.Static);
            if (getSizeOfMainGameView is not null) {
                Object res = getSizeOfMainGameView.Invoke(null,null);
                return (Vector2)res;
            }
        }
        Debug.LogError("Could not get main game view size");
        return new Vector2(0, 0);
    }

    private static GameViewSizeGroupType GetGroupType() {
        switch (EditorUserBuildSettings.activeBuildTarget) {
            case BuildTarget.iOS: return GameViewSizeGroupType.iOS;
            case BuildTarget.Android: return GameViewSizeGroupType.Android;
            case BuildTarget.StandaloneWindows: return GameViewSizeGroupType.Standalone;
            case BuildTarget.StandaloneLinux64: return GameViewSizeGroupType.Standalone;
            case BuildTarget.StandaloneOSX: return GameViewSizeGroupType.Standalone;
            case BuildTarget.StandaloneWindows64: return GameViewSizeGroupType.Standalone;
            default: return GameViewSizeGroupType.Standalone;
        }
    }

}
#endif
 
