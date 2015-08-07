using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScriptableObjectUtility
{
    public static void CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    [MenuItem("MOBA/Create/Minion Group")]
    public static void CreateNexusMinionGroupAsset()
    {
        CreateAsset<NexusMinionGroup>();
    }

    [MenuItem("MOBA/Create/Aura Database")]
    public static void CreateAuraDatatebaseAsset()
    {
        CreateAsset<AuraDatabase>();
    }

    [MenuItem("MOBA/Create/Fog Data Asset")]
    public static void CreateFogDataAsset()
    {
        CreateAsset<FogDataAsset>();
    }
}