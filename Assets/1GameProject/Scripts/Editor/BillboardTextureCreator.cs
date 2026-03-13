using UnityEngine;
using UnityEditor;
using System.IO;

public class BillboardTextureCreator : EditorWindow
{
    private GameObject objectToRender;
    private int textureSize = 2048;
    private string textureName = "Billboard_Texture";
    
    [MenuItem("Tools/Billboard Texture Creator")]
    static void Init()
    {
        BillboardTextureCreator window = GetWindow<BillboardTextureCreator>();
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Billboard Texture Generator", EditorStyles.boldLabel);
        
        objectToRender = (GameObject)EditorGUILayout.ObjectField("Object to Render", objectToRender, typeof(GameObject), true);
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        textureName = EditorGUILayout.TextField("Texture Name", textureName);
        
        if (GUILayout.Button("Generate Billboard Texture") && objectToRender != null)
        {
            GenerateBillboardTexture();
        }
    }
    
    void GenerateBillboardTexture()
    {
        // Создаем временную сцену
        GameObject tempObject = Instantiate(objectToRender);
        tempObject.transform.position = Vector3.zero;
        
        // Создаем камеру
        GameObject cameraGO = new GameObject("Temp Camera");
        Camera renderCamera = cameraGO.AddComponent<Camera>();
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.orthographic = true;
        
        // Позиционируем камеру
        Bounds bounds = GetObjectBounds(tempObject);
        float distance = bounds.size.magnitude * 2f;
        renderCamera.transform.position = bounds.center + Vector3.back * distance + Vector3.up * 0.5f;
        renderCamera.transform.LookAt(bounds.center);
        renderCamera.orthographicSize = bounds.extents.magnitude * 1.1f;
        
        // Создаем RenderTexture временно
        RenderTexture rt = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
        renderCamera.targetTexture = rt;
        
        // Рендерим
        renderCamera.Render();
        
        // Конвертируем в Texture2D
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true); // true для мипмапов!
        texture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        texture.Apply(true); // Генерируем мипмапы
        
        // Сохраняем как PNG
        byte[] pngData = texture.EncodeToPNG();
        string path = $"Assets/Textures/Billboards/{textureName}.png";
        
        // Создаем директорию если нужно
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();
        
        // Настраиваем импорт текстуры
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true; // Включаем мипмапы
            importer.filterMode = FilterMode.Trilinear; // Для плавного перехода между мипмапами
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = textureSize;
            
            // Настройки для качественных мипмапов
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.fadeout = false;
            
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
        
        // Очистка
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(cameraGO);
        DestroyImmediate(tempObject);
        
        Debug.Log($"Billboard texture saved to: {path}");
        
        // Выделяем созданную текстуру
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    
    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
}