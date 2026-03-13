#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

//убрал предупреждение, что статические 
#pragma warning disable UDR0001 
public static class SceneSwitcherToolbar
{
    // ===============================
    // Constants & Paths
    // ===============================
    
    private const string kModeElementPath = "Scene Switcher/Mode";
    private const string kSceneElementPath = "Scene Switcher/Scene";

    // ===============================
    // Scene Mode
    // ===============================

    public enum SceneMode
    {
        All = 0,
        Work = 1,
        Build = 2
    }

    private static readonly string[] modeLabels =
    {
        "All Scenes",
        "Work Scenes",
        "Scenes in Build"
    };

    private static SceneMode CurrentMode
    {
        get => (SceneMode)EditorPrefs.GetInt("SceneSwitcher_Mode", (int)SceneMode.Build);
        set => EditorPrefs.SetInt("SceneSwitcher_Mode", (int)value);
    }

    // ===============================
    // Scene List Data
    // ===============================

    private struct SceneInfo
    {
        public string name;
        public string path;
    }

    private static List<SceneInfo> sceneInfos = new List<SceneInfo>();
    private static int selectedIndex = 0;

    // ===============================
    // Static Constructor - Event Subscriptions
    // ===============================

    static SceneSwitcherToolbar()
    {
        RefreshSceneList();
        
        EditorApplication.projectChanged += OnProjectChanged;
        EditorBuildSettings.sceneListChanged += OnProjectChanged;
        SceneManager.activeSceneChanged += OnSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
    }

    private static void OnProjectChanged()
    {
        RefreshSceneList();
        MainToolbar.Refresh(kSceneElementPath);
    }

    private static void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateSelectedIndex();
        MainToolbar.Refresh(kSceneElementPath);
    }

    // ===============================
    // Mode Dropdown
    // ===============================

    [MainToolbarElement(kModeElementPath, defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement CreateModeDropdown()
    {
        var content = new MainToolbarContent(modeLabels[(int)CurrentMode], tooltip: "Scene filter mode");
        return new MainToolbarDropdown(content, ShowModeMenu);
    }

    private static void ShowModeMenu(Rect dropDownRect)
    {
        var menu = new GenericMenu();

        for (int i = 0; i < modeLabels.Length; i++)
        {
            int index = i;
            bool isSelected = index == (int)CurrentMode;

            menu.AddItem(new GUIContent(modeLabels[index]), isSelected, () =>
            {
                if (CurrentMode != (SceneMode)index)
                {
                    CurrentMode = (SceneMode)index;
                    RefreshSceneList();
                    MainToolbar.Refresh(kModeElementPath);
                    MainToolbar.Refresh(kSceneElementPath);
                }
            });
        }

        menu.DropDown(dropDownRect);
    }

    // ===============================
    // Scene Dropdown
    // ===============================

    [MainToolbarElement(kSceneElementPath, defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement CreateSceneDropdown()
    {
        string displayName = GetCurrentSceneDisplayName();
        var content = new MainToolbarContent(displayName, tooltip: "Select scene to open");
        return new MainToolbarDropdown(content, ShowSceneMenu);
    }

    private static string GetCurrentSceneDisplayName()
    {
        if (sceneInfos.Count == 0)
            return "No scenes";

        string activeScenePath = Application.isPlaying
            ? SceneManager.GetActiveScene().path
            : EditorSceneManager.GetActiveScene().path;

        if (string.IsNullOrEmpty(activeScenePath))
            return "Untitled";

        string activeSceneName = Path.GetFileNameWithoutExtension(activeScenePath);

        // Check if current scene is in the list
        int index = sceneInfos.FindIndex(s => s.path == activeScenePath);
        
        if (index >= 0)
        {
            return sceneInfos[index].name;
        }
        else if (CurrentMode == SceneMode.Build)
        {
            return activeSceneName + " (not in build)";
        }
        
        return activeSceneName;
    }

    private static void ShowSceneMenu(Rect dropDownRect)
    {
        var menu = new GenericMenu();

        if (sceneInfos.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No scenes found"));
            menu.DropDown(dropDownRect);
            return;
        }

        string activeScenePath = Application.isPlaying
            ? SceneManager.GetActiveScene().path
            : EditorSceneManager.GetActiveScene().path;

        for (int i = 0; i < sceneInfos.Count; i++)
        {
            int index = i;
            var sceneInfo = sceneInfos[i];
            bool isSelected = sceneInfo.path == activeScenePath;

            menu.AddItem(new GUIContent(sceneInfo.name), isSelected, () =>
            {
                LoadScene(sceneInfo);
            });
        }

        menu.DropDown(dropDownRect);
    }

    // ===============================
    // Scene Loading
    // ===============================

    private static void LoadScene(SceneInfo sceneInfo)
    {
        string path = sceneInfo.path;
        string name = sceneInfo.name;

        // Check for package scenes
        if (path.StartsWith("Packages/"))
        {
            Debug.LogWarning($"<color=orange>Scene Switcher:</color> Scene \"{name}\" is inside a read-only package.");
            return;
        }

        // Check if file exists
        if (!File.Exists(path))
        {
            Debug.LogWarning($"<color=orange>Scene Switcher:</color> Scene \"{name}\" could not be found at path: {path}");
            return;
        }

        if (Application.isPlaying)
        {
            // Runtime scene loading
            if (Application.CanStreamedLevelBeLoaded(name))
            {
                SceneManager.LoadScene(name);
            }
            else
            {
                Debug.LogWarning($"<color=orange>Scene Switcher:</color> Scene \"{name}\" is not in Build Settings and cannot be loaded at runtime.");
            }
        }
        else
        {
            // Editor scene loading
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }

    // ===============================
    // Scene List Management
    // ===============================

    private static void RefreshSceneList()
    {
        sceneInfos = GetSceneInfos(CurrentMode);
        UpdateSelectedIndex();
    }

    private static void UpdateSelectedIndex()
    {
        string currentPath = Application.isPlaying
            ? SceneManager.GetActiveScene().path
            : EditorSceneManager.GetActiveScene().path;

        selectedIndex = sceneInfos.FindIndex(s => s.path == currentPath);
        if (selectedIndex < 0) selectedIndex = 0;
    }

    private static List<SceneInfo> GetSceneInfos(SceneMode mode)
    {
        var result = new List<SceneInfo>();
        var missing = new List<string>();

        if (mode == SceneMode.All || mode == SceneMode.Work)
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".unity")) 
                    continue;

                bool isProjectScene = path.StartsWith("Assets/");
                
                if (mode == SceneMode.Work && !isProjectScene)
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                result.Add(new SceneInfo { name = name, path = path });
            }
        }
        else // Build Mode
        {
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled);

            foreach (var scene in buildScenes)
            {
                if (!File.Exists(scene.path))
                {
                    missing.Add(Path.GetFileNameWithoutExtension(scene.path));
                    continue;
                }

                if (!scene.path.StartsWith("Assets/"))
                {
                    Debug.LogWarning($"<color=orange>Scene Switcher:</color> Build scene \"{Path.GetFileNameWithoutExtension(scene.path)}\" is inside a read-only package and skipped.");
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(scene.path);
                result.Add(new SceneInfo { name = name, path = scene.path });
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    $"<color=orange>Scene Switcher:</color> Ignored {missing.Count} missing scene(s) in Build Settings:\n" +
                    string.Join(", ", missing)
                );
            }
        }

        return result.OrderBy(s => s.name).ToList();
    }
}
#endif

#pragma warning restore UDR0001