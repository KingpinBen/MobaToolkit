using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameplayArea))]
public sealed class GameplayAreaEditor : Editor
{
    private SerializedProperty _sizeProperty;
    private SerializedProperty _backgroundColorProperty;
    private GameplayArea _target;
    private Vector3[] _verts;

    private static Camera _minimapCamera;
    private static readonly string resourceFolderLocation = Application.dataPath + "/Resources";

    private readonly string minimapImagesLocation = resourceFolderLocation + "/Minimap Images";

    private readonly GUIContent _sizeContent = new GUIContent("Map Size", 
        "This value should be the same as the largest size of either width/length.\ni.e. If a map is 30:20units, this value should be 30.");
    private readonly GUIContent _backContent = new GUIContent("Background Color", 
        "If you'd prefer to not have a colored background you can set the alpha in the color to 0 and it will be hidden");

    void OnEnable()
    {
        _target = target as GameplayArea;
        _verts = new Vector3[4];

        _sizeProperty = serializedObject.FindProperty("_size");
        _backgroundColorProperty = serializedObject.FindProperty("_backgroundColor");
    }

    public override void OnInspectorGUI()
    {
        _sizeProperty.floatValue = EditorGUILayout.FloatField(_sizeContent, _sizeProperty.floatValue);

        _backgroundColorProperty.colorValue = EditorGUILayout.ColorField(_backContent, _backgroundColorProperty.colorValue);

        if (GUILayout.Button("Update Minimap Image"))
        {
            UpdateMinimapImage();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateMinimapImage()
    {
        //  We have to make sure the Resources folder exists so we can 
        //  grab the minimap image if the scene doesn't start with it set.
        CreateRequiredFolders();

        //  Make the camera and place it correctly
        CreateMinimapImageCamera();

        string sceneName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
        string endPath = string.Format("{0}/{1}.png", minimapImagesLocation, sceneName);

        //  Make a disposable renderTarget to draw the scene to
        RenderTexture rt = RenderTexture.GetTemporary(512, 512, 16, RenderTextureFormat.ARGB32);
        _minimapCamera.targetTexture = rt;
        _minimapCamera.Render();

        /*  We set the current render target to the one we just rendered to because
            (Texture2D).ReadPixels() uses the current RT to read the pixels from. */
        RenderTexture.active = rt;

        Texture2D image = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0, false);
        image.Apply();

        /*  If there is a file in the location with the same name, delete it
            and then make a new file with the data for the new minimap image. */
        if (File.Exists(endPath))
            File.Delete(endPath);

        File.WriteAllBytes(endPath, image.EncodeToPNG());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //  Clean up
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        _minimapCamera.targetTexture = null;
        DestroyImmediate(image);
        DestroyImmediate(_minimapCamera.gameObject);

        Debug.Log(string.Format("Minimap image has been created: {0}", endPath));
    }

    void CreateRequiredFolders()
    {
        if (!Directory.Exists(resourceFolderLocation))
            Directory.CreateDirectory(resourceFolderLocation);

        if (!Directory.Exists(minimapImagesLocation))
            Directory.CreateDirectory(minimapImagesLocation);
    }

    void CreateMinimapImageCamera()
    {
        GameObject camObject = new GameObject
        {
            hideFlags = HideFlags.DontSave
        };

        camObject.SetActive(false);

        _minimapCamera = camObject.AddComponent<Camera>();
        _minimapCamera.orthographic = true;
        _minimapCamera.backgroundColor = _backgroundColorProperty.colorValue;
        _minimapCamera.clearFlags = CameraClearFlags.SolidColor;

        // Todo: May want to also apply any image effects on the game camera
        //       so it also looks the same as in-game

        var pos = _target.transform.position;

        pos.y = 30.0f;

        _minimapCamera.transform.position = pos;
        _minimapCamera.transform.rotation = Quaternion.LookRotation(Vector3.down);

        _minimapCamera.aspect = 1.0f;
        _minimapCamera.orthographicSize = _sizeProperty.floatValue * .5f;
    }

    void OnSceneGUI()
    {
        float half = _sizeProperty.floatValue *.5f;
        _verts[0] = _target.transform.position + new Vector3(-half, 0, half);
        _verts[1] = _target.transform.position + new Vector3(half, 0, half);
        _verts[2] = _target.transform.position + new Vector3(half, 0, -half);
        _verts[3] = _target.transform.position + new Vector3(-half, 0, -half);

        Handles.DrawSolidRectangleWithOutline(_verts, Color.yellow * .5f, Color.red);
    }
}
