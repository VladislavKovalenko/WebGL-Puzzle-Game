#if UNITY_EDITOR_WIN

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class TrueFullscreenGameView
{
    // ============================================================
    // Settings
    // ============================================================

    private const string MENU_TOGGLE =
        "Tools/Megxlord Toolbox/Fullscreen/Toggle Fullscreen GameView _F11";

    private const string MENU_AUTO =
        "Tools/Megxlord Toolbox/Fullscreen/Auto Fullscreen GameView";

    private const string PREF_AUTO =
        "TrueFullscreenGameView.Auto";

    // ============================================================
    // State
    // ============================================================

    private static EditorWindow gameView;
    private static IntPtr gameViewHwnd;

    private static bool fullscreen;
    private static bool escPressed;

    // ============================================================
    // WinAPI
    // ============================================================

    private const int GWL_STYLE = -16;

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;

    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int VK_ESCAPE = 0x1B;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowLong(
        IntPtr hwnd,
        int index);

    [DllImport("user32.dll")]
    static extern uint SetWindowLong(
        IntPtr hwnd,
        int index,
        uint style);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int key);

    // ============================================================
    // Static ctor
    // ============================================================

    static TrueFullscreenGameView()
    {
        EditorApplication.update += Update;

        EditorApplication.playModeStateChanged +=
            OnPlayModeStateChanged;
    }

    // ============================================================
    // Menu
    // ============================================================

    [MenuItem(MENU_TOGGLE)]
    static void Toggle()
    {
        if (fullscreen)
            ExitFullscreen();
        else
            EnterFullscreen();
    }

    [MenuItem(MENU_AUTO)]
    static void ToggleAuto()
    {
        bool value = !EditorPrefs.GetBool(PREF_AUTO, true);

        EditorPrefs.SetBool(PREF_AUTO, value);

        Menu.SetChecked(MENU_AUTO, value);
    }

    [MenuItem(MENU_AUTO, true)]
    static bool ValidateAuto()
    {
        Menu.SetChecked(
            MENU_AUTO,
            EditorPrefs.GetBool(PREF_AUTO, true));

        return true;
    }

    // ============================================================
    // Update
    // ============================================================

    static void Update()
    {
        if (!fullscreen)
            return;

        bool down =
            (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;

        if (down && !escPressed)
        {
            ExitFullscreen();
        }

        escPressed = down;
    }

    // ============================================================
    // Play Mode
    // ============================================================

    static void OnPlayModeStateChanged(
        PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:

                if (EditorPrefs.GetBool(PREF_AUTO, true))
                    EnterFullscreen();

                break;

            case PlayModeStateChange.ExitingPlayMode:

                ExitFullscreen();

                EditorApplication.delayCall += () =>
                {
                    Resources.UnloadUnusedAssets();
                };

                break;
        }
    }

    // ============================================================
    // Enter
    // ============================================================

    static void EnterFullscreen()
    {
        if (fullscreen)
            return;

        var gameViewType =
            Type.GetType(
                "UnityEditor.GameView,UnityEditor");

        if (gameViewType == null)
        {
            Debug.LogError(
                "Cannot find UnityEditor.GameView");

            return;
        }

        gameView =
            ScriptableObject.CreateInstance(gameViewType)
            as EditorWindow;

        if (gameView == null)
        {
            Debug.LogError(
                "Cannot create GameView");

            return;
        }

        gameView.titleContent =
            new GUIContent("Game");

        try
        {
            MethodInfo showPopupMethod =
                typeof(EditorWindow).GetMethod(
                    "ShowPopup",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (showPopupMethod != null)
            {
                showPopupMethod.Invoke(gameView, null);
            }
            else
            {
                MethodInfo showWithModeMethod =
                    typeof(EditorWindow).GetMethod(
                        "ShowWithMode",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                if (showWithModeMethod != null)
                {
                    showWithModeMethod.Invoke(
                        gameView,
                        new object[] { 1, false });
                }
                else
                {
                    gameView.Show();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            gameView.Show();
        }

        gameView.Focus();

        fullscreen = true;

        EditorApplication.delayCall += ApplyFullscreen;
    }

    // ============================================================
    // Exit
    // ============================================================

    static void ExitFullscreen()
    {
        if (!fullscreen)
            return;

        fullscreen = false;

        if (gameView != null)
        {
            gameView.Close();
            DestroyableDestroyImmediate(gameView);
            gameView = null;
        }

        gameViewHwnd = IntPtr.Zero;
    }

    // ============================================================
    // Apply
    // ============================================================

    static void ApplyFullscreen()
    {
        if (gameView == null)
            return;

        try
        {
            gameView.maximized = true;

            gameViewHwnd = GetGameViewHwnd(gameView);

            if (gameViewHwnd == IntPtr.Zero)
            {
                gameViewHwnd = GetForegroundWindow();
            }

            if (gameViewHwnd != IntPtr.Zero)
            {
                SetWindowLong(
                    gameViewHwnd,
                    GWL_STYLE,
                    WS_POPUP | WS_VISIBLE);

                Resolution r =
                    Screen.currentResolution;

                SetWindowPos(
                    gameViewHwnd,
                    IntPtr.Zero,
                    0,
                    0,
                    r.width,
                    r.height,
                    SWP_FRAMECHANGED |
                    SWP_SHOWWINDOW);
            }

            HideToolbar();

            RemovePadding();

            ShiftIMGUIContainer();

            gameView.Repaint();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ============================================================
    // Get GameView Hwnd
    // ============================================================

    static IntPtr GetGameViewHwnd(EditorWindow window)
    {
        try
        {
            PropertyInfo prop =
                typeof(EditorWindow).GetProperty(
                    "nativeHandle",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (prop != null)
            {
                object value = prop.GetValue(window);

                if (value is IntPtr ptr)
                {
                    return ptr;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return IntPtr.Zero;
    }

    // ============================================================
    // Destroy
    // ============================================================

    static void DestroyableDestroyImmediate(UnityEngine.Object obj)
    {
        try
        {
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ============================================================
    // Hide Toolbar
    // ============================================================

    static void HideToolbar()
    {
        if (gameView == null)
            return;

        try
        {
            Type type = gameView.GetType();

            PropertyInfo prop =
                type.GetProperty(
                    "showToolbar",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(gameView, false);
            }
            else
            {
                FieldInfo field =
                    type.GetField(
                        "m_showToolbar",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                if (field != null)
                {
                    field.SetValue(gameView, false);
                }
            }

            gameView.Repaint();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ============================================================
    // Remove Padding
    // ============================================================

    static void RemovePadding()
    {
        if (gameView == null)
            return;

        try
        {
            Type gameViewType = gameView.GetType();

            PropertyInfo paddingProp =
                gameViewType.GetProperty(
                    "viewPadding",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (paddingProp != null &&
                paddingProp.CanWrite)
            {
                try
                {
                    object current =
                        paddingProp.GetValue(gameView);

                    if (current is RectOffset)
                    {
                        paddingProp.SetValue(
                            gameView,
                            new RectOffset(0, 0, 0, 0));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            FieldInfo parentField =
                typeof(EditorWindow).GetField(
                    "m_Parent",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (parentField == null)
                return;

            object dockArea =
                parentField.GetValue(gameView);

            if (dockArea == null)
                return;

            Type dockType =
                dockArea.GetType();

            FieldInfo borderField =
                dockType.GetField(
                    "m_BorderSize",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (borderField != null)
            {
                borderField.SetValue(
                    dockArea,
                    new RectOffset(0, 0, 0, 0));
            }

            PropertyInfo actualViewProp =
                dockType.GetProperty(
                    "actualView",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (actualViewProp != null)
            {
                actualViewProp.SetValue(dockArea, gameView);
            }

            MethodInfo updateViewRectMethod =
                dockType.GetMethod(
                    "UpdateViewRect",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (updateViewRectMethod != null)
            {
                updateViewRectMethod.Invoke(dockArea, null);
            }

            gameView.Repaint();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ============================================================
    // Shift IMGUIContainer
    // ============================================================

    static void ShiftIMGUIContainer()
    {
        if (gameView == null)
            return;

        try
        {
            var root = gameView.rootVisualElement;

            if (root == null)
                return;

            var imgui = root.Q<IMGUIContainer>("Dockarea8");

            if (imgui == null)
            {
                imgui = root.Q<IMGUIContainer>();
            }

            if (imgui != null)
            {
                imgui.style.translate = new Translate(0, -26);
            }

            root.style.position = Position.Absolute;
            root.style.top = -26;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}

#endif