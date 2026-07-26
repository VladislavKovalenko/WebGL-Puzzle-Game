#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class PocketPublisherTool : EditorWindow
{
    const string PREF_OUTPUT = "PocketPublisher.OutputFolder";
    const string PREF_TAB = "PocketPublisher.CurrentTab";
    const string PREF_AUTO_OPEN = "PocketPublisher.AutoOpenFolder";
    const string PREF_CAPTURE_ALPHA = "PocketPublisher.CaptureAlpha";

    enum Tab { Screenshot, Settings }
    enum AspectRatio { Free, Portrait9x16, Portrait10x16, Landscape16x9, Landscape4x3, Square1x1 }

    private Tab currentTab;
    private Vector2 scroll;
    private string outputFolder;
    private bool openFolderAfterSave;
    private static bool captureAlpha; // Статическое поле для доступа из движка захвата

    private readonly string[] tabs = { "Screenshot", "Settings" };

    // Настройки скриншота
    private int screenshotWidth = 1080;
    private int screenshotHeight = 1920;
    private AspectRatio aspect = AspectRatio.Portrait9x16;
    private bool lockAspect = true;
    private bool autoFindCamera = true;
    private Camera captureCamera;

    [MenuItem("Tools/Megxlord Toolbox/Marketing/Pocket Publisher Tool")]
    public static void Open()
    {
        var window = GetWindow<PocketPublisherTool>();
        window.titleContent = new GUIContent("Pocket Publisher");
        window.minSize = new Vector2(700, 550);
        window.Show();
    }

    void OnEnable()
    {
        currentTab = (Tab)EditorPrefs.GetInt(PREF_TAB, 0);
        outputFolder = EditorPrefs.GetString(PREF_OUTPUT, "");
        openFolderAfterSave = EditorPrefs.GetBool(PREF_AUTO_OPEN, true);
        captureAlpha = EditorPrefs.GetBool(PREF_CAPTURE_ALPHA, false);
    }

    void OnDisable()
    {
        EditorPrefs.SetInt(PREF_TAB, (int)currentTab);
        EditorPrefs.SetBool(PREF_AUTO_OPEN, openFolderAfterSave);
        EditorPrefs.SetBool(PREF_CAPTURE_ALPHA, captureAlpha);
    }

    // =========================================================
    // МОДУЛЬ 1: Отрисовка интерфейса окна
    // =========================================================
    void OnGUI()
    {
        GUILayout.Space(8);
        GUILayout.Label("Pocket Publisher Tool", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (string.IsNullOrEmpty(outputFolder))
        {
            if (GUILayout.Button("Select Output Folder", GUILayout.Height(40)))
                AskOutputFolder();
            return;
        }

        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, tabs);
        GUILayout.Space(10);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (currentTab == Tab.Screenshot) DrawScreenshotTab();
        else if (currentTab == Tab.Settings) DrawSettingsTab();
        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        DrawFooter();
    }

    void DrawScreenshotTab()
    {
        GUILayout.Label("Screenshot Engine", EditorStyles.boldLabel);
        
        autoFindCamera = EditorGUILayout.Toggle("Auto Find Camera", autoFindCamera);
        if (!autoFindCamera)
        {
            captureCamera = (Camera)EditorGUILayout.ObjectField("Target Camera", captureCamera, typeof(Camera), true);
        }

        GUILayout.Space(10);
        lockAspect = EditorGUILayout.Toggle("Lock Aspect", lockAspect);

        EditorGUI.BeginDisabledGroup(!lockAspect);
        EditorGUI.BeginChangeCheck();
        aspect = (AspectRatio)EditorGUILayout.EnumPopup("Aspect Ratio", aspect);
        if (EditorGUI.EndChangeCheck()) ApplyAspect(true);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        EditorGUI.BeginChangeCheck();
        screenshotWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", screenshotWidth));
        if (EditorGUI.EndChangeCheck() && lockAspect) ApplyAspect(true);

        EditorGUI.BeginChangeCheck();
        screenshotHeight = Mathf.Max(1, EditorGUILayout.IntField("Height", screenshotHeight));
        if (EditorGUI.EndChangeCheck() && lockAspect) ApplyAspect(false);

        GUILayout.Space(20);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Take Screenshot", GUILayout.Height(40)))
        {
            ExecuteCapture();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        // Кнопка открытия папки прямо на вкладке
        if (GUILayout.Button("Open Screenshot Folder", GUILayout.Height(30)))
        {
            OpenOutputFolder();
        }
    }

    void DrawSettingsTab()
    {
        GUILayout.Label("General Settings", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // Настройка авто-открытия папки
        EditorGUI.BeginChangeCheck();
        openFolderAfterSave = EditorGUILayout.Toggle("Open Folder on Save", openFolderAfterSave);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(PREF_AUTO_OPEN, openFolderAfterSave);
        }

        // Новая настройка сохранения прозрачности
        EditorGUI.BeginChangeCheck();
        captureAlpha = EditorGUILayout.Toggle("Capture Alpha (Transparency)", captureAlpha);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(PREF_CAPTURE_ALPHA, captureAlpha);
        }

        GUILayout.Space(15);
        GUILayout.Label("Output Directory:", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(outputFolder, GUILayout.Height(20));
        
        if (GUILayout.Button("Change Folder")) AskOutputFolder();
    }

    void DrawFooter()
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label(outputFolder, EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Open Folder", GUILayout.Width(120)))
        {
            OpenOutputFolder();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    void OpenOutputFolder()
    {
        if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder))
        {
            EditorUtility.RevealInFinder(outputFolder);
        }
        else
        {
            Debug.LogWarning("[PocketPublisher] Output folder does not exist or not selected.");
        }
    }

    void AskOutputFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
        if (!string.IsNullOrEmpty(folder))
        {
            outputFolder = folder;
            EditorPrefs.SetString(PREF_OUTPUT, outputFolder);
        }
    }

    // =========================================================
    // МОДУЛЬ 2: Математика разрешений
    // =========================================================
    void ApplyAspect(bool fromWidth)
    {
        if (aspect == AspectRatio.Free) return;

        float ratio = GetRatioValue(aspect);
        if (fromWidth)
            screenshotHeight = Mathf.RoundToInt(screenshotWidth / ratio);
        else
            screenshotWidth = Mathf.RoundToInt(screenshotHeight * ratio);
    }

    float GetRatioValue(AspectRatio ar)
    {
        switch (ar)
        {
            case AspectRatio.Portrait9x16: return 9f / 16f;
            case AspectRatio.Portrait10x16: return 10f / 16f;
            case AspectRatio.Landscape16x9: return 16f / 9f;
            case AspectRatio.Landscape4x3: return 4f / 3f;
            case AspectRatio.Square1x1: return 1f;
            default: return 1f;
        }
    }

    // =========================================================
    // МОДУЛЬ 3: Подготовка, Поиск Камеры и Запуск
    // =========================================================
    Camera GetActiveCamera()
    {
        if (!autoFindCamera && captureCamera != null) 
            return captureCamera;

        if (Camera.main != null) 
            return Camera.main;

        Camera[] allCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam.isActiveAndEnabled)
            {
                Debug.LogWarning($"[PocketPublisher] 'MainCamera' tag not found. Using fallback camera: {cam.name}");
                return cam;
            }
        }

        return null;
    }

    void ExecuteCapture()
    {
        Camera cam = GetActiveCamera();
        
        if (cam == null)
        {
            Debug.LogError("[PocketPublisher] No active Camera found in the scene! Please assign one manually or add 'MainCamera' tag.");
            return;
        }

        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        string fileName = $"Capture_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string path = Path.Combine(outputFolder, fileName);

        CaptureEngine.TakeHighResScreenshot(cam, path, screenshotWidth, screenshotHeight);

        AssetDatabase.Refresh();
        Debug.Log($"[PocketPublisher] Screenshot saved: {path}");

        if (openFolderAfterSave)
        {
            EditorApplication.delayCall += () =>
            {
                EditorUtility.RevealInFinder(path);
            };
        }
    }
}

// =========================================================
// МОДУЛЬ 4: Движок захвата (Core Engine)
// =========================================================
public static class CaptureEngine
{
    public static void TakeHighResScreenshot(Camera cam, string path, int width, int height)
    {
        float originalAspect = cam.aspect;
        RenderTexture oldRT = RenderTexture.active;
        RenderTexture oldTarget = cam.targetTexture;

        var modifiedCanvases = new List<Canvas>();
        Canvas[] allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (var c in allCanvases)
        {
            if (c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                modifiedCanvases.Add(c);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.01f; 
            }
        }

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        
        try
        {
            cam.aspect = (float)width / height;
            cam.targetTexture = rt;
            RenderTexture.active = rt;

            cam.Render();

            // Динамический выбор формата текстуры в зависимости от настройки CaptureAlpha
            TextureFormat format = targetAlphaSetting ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            Texture2D tex = new Texture2D(width, height, format, false);
            
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(false); 

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            cam.targetTexture = oldTarget;
            RenderTexture.active = oldRT;
            cam.aspect = originalAspect;

            foreach (var c in modifiedCanvases)
            {
                if (c != null)
                {
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.worldCamera = null;
                }
            }

            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    // Прокси-свойство для чтения статического состояния из UI-окна
    private static bool targetAlphaSetting
    {
        get
        {
            // Читаем напрямую из EditorPrefs, чтобы движок всегда знал актуальное значение
            return EditorPrefs.GetBool("PocketPublisher.CaptureAlpha", false);
        }
    }
}
#endif