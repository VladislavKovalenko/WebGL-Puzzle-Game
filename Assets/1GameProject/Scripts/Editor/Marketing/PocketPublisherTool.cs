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

    enum Tab
    {
        Screenshot,
        Gif,
        Batch,
        Settings
    }

    enum CaptureMode
    {
        Auto,
        CameraRender,
        GameView
    }

    enum AspectRatio
    {
        Free,
        Portrait9x16,
        Portrait10x16,
        Landscape16x9,
        Landscape4x3,
        Square1x1
    }

    private Tab currentTab;

    private Vector2 scroll;

    private string outputFolder;

    private readonly string[] tabs =
    {
        "Screenshot",
        "GIF",
        "Batch",
        "Settings"
    };

    // ---------------------------------------------------------

    [MenuItem("Tools/Megxlord Toolbox/Marketing/Pocket Publisher Tool")]
    public static void Open()
    {
        var window =
            GetWindow<PocketPublisherTool>();

        window.titleContent =
            new GUIContent("Pocket Publisher");

        window.minSize =
            new Vector2(700, 550);

        window.Show();
    }

    // ---------------------------------------------------------

    void OnEnable()
    {
        currentTab =
            (Tab)EditorPrefs.GetInt(PREF_TAB, 0);

        outputFolder =
            EditorPrefs.GetString(PREF_OUTPUT, "");

        if (string.IsNullOrEmpty(outputFolder))
        {
            AskOutputFolder();
        }
    }

    // ---------------------------------------------------------

    void OnDisable()
    {
        EditorPrefs.SetInt(
            PREF_TAB,
            (int)currentTab);
    }

    // ---------------------------------------------------------

    void OnGUI()
    {
        DrawHeader();

        if (string.IsNullOrEmpty(outputFolder))
        {
            DrawNoFolder();

            return;
        }

        DrawTabs();

        GUILayout.Space(10);

        scroll =
            EditorGUILayout.BeginScrollView(scroll);

        switch (currentTab)
        {
            case Tab.Screenshot:
                DrawScreenshotTab();
                break;

            case Tab.Gif:
                DrawGifTab();
                break;

            case Tab.Batch:
                DrawBatchTab();
                break;

            case Tab.Settings:
                DrawSettingsTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        DrawFooter();
    }

    // ---------------------------------------------------------

    void DrawHeader()
    {
        GUILayout.Space(8);

        GUILayout.Label(
            "Pocket Publisher Tool",
            EditorStyles.boldLabel);

        GUILayout.Label(
            "Version 1.0",
            EditorStyles.miniLabel);

        GUILayout.Space(5);
    }

    // ---------------------------------------------------------

    void DrawTabs()
    {
        int selected =
            GUILayout.Toolbar(
                (int)currentTab,
                tabs);

        currentTab =
            (Tab)selected;
    }

    // ---------------------------------------------------------

    void DrawFooter()
    {
        GUILayout.Space(5);

        EditorGUILayout.LabelField(
            "",
            GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        GUILayout.Label(
            outputFolder,
            EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
            "Open Folder",
            GUILayout.Width(120)))
        {
            EditorUtility.RevealInFinder(outputFolder);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(5);
    }

    // ---------------------------------------------------------

    void DrawNoFolder()
    {
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical("box");

        GUILayout.Label(
            "Output folder is not selected.",
            EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.Label(
            "Please choose where screenshots,\nGIFs and future exports will be saved.");

        GUILayout.Space(15);

        if (GUILayout.Button(
            "Select Folder",
            GUILayout.Height(40)))
        {
            AskOutputFolder();
        }

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
    }

    // ---------------------------------------------------------

    void AskOutputFolder()
    {
        string folder =
            EditorUtility.OpenFolderPanel(
                "Pocket Publisher Output Folder",
                "",
                "");

        if (string.IsNullOrEmpty(folder))
            return;

        outputFolder = folder;

        EditorPrefs.SetString(
            PREF_OUTPUT,
            outputFolder);
    }

    // =========================================================
    // Screenshot
    // =========================================================

    int screenshotWidth = 1080;
    int screenshotHeight = 1920;

    CaptureMode captureMode = CaptureMode.Auto;
    AspectRatio aspect = AspectRatio.Portrait9x16;

    bool lockAspect = true;

    bool autoFindCamera = true;
    Camera captureCamera;

    bool openFolderAfterSave = true;

    void DrawScreenshotTab()
    {
        GUILayout.Label("Screenshot", EditorStyles.boldLabel);
        GUILayout.Space(10);

        captureMode =
            (CaptureMode)EditorGUILayout.EnumPopup(
                "Capture Mode",
                captureMode);

        autoFindCamera =
            EditorGUILayout.Toggle(
                "Auto Find Camera",
                autoFindCamera);

        if (!autoFindCamera)
        {
            captureCamera =
                (Camera)EditorGUILayout.ObjectField(
                    "Camera",
                    captureCamera,
                    typeof(Camera),
                    true);
        }

        GUILayout.Space(10);

        lockAspect =
            EditorGUILayout.Toggle(
                "Lock Aspect",
                lockAspect);

        EditorGUI.BeginDisabledGroup(!lockAspect);
        aspect =
            (AspectRatio)EditorGUILayout.EnumPopup(
                "Aspect",
                aspect);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // Width
        EditorGUI.BeginChangeCheck();
        int newWidth =
            EditorGUILayout.IntField(
                "Width",
                screenshotWidth);

        if (EditorGUI.EndChangeCheck())
        {
            screenshotWidth = Mathf.Max(1, newWidth);

            if (lockAspect && aspect != AspectRatio.Free)
                ApplyAspectFromWidth();
        }

        // Height
        EditorGUI.BeginChangeCheck();
        int newHeight =
            EditorGUILayout.IntField(
                "Height",
                screenshotHeight);

        if (EditorGUI.EndChangeCheck())
        {
            screenshotHeight = Mathf.Max(1, newHeight);

            if (lockAspect && aspect != AspectRatio.Free)
                ApplyAspectFromHeight();
        }

        openFolderAfterSave =
            EditorGUILayout.Toggle(
                "Open Folder",
                openFolderAfterSave);

        GUILayout.Space(20);

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button(
            "Take Screenshot",
            GUILayout.Height(40)))
        {
            TakeScreenshot();
        }

        GUI.backgroundColor = Color.white;
    }

    // =========================================================
    // Screenshot Logic
    // =========================================================

    Camera GetCaptureCamera()
    {
        if (!autoFindCamera)
            return captureCamera;

        if (Camera.main != null)
        {
            Debug.Log("Using Camera.main: " + Camera.main.name);
            return Camera.main;
        }

        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsSortMode.None);

        Debug.Log("Camera.main == NULL");
        Debug.Log("Found cameras: " + cameras.Length);

        foreach (var cam in cameras)
        {
            Debug.Log(
                $"Camera: {cam.name} | " +
                $"Tag={cam.tag} | " +
                $"Enabled={cam.enabled} | " +
                $"Active={cam.gameObject.activeInHierarchy}");
        }

        // Fallback
        foreach (var cam in cameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                Debug.Log("Fallback camera: " + cam.name);
                return cam;
            }
        }

        return null;
    }

    void TakeScreenshot()
    {
        if (string.IsNullOrEmpty(outputFolder))
        {
            Debug.LogError("Output folder is not selected.");
            return;
        }

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        Camera cam = GetCaptureCamera();

        if (cam == null)
        {
            Debug.LogError("No Capture Camera found.");
            return;
        }

        string fileName =
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        string path =
            Path.Combine(outputFolder, fileName + ".png");

        switch (captureMode)
        {
            case CaptureMode.Auto:
                if (HasOverlayCanvas())
                    CaptureGameView(path);
                else
                    CaptureCamera(cam, path);
                break;

            case CaptureMode.CameraRender:
                CaptureCamera(cam, path);
                break;

            case CaptureMode.GameView:
                CaptureGameView(path);
                break;
        }

        AssetDatabase.Refresh();

        if (openFolderAfterSave)
        {
            EditorApplication.delayCall += () =>
            {
                EditorUtility.RevealInFinder(path);
            };
        }
    }

    void ApplyAspectFromWidth()
    {
        switch (aspect)
        {
            case AspectRatio.Portrait9x16:
                screenshotHeight = Mathf.RoundToInt(screenshotWidth * 16f / 9f);
                break;

            case AspectRatio.Portrait10x16:
                screenshotHeight = Mathf.RoundToInt(screenshotWidth * 16f / 10f);
                break;

            case AspectRatio.Landscape16x9:
                screenshotHeight = Mathf.RoundToInt(screenshotWidth * 9f / 16f);
                break;

            case AspectRatio.Landscape4x3:
                screenshotHeight = Mathf.RoundToInt(screenshotWidth * 3f / 4f);
                break;

            case AspectRatio.Square1x1:
                screenshotHeight = screenshotWidth;
                break;
        }
    }

    void ApplyAspectFromHeight()
    {
        switch (aspect)
        {
            case AspectRatio.Portrait9x16:
                screenshotWidth = Mathf.RoundToInt(screenshotHeight * 9f / 16f);
                break;

            case AspectRatio.Portrait10x16:
                screenshotWidth = Mathf.RoundToInt(screenshotHeight * 10f / 16f);
                break;

            case AspectRatio.Landscape16x9:
                screenshotWidth = Mathf.RoundToInt(screenshotHeight * 16f / 9f);
                break;

            case AspectRatio.Landscape4x3:
                screenshotWidth = Mathf.RoundToInt(screenshotHeight * 4f / 3f);
                break;

            case AspectRatio.Square1x1:
                screenshotWidth = screenshotHeight;
                break;
        }
    }

    bool HasOverlayCanvas()
    {
        Canvas[] canvases =
            FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (var c in canvases)
        {
            if (c != null &&
                c.isActiveAndEnabled &&
                c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return true;
            }
        }

        return false;
    }

    void CaptureCamera(Camera cam, string path)
    {
        if (cam == null)
        {
            Debug.LogError("Capture Camera is NULL.");
            return;
        }

        RenderTexture oldRT = RenderTexture.active;
        RenderTexture oldTarget = cam.targetTexture;

        RenderTexture rt = new RenderTexture(
            screenshotWidth,
            screenshotHeight,
            24,
            RenderTextureFormat.ARGB32);

        Texture2D tex = new Texture2D(
            screenshotWidth,
            screenshotHeight,
            TextureFormat.RGBA32,
            false);

        try
        {
            cam.targetTexture = rt;
            RenderTexture.active = rt;

            cam.Render();

            tex.ReadPixels(
                new Rect(0, 0, screenshotWidth, screenshotHeight),
                0, 0);

            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);

            Debug.Log("Saved Screenshot:\n" + path);
        }
        finally
        {
            cam.targetTexture = oldTarget;
            RenderTexture.active = oldRT;

            DestroyImmediate(rt);
            DestroyImmediate(tex);
        }
    }

    void CaptureGameView(string path)
    {
        // TODO: нормальный захват GameView
        // Пока fallback
        ScreenCapture.CaptureScreenshot(path);
        Debug.LogWarning(
            "GameView capture uses ScreenCapture fallback.\n" +
            "Width/Height may be ignored in this mode.");
    }

    // =========================================================
    // GIF
    // =========================================================

    int gifFPS = 15;

    float gifDuration = 5;

    float gifDelay = 0;

    bool gifLoop = true;

    void DrawGifTab()
    {
        GUILayout.Label(
            "GIF Recorder",
            EditorStyles.boldLabel);

        GUILayout.Space(10);

        gifFPS =
            EditorGUILayout.IntSlider(
                "FPS",
                gifFPS,
                5,
                60);

        gifDuration =
            EditorGUILayout.FloatField(
                "Duration",
                gifDuration);

        gifDelay =
            EditorGUILayout.FloatField(
                "Delay",
                gifDelay);

        gifLoop =
            EditorGUILayout.Toggle(
                "Loop",
                gifLoop);

        GUILayout.Space(20);

        if (GUILayout.Button(
            "Record GIF",
            GUILayout.Height(40)))
        {
            Debug.Log(
                "GIF recorder will be implemented in Part 2");
        }
    }

    // =========================================================
    // Batch
    // =========================================================

    private List<SceneAsset> scenes = new List<SceneAsset>();

    private Vector2 sceneScroll;

    private bool batchScreenshot = true;
    private bool batchGif = false;

    void DrawBatchTab()
    {
        GUILayout.Label(
            "Scene Batch",
            EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Scene"))
        {
            scenes.Add(null);
        }

        if (GUILayout.Button("Clear"))
        {
            scenes.Clear();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        sceneScroll =
            EditorGUILayout.BeginScrollView(
                sceneScroll,
                GUILayout.Height(220));

        for (int i = 0; i < scenes.Count; i++)
        {
            GUILayout.BeginHorizontal();

            scenes[i] =
                (SceneAsset)EditorGUILayout.ObjectField(
                    scenes[i],
                    typeof(SceneAsset),
                    false);

            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                scenes.RemoveAt(i);
                i--;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        batchScreenshot =
            EditorGUILayout.Toggle(
                "Take Screenshot",
                batchScreenshot);

        batchGif =
            EditorGUILayout.Toggle(
                "Record GIF",
                batchGif);

        GUILayout.Space(15);

        GUI.backgroundColor = Color.yellow;

        if (GUILayout.Button(
            "Run Batch",
            GUILayout.Height(40)))
        {
            RunBatch();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        EditorGUILayout.HelpBox(
@"Future improvements

• Iterate Localizations
• Auto wait before capture
• Multiple resolutions
• Multiple aspect ratios
• Auto naming
• Video export",
            MessageType.Info);
    }

    void RunBatch()
    {
        Debug.Log(
            "Batch system will be implemented.");

        foreach (var s in scenes)
        {
            if (s == null)
                continue;

            Debug.Log("Scene : " + s.name);
        }

        /*
            TODO

            Future version:

            foreach(Scene)
            {
                foreach(Localization)
                {
                    OpenScene()

                    Wait()

                    Screenshot()

                    GIF()

                    Video()
                }
            }
        */
    }

    // =========================================================
    // Settings
    // =========================================================

    void DrawSettingsTab()
    {
        GUILayout.Label(
            "Settings",
            EditorStyles.boldLabel);

        GUILayout.Space(15);

        EditorGUILayout.LabelField(
            "Output Folder",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            outputFolder,
            GUILayout.Height(40));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Change Folder"))
        {
            AskOutputFolder();
        }

        if (GUILayout.Button("Open Folder"))
        {
            EditorUtility.RevealInFinder(outputFolder);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        if (GUILayout.Button("Reset Settings"))
        {
            if (EditorUtility.DisplayDialog(
                "Pocket Publisher",
                "Reset all settings?",
                "Yes",
                "No"))
            {
                EditorPrefs.DeleteKey(PREF_OUTPUT);
                EditorPrefs.DeleteKey(PREF_TAB);

                outputFolder = "";

                AskOutputFolder();
            }
        }

        GUILayout.Space(30);

        EditorGUILayout.HelpBox(
@"Pocket Publisher Tool

Version 1.0

Future modules:

• Localization Batch
• Video Capture
• Steam Assets
• Google Play Assets
• AppStore Assets
• Auto Crop
• Auto Upload",
            MessageType.None);
    }

}

#endif