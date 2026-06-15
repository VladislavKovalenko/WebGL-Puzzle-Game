using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Megxlord.UI.Editor
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENTRY POINT
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaLayoutEditor : EditorWindow
{
    private FigmaState          _state;
    private FigmaSync           _sync;
    private FigmaVisualEditor   _visual;
    private FigmaConstraintsUI  _constraintsUI;
    private FigmaAlignUI        _alignUI;
    private FigmaSettingsUI     _settingsUI;
    private FigmaStyleKit       _styles;
    private FigmaInputHandler   _input;

    // Фиксированная высота нижней панели (constraints + buttons + statusbar)
    private const float BOTTOM_PANEL_HEIGHT = 210f;

    [MenuItem("Tools/Megxlord uGUI/Figma Layout Editor", priority = 100)]
    public static void ShowWindow() => GetWindow<FigmaLayoutEditor>("✦ Figma Layout");

    private void OnEnable()
    {
        wantsMouseMove = true;
        minSize        = new Vector2(380, 520);

        _state         = new FigmaState();
        _styles        = new FigmaStyleKit();
        _sync          = new FigmaSync(_state);
        _visual        = new FigmaVisualEditor(_state, _styles);
        _constraintsUI = new FigmaConstraintsUI(_state, _styles, _sync);
        _alignUI       = new FigmaAlignUI(_state, _styles, _sync);
        _settingsUI    = new FigmaSettingsUI(_state, _styles);
        _input         = new FigmaInputHandler(_state, _sync, _visual);

        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update   += OnEditorUpdate;
        SceneView.duringSceneGui   += OnSceneGUI;
        _sync.SyncFromSelection();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update   -= OnEditorUpdate;
        SceneView.duringSceneGui   -= OnSceneGUI;
    }

    private void OnSelectionChanged() { _sync.SyncFromSelection(); Repaint(); }

    private void OnEditorUpdate()
    {
        if (_state.NeedsRepaint) { _state.NeedsRepaint = false; Repaint(); }
        _sync.CheckSceneOverlayExpiry();
    }

    private void OnSceneGUI(SceneView sv) => _sync.DrawSceneOverlay(sv);

    private void OnGUI()
    {
        _styles.Init();
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), FigmaStyleKit.ColBg);

        // ── Шапка (фиксированная высота) ──────────────────────────────────
        EditorGUILayout.Space(6);
        DrawHeader();

        // ── Переключатель "От родителя / От Canvas" ───────────────────────
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            bool newVal = GUILayout.Toggle(_state.RelativeToCanvas,
                "Измерять от Canvas", "Button", GUILayout.Height(20));
            if (newVal != _state.RelativeToCanvas)
            {
                _state.RelativeToCanvas = newVal;
                _sync.SyncFromSelection();
                Repaint();
            }
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.Space(2);

        DrawTabBar();
        EditorGUILayout.Space(4);

        if (_state.Target == null && _state.MultiTargets.Count == 0)
        {
            DrawNoSelection();
            return;
        }

        DrawTargetBanner();
        EditorGUILayout.Space(6);

        // ── Не-Layout вкладки — обычная прокрутка ─────────────────────────
        if (_state.CurrentTab != FigmaTab.Layout)
        {
            switch (_state.CurrentTab)
            {
                case FigmaTab.Align:       _alignUI.Draw(position.width);                  break;
                case FigmaTab.Constraints: _constraintsUI.DrawConstraintsTab(position.width); break;
                case FigmaTab.Settings:    _settingsUI.Draw();                              break;
            }
            return;
        }

        // ── Layout вкладка ────────────────────────────────────────────────
        if (_state.MultiTargets.Count > 1)
        {
            EditorGUILayout.HelpBox("Несколько объектов — используйте вкладку Align", MessageType.Info);
            return;
        }

        // Считаем сколько места занимают шапка + баннер + отступы
        // Всё что выше visual editor уже нарисовано через GUILayout,
        // поэтому берём текущий Y курсора как начало visual editor
        float usedTop    = GUILayoutUtility.GetLastRect().yMax + 2;
        float totalH     = position.height;
        float bottomH    = BOTTOM_PANEL_HEIGHT;

        // Высота visual editor = всё оставшееся место минус нижняя панель
        float visualH    = Mathf.Max(120, totalH - usedTop - bottomH - 10);
        _state.VisualHeight = visualH;

        // Рисуем visual editor с вычисленной высотой
        _visual.Draw(position.width, ref _state.ShowVisualEditor);

        // ── Нижняя панель — рисуем через абсолютные координаты ────────────
        float bottomY = totalH - bottomH;

        // Фоновая полоска-разделитель
        EditorGUI.DrawRect(new Rect(0, bottomY - 1, position.width, 1), FigmaStyleKit.ColBorder);

        // Рисуем нижний блок в его зарезервированной зоне
        GUILayout.BeginArea(new Rect(0, bottomY, position.width, bottomH));
        DrawBottomPanel();
        GUILayout.EndArea();

        // Input handling
        _input.HandleMouseEvents(_visual.VisualRect, _visual.ContentRect, _visual.ObjectRect);
        _input.HandleKeyboard();
    }

    // ── Нижняя панель (constraints + кнопки + статусбар) ──────────────────
    private void DrawBottomPanel()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, BOTTOM_PANEL_HEIGHT), FigmaStyleKit.ColBg);

        EditorGUILayout.Space(4);
        _constraintsUI.DrawFieldsPanel(position.width);
        EditorGUILayout.Space(4);
        DrawActionButtons();
        DrawStatusBar();
    }

    // ── Header ────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("✦ Figma Layout Pro", _styles.Title);
            GUILayout.FlexibleSpace();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Visual constraint-based UI positioning", _styles.Mini);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.Space(4);
    }

    // ── Tab bar ───────────────────────────────────────────────────────────
    private void DrawTabBar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTab("⊞ Layout",      FigmaTab.Layout);
            DrawTab("⊡ Align",       FigmaTab.Align);
            DrawTab("⬡ Constraints", FigmaTab.Constraints);
            DrawTab("⚙ Settings",    FigmaTab.Settings);
        }
    }

    private void DrawTab(string label, FigmaTab tab)
    {
        bool active = _state.CurrentTab == tab;
        if (GUILayout.Button(label, active ? _styles.TabActive : _styles.Tab, GUILayout.Height(26)))
        {
            _state.CurrentTab = tab;
            Repaint();
        }
    }

    // ── No selection ──────────────────────────────────────────────────────
    private void DrawNoSelection()
    {
        EditorGUILayout.Space(20);
        var r = GUILayoutUtility.GetRect(position.width - 30, 90);
        EditorGUI.DrawRect(r, FigmaStyleKit.ColPanel);
        FigmaDrawUtils.DrawBorder(r, FigmaStyleKit.ColBorder);
        GUI.Label(r, "⬚", new GUIStyle(GUI.skin.label)
            { fontSize = 56, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.3f,0.3f,0.35f) } });

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Выберите UI объект (RectTransform внутри другого RectTransform)",
            new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true, fontSize = 10 });
    }

    // ── Target banner ─────────────────────────────────────────────────────
    private void DrawTargetBanner()
    {
        var r = GUILayoutUtility.GetRect(position.width - 20, 44);
        r.x += 10; r.width -= 10;
        EditorGUI.DrawRect(r, FigmaStyleKit.ColPanel);
        FigmaDrawUtils.DrawBorder(r, FigmaStyleKit.ColBorder);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), FigmaStyleKit.ColAccent);

        string name = _state.MultiTargets.Count > 1
            ? $"{_state.MultiTargets.Count} objects"
            : _state.Target?.name ?? "";

        GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 100, 18), name,
            new GUIStyle(GUI.skin.label)
                { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = FigmaStyleKit.ColText } });

        if (_state.Parent != null)
            GUI.Label(new Rect(r.x + 10, r.y + 23, r.width - 100, 13),
                $"in  {_state.Parent.name}  ({_state.Parent.rect.width:F0} × {_state.Parent.rect.height:F0})",
                new GUIStyle(GUI.skin.label)
                    { fontSize = 9, normal = { textColor = FigmaStyleKit.ColTextDim } });

        if (_state.Target != null)
        {
            var badge = new Rect(r.xMax - 84, r.y + 10, 76, 22);
            EditorGUI.DrawRect(badge,
                new Color(FigmaStyleKit.ColAccent.r, FigmaStyleKit.ColAccent.g, FigmaStyleKit.ColAccent.b, 0.15f));
            FigmaDrawUtils.DrawBorder(badge, FigmaStyleKit.ColAccent);
            GUI.Label(badge, $"{_state.W:F0} × {_state.H:F0}",
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = FigmaStyleKit.ColAccent }
                });
        }
    }

    // ── Action buttons ────────────────────────────────────────────────────
    private void DrawActionButtons()
    {
        // Hint
        var hintR = GUILayoutUtility.GetRect(position.width - 20, 22);
        hintR.x += 10; hintR.width -= 10;
        EditorGUI.DrawRect(hintR, new Color(0.1f, 0.16f, 0.1f, 1f));
        FigmaDrawUtils.DrawBorder(hintR, new Color(0.2f, 0.5f, 0.2f, 0.5f));
        GUI.Label(new Rect(hintR.x + 8, hintR.y + 4, hintR.width - 16, 14),
            "Измени значения выше  →  нажми APPLY чтобы применить к объекту на сцене",
            new GUIStyle(GUI.skin.label)
                { fontSize = 8, normal = { textColor = new Color(0.45f, 0.85f, 0.45f, 1f) } });

        EditorGUILayout.Space(3);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Center", _styles.Btn, GUILayout.Height(24)))
                _sync.ResetToCenter();
            GUILayout.Space(4);
            if (GUILayout.Button("↻ Sync", _styles.Btn, GUILayout.Height(24)))
            {
                _sync.SyncFromSelection(); Repaint();
            }
            GUILayout.Space(4);
            if (GUILayout.Button("⎌ Undo", _styles.Btn, GUILayout.Height(24)))
            {
                _state.SwapWithPrev(); _sync.Apply(true);
            }
            GUILayout.Space(10);
        }

        EditorGUILayout.Space(3);

        // Apply — большой зелёный
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            var applyStyle = new GUIStyle(_styles.Btn)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor  = Color.white,
                              background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.15f, 0.55f, 0.22f, 1f)) },
                hover     = { textColor  = Color.white,
                              background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.2f, 0.72f, 0.3f, 1f)) },
                active    = { textColor  = Color.white,
                              background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.1f, 0.4f, 0.15f, 1f)) },
            };
            if (GUILayout.Button("✓  APPLY TO SCENE", applyStyle, GUILayout.Height(34)))
            {
                _sync.Apply(true);
                ShowNotification(new GUIContent("✓ Applied!"));
            }
            GUILayout.Space(10);
        }
    }

    // ── Status bar ────────────────────────────────────────────────────────
    private void DrawStatusBar()
    {
        EditorGUILayout.Space(3);
        var r = GUILayoutUtility.GetRect(position.width, 16);
        EditorGUI.DrawRect(r, FigmaStyleKit.ColPanel);
        string info = _state.Target != null
            ? $"L:{_state.L:F1}  R:{_state.R:F1}  T:{_state.T:F1}  B:{_state.B:F1}   {_state.W:F0} × {_state.H:F0}"
            : "Нет выбора";
        GUI.Label(new Rect(r.x + 8, r.y + 1, r.width - 16, 14), info,
            new GUIStyle(GUI.skin.label)
                { fontSize = 9, normal = { textColor = FigmaStyleKit.ColTextDim } });
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  STATE  (данные, которые разделяют все классы)
    // ═══════════════════════════════════════════════════════════════════════
    public enum FigmaTab { Layout, Align, Constraints, Settings }

    public class FigmaState
    {
        // Targets
        public RectTransform        Target;
        public RectTransform        Parent;
        public List<RectTransform>  MultiTargets = new();

        // Figma values (current)
        public float L, R, T, B, W, H;

        // Previous (for undo)
        public float PL, PR, PT, PB, PW, PH;

        // UI state
        public FigmaTab CurrentTab     = FigmaTab.Layout;
        public bool     ShowVisualEditor = true;
        public bool     NeedsRepaint;

        // Visual editor settings
        public bool   ShowGrid       = true;
        public bool   SnapToGrid     = false;
        public float  GridSize       = 8f;
        public bool   SnapToPixels   = true;
        public bool   ShowRulers     = true;
        public bool   ShowSpacing    = true;
        public bool   ShowDimensions = true;
        public bool   ShowCrosshair  = true;
        public bool   ShowSafeZone   = false;
        public bool   RelativeToCanvas = false;
        public bool   MaintainAspect = false;
        public float  AspectRatio    = 1f;
        public bool   LivePreview    = true;
        public float  VisualHeight   = 280f;
        public float  Zoom           = 1f;
        public Vector2 PanOffset     = Vector2.zero;

        // Colors
        public Color ContainerColor    = new(0.11f, 0.11f, 0.13f, 1f);
        public Color ObjectColor       = new(0.25f, 0.55f, 0.95f, 0.25f);
        public Color ObjectBorderColor = new(0.35f, 0.65f, 1f,    1f);
        public Color HandleColor       = new(0.92f, 0.92f, 0.95f, 1f);
        public Color HandleHover       = new(1f,    0.8f,  0.15f, 1f);
        public Color HandleActive      = new(0.2f,  0.92f, 0.45f, 1f);

        // Scene overlay
        public bool   SceneOverlayVisible;
        public string SceneOverlayText = "";
        public double SceneOverlayHide;

        public void StorePrev()
        {
            PL = L; PR = R; PT = T; PB = B; PW = W; PH = H;
        }

        public void SwapWithPrev()
        {
            (L, PL) = (PL, L); (R, PR) = (PR, R);
            (T, PT) = (PT, T); (B, PB) = (PB, B);
            (W, PW) = (PW, W); (H, PH) = (PH, H);
        }

        public void Clamp()
        {
            W = Mathf.Max(1, W); H = Mathf.Max(1, H);
            L = Mathf.Max(0, L); R = Mathf.Max(0, R);
            T = Mathf.Max(0, T); B = Mathf.Max(0, B);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SYNC  (чтение/запись RectTransform <-> FigmaState)
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaSync
{
    private readonly FigmaState _s;

    public FigmaSync(FigmaState state) => _s = state;

    private RectTransform ResolveParent(RectTransform rt)
    {
        if (_s.RelativeToCanvas)
        {
            var canvasRt = GetCanvasRect(rt);
            if (canvasRt != null) return canvasRt;
        }
        return rt.parent as RectTransform;
    }

    private static RectTransform GetCanvasRect(RectTransform rt)
    {
        var canvas = rt.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    public void SyncFromSelection()
    {
        var targets = GetValidTargets();
        _s.MultiTargets = targets;

        if (targets.Count >= 1)
        {
            _s.Target = targets[0];
            _s.Parent = ResolveParent(_s.Target);
            Calculate();
            _s.StorePrev();
        }
        else
        {
            _s.Target = null;
            _s.Parent = null;
        }
    }

    public void Calculate()
    {
        var rt     = _s.Target;
        var parent = rt != null ? ResolveParent(rt) : _s.Parent;
        if (rt == null || parent == null) return;

        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);

        Vector2 localMin = parent.InverseTransformPoint(worldCorners[0]);
        Vector2 localMax = parent.InverseTransformPoint(worldCorners[2]);

        var ps = parent.rect.size;
        _s.W = localMax.x - localMin.x;
        _s.H = localMax.y - localMin.y;

        float leftEdge = -parent.pivot.x * ps.x;
        float topEdge  = (1f - parent.pivot.y) * ps.y;

        _s.L = localMin.x - leftEdge;
        _s.R = ps.x - _s.W - _s.L;
        _s.T = topEdge - localMax.y;
        _s.B = ps.y - _s.H - _s.T;
    }

    public void Apply(bool recordUndo = false)
    {
        var rt     = _s.Target;
        var parent = ResolveParent(rt);
        if (rt == null || parent == null) return;

        if (recordUndo) Undo.RecordObject(rt, "Figma Layout");

        bool isStretchedH = rt.anchorMin.x != rt.anchorMax.x;
        bool isStretchedV = rt.anchorMin.y != rt.anchorMax.y;

        if (isStretchedH || isStretchedV)
        {
            Vector3[] stretchedCorners = new Vector3[4];
            rt.GetWorldCorners(stretchedCorners);

            Vector2 stretchedLocalMin = parent.InverseTransformPoint(stretchedCorners[0]);
            Vector2 stretchedLocalMax = parent.InverseTransformPoint(stretchedCorners[2]);

            float w = stretchedLocalMax.x - stretchedLocalMin.x;
            float h = stretchedLocalMax.y - stretchedLocalMin.y;
            float cx = (stretchedLocalMin.x + stretchedLocalMax.x) * 0.5f;
            float cy = (stretchedLocalMin.y + stretchedLocalMax.y) * 0.5f;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            var directParent = rt.parent as RectTransform;
            Vector3 stretchedWorldCenter = parent.TransformPoint(new Vector3(cx, cy, 0));
            Vector2 stretchedDpLocal = directParent.InverseTransformPoint(stretchedWorldCenter);

            float stretchedDpAnchorX = (0.5f - directParent.pivot.x) * directParent.rect.size.x;
            float stretchedDpAnchorY = (0.5f - directParent.pivot.y) * directParent.rect.size.y;

            rt.anchoredPosition = new Vector2(
                stretchedDpLocal.x - stretchedDpAnchorX,
                stretchedDpLocal.y - stretchedDpAnchorY);
            rt.sizeDelta = new Vector2(w, h);

            Calculate();
            _s.StorePrev();
            EditorUtility.SetDirty(rt);
            ShowOverlay($"Anchors → Center! L:{_s.L:F0}  R:{_s.R:F0}  T:{_s.T:F0}  B:{_s.B:F0}   {_s.W:F0}×{_s.H:F0}");
            SceneView.RepaintAll();
            return;
        }

        _s.Clamp();
        var ps = parent.rect.size;
        float leftEdge = -parent.pivot.x * ps.x;
        float topEdge  = (1f - parent.pivot.y) * ps.y;

        float minX = leftEdge + _s.L;
        float maxX = minX + _s.W;
        float maxY = topEdge - _s.T;
        float minY = maxY - _s.H;

        Vector2 parentLocalCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector3 worldCenter = parent.TransformPoint(parentLocalCenter);

        var dp = rt.parent as RectTransform;
        Vector2 dpLocalCenter = dp.InverseTransformPoint(worldCenter);

        float dpAnchorX = (rt.anchorMin.x - dp.pivot.x) * dp.rect.size.x;
        float dpAnchorY = (rt.anchorMin.y - dp.pivot.y) * dp.rect.size.y;

        rt.anchoredPosition = new Vector2(dpLocalCenter.x - dpAnchorX, dpLocalCenter.y - dpAnchorY);
        rt.sizeDelta        = new Vector2(_s.W, _s.H);

        EditorUtility.SetDirty(rt);
        ShowOverlay($"L:{_s.L:F0}  R:{_s.R:F0}  T:{_s.T:F0}  B:{_s.B:F0}   {_s.W:F0}×{_s.H:F0}");
        SceneView.RepaintAll();
    }

    public void ResetToCenter()
    {
        var parent = _s.Target != null ? ResolveParent(_s.Target) : _s.Parent;
        if (parent == null) return;
        _s.StorePrev();

        var ps = parent.rect.size;

        _s.L = (ps.x - _s.W) * 0.5f;
        _s.R = (ps.x - _s.W) * 0.5f;
        _s.T = (ps.y - _s.H) * 0.5f;
        _s.B = (ps.y - _s.H) * 0.5f;

        Apply(true);
    }

    private void ShowOverlay(string text)
    {
        _s.SceneOverlayText    = text;
        _s.SceneOverlayVisible = true;
        _s.SceneOverlayHide    = EditorApplication.timeSinceStartup + 2.0;
        SceneView.RepaintAll();
    }

    public void CheckSceneOverlayExpiry()
    {
        if (_s.SceneOverlayVisible && EditorApplication.timeSinceStartup > _s.SceneOverlayHide)
        {
            _s.SceneOverlayVisible = false;
            SceneView.RepaintAll();
        }
    }

    public void DrawSceneOverlay(SceneView sv)
    {
        if (!_s.SceneOverlayVisible) return;
        Handles.BeginGUI();
        float w = 340, h = 30;
        var r = new Rect((sv.position.width - w) * 0.5f, sv.position.height - h - 44, w, h);
        EditorGUI.DrawRect(r, new Color(0.08f, 0.08f, 0.1f, 0.93f));
        FigmaDrawUtils.DrawBorder(r, FigmaStyleKit.ColAccent);
        GUI.Label(new Rect(r.x + 10, r.y + 7, w - 20, 16), _s.SceneOverlayText,
            new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = FigmaStyleKit.ColAccent }
            });
        Handles.EndGUI();
    }

    private static List<RectTransform> GetValidTargets()
    {
        var res = new List<RectTransform>();
        foreach (var go in Selection.gameObjects)
            if (go.TryGetComponent<RectTransform>(out var rt) && rt.parent is RectTransform)
                res.Add(rt);
        return res;
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  VISUAL EDITOR  (рисование превью-канваса)
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaVisualEditor
{
    private readonly FigmaState    _s;
    private readonly FigmaStyleKit _sk;

    public Rect VisualRect  { get; private set; }
    public Rect ContentRect { get; private set; }
    public Rect ObjectRect  { get; private set; }

    private bool    _isResizingPanel;
    private float   _resizePanelStartY, _resizePanelStartH;
    private Vector2 _mousePos;

    private const float RULER = 22f;

    public FigmaVisualEditor(FigmaState s, FigmaStyleKit sk) { _s = s; _sk = sk; }

    public void Draw(float windowWidth, ref bool show)
    {
        FigmaDrawUtils.SectionHeader("Visual Editor", windowWidth);

        if (!show) { DrawResizeHandle(windowWidth); return; }

        float aw   = windowWidth - 20;
        var outer  = GUILayoutUtility.GetRect(aw, _s.VisualHeight, GUILayout.ExpandWidth(false));
        outer.x   += 10;
        VisualRect = outer;

        EditorGUI.DrawRect(outer, _s.ContainerColor);
        FigmaDrawUtils.DrawBorder(outer, FigmaStyleKit.ColBorder);

        if (_s.Parent == null) { DrawResizeHandle(windowWidth); return; }

        var ps     = _s.Parent.rect.size;
        float margin = _s.ShowRulers ? RULER : 4f;
        float maxW = (outer.width  - margin - 12) * _s.Zoom;
        float maxH = (outer.height - margin - 12) * _s.Zoom;
        float aspect = ps.x / Mathf.Max(1, ps.y);
        float cw   = maxW, ch = cw / aspect;
        if (ch > maxH) { ch = maxH; cw = ch * aspect; }

        float originX = outer.x + margin + (outer.width  - margin - 12 - cw) * 0.5f + _s.PanOffset.x;
        float originY = outer.y + (outer.height - ch) * 0.5f + _s.PanOffset.y;
        originX = Mathf.Clamp(originX, outer.x - cw + 40, outer.xMax - 40);
        originY = Mathf.Clamp(originY, outer.y - ch + 40, outer.yMax - 40);

        ContentRect = new Rect(originX, originY, cw, ch);
        float sx    = cw / ps.x;
        float sy    = ch / ps.y;

        _mousePos = Event.current.mousePosition;

        if (_s.ShowGrid) DrawGrid(sx, sy, ps);

        Handles.BeginGUI();

        // Canvas background (when measuring from Canvas and Canvas != parent)
        if (_s.RelativeToCanvas)
        {
            var canvasRt = GetCanvasRect(_s.Target);
            if (canvasRt != null && canvasRt != _s.Parent)
            {
                EditorGUI.DrawRect(ContentRect, new Color(0.1f, 0.1f, 0.3f, 0.5f));
                GUI.Label(ContentRect, "Canvas", EditorStyles.centeredGreyMiniLabel);
            }
        }

            EditorGUI.DrawRect(ContentRect, new Color(0.17f, 0.17f, 0.21f, 1f));
            Handles.color = new Color(0.5f, 0.5f, 0.55f, 0.9f);
            FigmaDrawUtils.HandlesRect(ContentRect);

            if (_s.RelativeToCanvas && _s.Target != null)
            {
                var directParent = _s.Target.parent as RectTransform;
                if (directParent != null && directParent != _s.Parent)
                {
                    Vector3[] parentCorners = new Vector3[4];
                    directParent.GetWorldCorners(parentCorners);

                    Vector2 localMin = _s.Parent.InverseTransformPoint(parentCorners[0]);
                    Vector2 localMax = _s.Parent.InverseTransformPoint(parentCorners[2]);

                    var canvasPs = _s.Parent.rect.size;

                    float canvasLeftEdge = -_s.Parent.pivot.x * canvasPs.x;
                    float canvasTopEdge = (1f - _s.Parent.pivot.y) * canvasPs.y;

                    float pL = localMin.x - canvasLeftEdge;
                    float pT = canvasTopEdge - localMax.y;
                    float pW = localMax.x - localMin.x;
                    float pH = localMax.y - localMin.y;

                    float pOx = ContentRect.x + pL * sx;
                    float pOy = ContentRect.y + pT * sy;
                    float pOw = Mathf.Max(2, pW * sx);
                    float pOh = Mathf.Max(2, pH * sy);
                    Rect parentRect = new Rect(pOx, pOy, pOw, pOh);

                    FigmaDrawUtils.DrawDashedRect(parentRect, new Color(1f, 0.6f, 0.2f, 0.6f));
                }
            }

            if (_s.ShowSafeZone) DrawSafeZone(sx, sy);

            if (_s.ShowCrosshair && ContentRect.Contains(_mousePos))
            {
                Handles.color = new Color(0.28f, 0.56f, 1f, 0.25f);
                Handles.DrawLine(new Vector3(_mousePos.x, ContentRect.y,    0), new Vector3(_mousePos.x, ContentRect.yMax, 0));
                Handles.DrawLine(new Vector3(ContentRect.x, _mousePos.y,   0), new Vector3(ContentRect.xMax, _mousePos.y,  0));
            }

            // T — отступ от верхнего края (экранный Y идёт вниз, поэтому +T*sy)
            float ox = ContentRect.x + _s.L * sx;
            float oy = ContentRect.y + _s.T * sy;
            ObjectRect = new Rect(ox, oy, Mathf.Max(2, _s.W * sx), Mathf.Max(2, _s.H * sy));

        DrawShadow(ObjectRect);

        if (_s.ShowSpacing) DrawSpacing(sx, sy);

        EditorGUI.DrawRect(ObjectRect, _s.ObjectColor);
        Handles.color = _s.ObjectBorderColor;
        FigmaDrawUtils.HandlesRect(ObjectRect);
        DrawCornerMarkers(ObjectRect);

        if (_s.ShowDimensions) DrawDimensions();

        DrawPivot();
        DrawHandles();

        Handles.EndGUI();

        if (_s.ShowRulers) DrawRulers(ps, sx, sy);

        EditorGUILayout.Space(3);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                $"Zoom {_s.Zoom:F2}×  •  Scroll=zoom  ПКМ=pan  ЛКМ=move/resize  Arrows=nudge",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 8 });
            GUILayout.FlexibleSpace();
        }

        DrawResizeHandle(windowWidth);
    }

    // ── Grid ──────────────────────────────────────────────────────────────
    private void DrawGrid(float sx, float sy, Vector2 ps)
    {
        if (_s.GridSize < 1) return;
        Handles.BeginGUI();
        int stX = Mathf.FloorToInt(ps.x / _s.GridSize);
        int stY = Mathf.FloorToInt(ps.y / _s.GridSize);
        for (int i = 0; i <= stX; i++)
        {
            Handles.color = i % 8 == 0 ? new Color(1,1,1,0.09f) : new Color(1,1,1,0.04f);
            float x = ContentRect.x + i * _s.GridSize * sx;
            Handles.DrawLine(new Vector3(x, ContentRect.y, 0), new Vector3(x, ContentRect.yMax, 0));
        }
        for (int i = 0; i <= stY; i++)
        {
            Handles.color = i % 8 == 0 ? new Color(1,1,1,0.09f) : new Color(1,1,1,0.04f);
            float y = ContentRect.y + i * _s.GridSize * sy;
            Handles.DrawLine(new Vector3(ContentRect.x, y, 0), new Vector3(ContentRect.xMax, y, 0));
        }
        Handles.EndGUI();
    }

    // ── Rulers ────────────────────────────────────────────────────────────
    private void DrawRulers(Vector2 ps, float sx, float sy)
    {
        var topR  = new Rect(ContentRect.x, ContentRect.y - RULER, ContentRect.width, RULER);
        var leftR = new Rect(ContentRect.x - RULER, ContentRect.y, RULER, ContentRect.height);
        EditorGUI.DrawRect(topR,  FigmaStyleKit.ColRuler);
        EditorGUI.DrawRect(leftR, FigmaStyleKit.ColRuler);
        EditorGUI.DrawRect(new Rect(topR.x - RULER, topR.y, RULER, RULER), FigmaStyleKit.ColRuler);

        var tickStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 8, alignment = TextAnchor.MiddleCenter, normal = { textColor = FigmaStyleKit.ColRulerText } };

        Handles.BeginGUI();

        float stepX = ps.x > 1000 ? 200 : ps.x > 400 ? 100 : 50;
        for (float v = 0; v <= ps.x + 0.01f; v += stepX)
        {
            float x = ContentRect.x + v * sx;
            Handles.color = new Color(0.5f, 0.5f, 0.55f, 0.8f);
            Handles.DrawLine(new Vector3(x, topR.yMax - RULER * 0.45f, 0), new Vector3(x, topR.yMax, 0));
            GUI.Label(new Rect(x - 16, topR.y + 2, 32, 12), v.ToString("F0"), tickStyle);
        }

        float stepY = ps.y > 1000 ? 200 : ps.y > 400 ? 100 : 50;
        for (float v = 0; v <= ps.y + 0.01f; v += stepY)
        {
            float y = ContentRect.y + v * sy;
            Handles.color = new Color(0.5f, 0.5f, 0.55f, 0.8f);
            Handles.DrawLine(new Vector3(leftR.xMax - RULER * 0.45f, y, 0), new Vector3(leftR.xMax, y, 0));
            GUI.Label(new Rect(leftR.x, y - 6, RULER, 12), v.ToString("F0"), tickStyle);
        }

        if (ContentRect.Contains(_mousePos))
        {
            float vx = (_mousePos.x - ContentRect.x) / sx;
            float vy = (_mousePos.y - ContentRect.y) / sy;
            Handles.color = FigmaStyleKit.ColAccent;
            Handles.DrawLine(new Vector3(_mousePos.x, topR.y,   0), new Vector3(_mousePos.x, topR.yMax,  0));
            Handles.DrawLine(new Vector3(leftR.x, _mousePos.y,  0), new Vector3(leftR.xMax, _mousePos.y, 0));
            DrawRulerBadge(vx, _mousePos.x - 14, topR.y,       28, RULER);
            DrawRulerBadge(vy, leftR.x,           _mousePos.y - 8, RULER, 16);
        }

        Handles.EndGUI();
    }

    private static void DrawRulerBadge(float v, float x, float y, float w, float h)
    {
        EditorGUI.DrawRect(new Rect(x, y, w, h),
            new Color(FigmaStyleKit.ColAccent.r, FigmaStyleKit.ColAccent.g, FigmaStyleKit.ColAccent.b, 0.92f));
        GUI.Label(new Rect(x, y, w, h), v.ToString("F0"),
            new GUIStyle(GUI.skin.label)
                { fontSize = 8, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
    }

    // ── Spacing ───────────────────────────────────────────────────────────
    private void DrawSpacing(float sx, float sy)
    {
        float ol  = ContentRect.x + _s.L * sx;
        float or2 = ContentRect.x + ContentRect.width  - _s.R * sx;
        float ot  = ContentRect.y + _s.T * sy;
        float ob  = ContentRect.y + ContentRect.height - _s.B * sy;
        float mx  = (ol + or2) * 0.5f;
        float my  = (ot + ob)  * 0.5f;

        Handles.color = new Color(1f, 0.45f, 0.15f, 0.75f);

        if (_s.L > 0.5f) { DrawArrow(new Vector2(ContentRect.x,    my), new Vector2(ol,  my)); DrawSpaceBadge((ContentRect.x + ol) * 0.5f,       my,  _s.L); }
        if (_s.R > 0.5f) { DrawArrow(new Vector2(ContentRect.xMax, my), new Vector2(or2, my)); DrawSpaceBadge((or2 + ContentRect.xMax) * 0.5f,    my,  _s.R); }
        if (_s.T > 0.5f) { DrawArrow(new Vector2(mx, ContentRect.y),    new Vector2(mx,  ot)); DrawSpaceBadge(mx, (ContentRect.y + ot) * 0.5f,        _s.T); }
        if (_s.B > 0.5f) { DrawArrow(new Vector2(mx, ContentRect.yMax), new Vector2(mx,  ob)); DrawSpaceBadge(mx, (ob + ContentRect.yMax) * 0.5f,     _s.B); }
    }

    private static void DrawArrow(Vector2 from, Vector2 to)
    {
        Handles.DrawLine(new Vector3(from.x, from.y, 0), new Vector3(to.x, to.y, 0));
        var dir  = (to - from).normalized;
        var perp = new Vector2(-dir.y, dir.x) * 4;
        var f3   = new Vector3(from.x, from.y, 0);
        var t3   = new Vector3(to.x,   to.y,   0);
        Handles.DrawLine(f3 + new Vector3(perp.x, perp.y, 0), f3 - new Vector3(perp.x, perp.y, 0));
        Handles.DrawLine(t3 + new Vector3(perp.x, perp.y, 0), t3 - new Vector3(perp.x, perp.y, 0));
    }

    private static void DrawSpaceBadge(float x, float y, float v)
    {
        string t = v.ToString("F0");
        var st   = new GUIStyle(GUI.skin.label)
            { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.45f, 0.15f) } };
        var sz   = st.CalcSize(new GUIContent(t));
        var bg   = new Rect(x - sz.x * 0.5f - 3, y - sz.y * 0.5f - 1, sz.x + 6, sz.y + 2);
        EditorGUI.DrawRect(bg, new Color(0.08f, 0.08f, 0.1f, 0.9f));
        FigmaDrawUtils.DrawBorder(bg, new Color(1f, 0.45f, 0.15f, 0.5f));
        GUI.Label(new Rect(bg.x + 3, bg.y + 1, sz.x, sz.y), t, st);
    }

    // ── Dimensions ────────────────────────────────────────────────────────
    private void DrawDimensions()
    {
        float cx      = ObjectRect.x + ObjectRect.width  * 0.5f;
        float bottomY = ObjectRect.yMax;
        float offset  = Mathf.Clamp(ObjectRect.width * 0.18f, 16f, 60f);

        // W — снизу слева от центра
        float wBadgeX = cx - offset;
        float wBadgeY = bottomY + 14f;

        if (wBadgeY + 14 < ContentRect.yMax)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.18f);
            Handles.DrawLine(new Vector3(ObjectRect.x,    bottomY + 4, 0), new Vector3(ObjectRect.xMax, bottomY + 4, 0));
            Handles.DrawLine(new Vector3(ObjectRect.x,    bottomY,     0), new Vector3(ObjectRect.x,    bottomY + 8, 0));
            Handles.DrawLine(new Vector3(ObjectRect.xMax, bottomY,     0), new Vector3(ObjectRect.xMax, bottomY + 8, 0));
            Handles.DrawLine(new Vector3(ObjectRect.x,    bottomY + 4, 0), new Vector3(wBadgeX,         wBadgeY,     0));
            DrawDimBadge(wBadgeX, wBadgeY, $"W:{_s.W:F0}", BadgeAnchor.Right);
        }

        // H — снизу справа от центра
        float hBadgeX = cx + offset;
        float hBadgeY = bottomY + 14f;

        if (hBadgeY + 14 < ContentRect.yMax)
        {
            float lineX = ObjectRect.xMax + 6;
            if (lineX + 6 < ContentRect.xMax)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.18f);
                Handles.DrawLine(new Vector3(lineX, ObjectRect.y,    0), new Vector3(lineX, ObjectRect.yMax, 0));
                Handles.DrawLine(new Vector3(lineX - 4, ObjectRect.y,    0), new Vector3(lineX + 4, ObjectRect.y,    0));
                Handles.DrawLine(new Vector3(lineX - 4, ObjectRect.yMax, 0), new Vector3(lineX + 4, ObjectRect.yMax, 0));
            }
            Handles.color = new Color(1f, 1f, 1f, 0.18f);
            Handles.DrawLine(new Vector3(ObjectRect.xMax, bottomY + 4, 0), new Vector3(hBadgeX, hBadgeY, 0));
            DrawDimBadge(hBadgeX, hBadgeY, $"H:{_s.H:F0}", BadgeAnchor.Left);
        }
    }

    private enum BadgeAnchor { Left, Right, Center }

    private static void DrawDimBadge(float x, float y, string text, BadgeAnchor anchor = BadgeAnchor.Center)
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 9, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.9f, 1f) }
        };
        var sz   = st.CalcSize(new GUIContent(text));
        float pw = sz.x + 6, ph = sz.y + 2;
        float bx = anchor switch
        {
            BadgeAnchor.Right  => x - pw,
            BadgeAnchor.Left   => x,
            _                  => x - pw * 0.5f
        };
        var bg = new Rect(bx, y - ph * 0.5f, pw, ph);
        EditorGUI.DrawRect(bg, new Color(0.08f, 0.08f, 0.11f, 0.9f));
        FigmaDrawUtils.DrawBorder(bg, new Color(1f, 1f, 1f, 0.15f));
        GUI.Label(new Rect(bg.x + 3, bg.y + 1, sz.x, sz.y), text, st);
    }

    // ── Pivot ─────────────────────────────────────────────────────────────
    private void DrawPivot()
    {
        if (_s.Target == null) return;
        var   pv = _s.Target.pivot;
        float px = ObjectRect.x + ObjectRect.width  * pv.x;
        float py = ObjectRect.y + ObjectRect.height * (1f - pv.y);
        Handles.color = new Color(1f, 0.25f, 0.25f, 0.85f);
        Handles.DrawSolidDisc(new Vector3(px, py, 0), Vector3.forward, 3f);
        Handles.color = new Color(1f, 1f, 1f, 0.6f);
        Handles.DrawWireDisc(new Vector3(px, py, 0), Vector3.forward, 5f);
        Handles.color = new Color(1f, 0.25f, 0.25f, 0.45f);
        Handles.DrawLine(new Vector3(px - 9, py, 0), new Vector3(px + 9, py, 0));
        Handles.DrawLine(new Vector3(px, py - 9, 0), new Vector3(px, py + 9, 0));
    }

    // ── Resize handles ────────────────────────────────────────────────────
    public void DrawHandles()
    {
        var pos = GetHandlePositions(ObjectRect);
        for (int i = 0; i < pos.Length; i++)
        {
            bool  hover = Vector2.Distance(_mousePos, pos[i]) < 10f;
            Color c     = hover ? _s.HandleHover : _s.HandleColor;
            if (hover)
            {
                Handles.color = new Color(c.r, c.g, c.b, 0.2f);
                Handles.DrawSolidDisc(new Vector3(pos[i].x, pos[i].y, 0), Vector3.forward, 9f);
            }
            Handles.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            Handles.DrawSolidDisc(new Vector3(pos[i].x, pos[i].y, 0), Vector3.forward, 5.5f);
            Handles.color = c;
            Handles.DrawSolidDisc(new Vector3(pos[i].x, pos[i].y, 0), Vector3.forward, 4.5f);
        }
    }

    public static Vector2[] GetHandlePositions(Rect r)
    {
        float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
        return new[]
        {
            new Vector2(r.x,    r.y),    new Vector2(cx, r.y),    new Vector2(r.xMax, r.y),
            new Vector2(r.x,    cy),                               new Vector2(r.xMax, cy),
            new Vector2(r.x,    r.yMax), new Vector2(cx, r.yMax), new Vector2(r.xMax, r.yMax),
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static RectTransform GetCanvasRect(RectTransform rt)
    {
        if (rt == null) return null;
        var canvas = rt.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    private static void DrawCornerMarkers(Rect r)
    {
        float len = Mathf.Min(7f, r.width * 0.18f, r.height * 0.18f);
        Handles.color = new Color(0.35f, 0.65f, 1f, 0.45f);
        Handles.DrawLine(new Vector3(r.x,        r.y + len,  0), new Vector3(r.x,        r.y,        0));
        Handles.DrawLine(new Vector3(r.x,        r.y,        0), new Vector3(r.x + len,  r.y,        0));
        Handles.DrawLine(new Vector3(r.xMax-len, r.y,        0), new Vector3(r.xMax,     r.y,        0));
        Handles.DrawLine(new Vector3(r.xMax,     r.y,        0), new Vector3(r.xMax,     r.y + len,  0));
        Handles.DrawLine(new Vector3(r.x,        r.yMax-len, 0), new Vector3(r.x,        r.yMax,     0));
        Handles.DrawLine(new Vector3(r.x,        r.yMax,     0), new Vector3(r.x + len,  r.yMax,     0));
        Handles.DrawLine(new Vector3(r.xMax-len, r.yMax,     0), new Vector3(r.xMax,     r.yMax,     0));
        Handles.DrawLine(new Vector3(r.xMax,     r.yMax-len, 0), new Vector3(r.xMax,     r.yMax,     0));
    }

    private static void DrawShadow(Rect r)
    {
        for (int i = 5; i >= 1; i--)
            EditorGUI.DrawRect(
                new Rect(r.x - i, r.y + i * 0.4f, r.width + i * 2, r.height + i),
                new Color(0, 0, 0, 0.035f * (6 - i)));
    }

    private void DrawSafeZone(float sx, float sy)
    {
        float ins = 20;
        var sz = new Rect(
            ContentRect.x + ins * sx, ContentRect.y + ins * sy,
            ContentRect.width - 2 * ins * sx, ContentRect.height - 2 * ins * sy);
        EditorGUI.DrawRect(sz, new Color(0f, 1f, 0.8f, 0.07f));
        Handles.color = new Color(0f, 1f, 0.8f, 0.4f);
        FigmaDrawUtils.HandlesRect(sz);
    }

    // ── Panel resize handle ───────────────────────────────────────────────
    private void DrawResizeHandle(float windowWidth)
    {
        var hr = GUILayoutUtility.GetRect(windowWidth - 20, 6);
        hr.x  += 10;
        EditorGUI.DrawRect(hr, FigmaStyleKit.ColPanel);
        EditorGUI.DrawRect(new Rect(hr.x + hr.width * 0.4f, hr.y + 2, hr.width * 0.2f, 2), FigmaStyleKit.ColBorder);
        EditorGUIUtility.AddCursorRect(hr, MouseCursor.ResizeVertical);

        var e = Event.current;
        if (e.type == EventType.MouseDown && hr.Contains(e.mousePosition))
        {
            _isResizingPanel   = true;
            _resizePanelStartY = e.mousePosition.y;
            _resizePanelStartH = _s.VisualHeight;
            e.Use();
        }
        if (e.type == EventType.MouseDrag && _isResizingPanel)
        {
            _s.VisualHeight = Mathf.Clamp(
                _resizePanelStartH + (e.mousePosition.y - _resizePanelStartY), 120, 600);
            _s.NeedsRepaint = true;
            e.Use();
        }
        if (e.type == EventType.MouseUp && _isResizingPanel) { _isResizingPanel = false; e.Use(); }
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  INPUT HANDLER  (drag, resize, scroll-zoom, pan, keyboard)
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaInputHandler
    {
        private readonly FigmaState  _s;
        private readonly FigmaSync   _sync;
        private readonly FigmaVisualEditor _ve;

        private bool    _isDragging, _isResizing, _isPanning;
        private int     _resizeHandle = -1;
        private Vector2 _dragStartMouse, _dragStartPos, _dragStartSize, _dragStartOffsets;
        private Vector2 _panStartMouse, _panStartOffset;

        public FigmaInputHandler(FigmaState s, FigmaSync sync, FigmaVisualEditor ve)
        {
            _s = s; _sync = sync; _ve = ve;
        }

        public void HandleMouseEvents(Rect visualRect, Rect contentRect, Rect objectRect)
        {
            var e = Event.current;
            if (e == null || _s.Target == null || _s.Parent == null) return;

            if (e.type == EventType.ScrollWheel && visualRect.Contains(e.mousePosition))
            {
                float delta = -e.delta.y * 0.06f;
                _s.Zoom = Mathf.Clamp(_s.Zoom + delta, 0.25f, 4f);
                _s.NeedsRepaint = true;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && (e.button == 1 || e.button == 2) && visualRect.Contains(e.mousePosition))
            {
                _isPanning        = true;
                _panStartMouse    = e.mousePosition;
                _panStartOffset   = _s.PanOffset;
                e.Use(); return;
            }
            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _s.PanOffset    = _panStartOffset + (e.mousePosition - _panStartMouse);
                _s.NeedsRepaint = true;
                e.Use(); return;
            }
            if (e.type == EventType.MouseUp && _isPanning && (e.button == 1 || e.button == 2))
            {
                _isPanning = false; e.Use(); return;
            }

            GetScaleFactors(out float sx, out float sy);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                int h = GetHandleAt(e.mousePosition, objectRect);
                if (h >= 0)
                {
                    _isResizing       = true;
                    _resizeHandle     = h;
                    _dragStartMouse   = e.mousePosition;
                    _dragStartSize    = new Vector2(_s.W, _s.H);
                    _dragStartOffsets = new Vector2(_s.L, _s.T);
                    _s.StorePrev();
                    e.Use(); return;
                }
                if (objectRect.Contains(e.mousePosition))
                {
                    _isDragging     = true;
                    _dragStartMouse = e.mousePosition;
                    _dragStartPos   = new Vector2(_s.L, _s.T);
                    _s.StorePrev();
                    e.Use(); return;
                }
            }

            if (e.type == EventType.MouseDrag && _isDragging)
            {
                var d = e.mousePosition - _dragStartMouse;
                var ps = _s.Parent.rect.size;

                float newL = _dragStartPos.x + d.x / sx;
                float newT = _dragStartPos.y + d.y / sy;

                float snapThreshold = 5f;
                newL = SnapValue(newL, snapThreshold, ps.x, _s.W, true);
                newT = SnapValue(newT, snapThreshold, ps.y, _s.H, false);

                _s.L = Mathf.Clamp(newL, 0, ps.x - _s.W);
                _s.T = Mathf.Clamp(newT, 0, ps.y - _s.H);

                if (_s.SnapToPixels) { _s.L = Mathf.Round(_s.L); _s.T = Mathf.Round(_s.T); }

                _s.R = Mathf.Max(0, ps.x - _s.L - _s.W);
                _s.B = Mathf.Max(0, ps.y - _s.T - _s.H);

                if (_s.LivePreview) _sync.Apply();
                _s.NeedsRepaint = true;
                e.Use(); return;
            }

            if (e.type == EventType.MouseDrag && _isResizing)
            {
                var d  = e.mousePosition - _dragStartMouse;
                float dw = d.x / sx, dh = d.y / sy;
                ApplyResize(dw, dh);
                SnapValues();
                var ps = _s.Parent.rect.size;
                _s.R = Mathf.Max(0, ps.x - _s.L - _s.W);
                _s.B = Mathf.Max(0, ps.y - _s.T - _s.H);
                if (_s.LivePreview) _sync.Apply();
                _s.NeedsRepaint = true;
                e.Use(); return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && (_isDragging || _isResizing))
            {
                _isDragging = false; _isResizing = false; _resizeHandle = -1;
                _sync.Apply(true);
                e.Use(); return;
            }

            if (e.type == EventType.MouseMove) _s.NeedsRepaint = true;
        }

        public void HandleKeyboard()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown || _s.Target == null) return;

            float step = e.shift ? 10f : 1f;
            var ps = _s.Parent != null ? _s.Parent.rect.size : Vector2.one * 9999;
            bool moved = false;

            switch (e.keyCode)
            {
                case KeyCode.LeftArrow:  _s.L = Mathf.Max(0, _s.L - step); _s.R = Mathf.Max(0, ps.x - _s.L - _s.W); moved = true; break;
                case KeyCode.RightArrow: _s.L = Mathf.Max(0, _s.L + step); _s.R = Mathf.Max(0, ps.x - _s.L - _s.W); moved = true; break;
                case KeyCode.UpArrow:    _s.T = Mathf.Max(0, _s.T - step); _s.B = Mathf.Max(0, ps.y - _s.T - _s.H); moved = true; break;
                case KeyCode.DownArrow:  _s.T = Mathf.Max(0, _s.T + step); _s.B = Mathf.Max(0, ps.y - _s.T - _s.H); moved = true; break;
            }

            if (!moved) return;
            _sync.Apply(true);
            _s.NeedsRepaint = true;
            e.Use();
        }

        private float SnapValue(float val, float threshold, float parentSize, float objSize, bool isHorizontal)
        {
            var snapPoints = new List<float> { 0, parentSize * 0.5f - objSize * 0.5f, parentSize - objSize };

            if (_s.RelativeToCanvas && _s.Target != null)
            {
                var directParent = _s.Target.parent as RectTransform;
                if (directParent != null && directParent != _s.Parent)
                {
                    var parentBounds = new FigmaValues(directParent, _s.Parent);
                    if (isHorizontal)
                    {
                        snapPoints.Add(parentBounds.L);
                        snapPoints.Add(parentBounds.L + (parentBounds.W - objSize) * 0.5f);
                        snapPoints.Add(parentBounds.L + parentBounds.W - objSize);
                    }
                    else
                    {
                        snapPoints.Add(parentBounds.T);
                        snapPoints.Add(parentBounds.T + (parentBounds.H - objSize) * 0.5f);
                        snapPoints.Add(parentBounds.T + parentBounds.H - objSize);
                    }
                }
            }

            foreach (float p in snapPoints)
            {
                if (Mathf.Abs(val - p) < threshold)
                    return p;
            }
            return val;
        }

        private void ApplyResize(float dw, float dh)
        {
            float nl = _dragStartOffsets.x, nt = _dragStartOffsets.y;
            float nw = _dragStartSize.x,    nh = _dragStartSize.y;

            switch (_resizeHandle)
            {
                case 0: nl += dw; nw -= dw; nt += dh; nh -= dh; break;
                case 1:                      nt += dh; nh -= dh; break;
                case 2:           nw += dw; nt += dh; nh -= dh; break;
                case 3: nl += dw; nw -= dw;                      break;
                case 4:           nw += dw;                      break;
                case 5: nl += dw; nw -= dw;           nh += dh; break;
                case 6:                                nh += dh; break;
                case 7:           nw += dw;            nh += dh; break;
            }

            if (_s.MaintainAspect && _s.AspectRatio > 0)
            {
                if (_resizeHandle == 1 || _resizeHandle == 6)      nw = nh * _s.AspectRatio;
                else if (_resizeHandle == 3 || _resizeHandle == 4)  nh = nw / _s.AspectRatio;
                else if (Mathf.Abs(dw) > Mathf.Abs(dh))            nh = nw / _s.AspectRatio;
                else                                                 nw = nh * _s.AspectRatio;
            }

            _s.L = Mathf.Max(0, nl);
            _s.T = Mathf.Max(0, nt);
            _s.W = Mathf.Max(1, nw);
            _s.H = Mathf.Max(1, nh);
        }

        private void SnapValues()
        {
            if (_s.SnapToGrid)
            {
                float g = _s.GridSize;
                _s.L = Mathf.Round(_s.L / g) * g;
                _s.T = Mathf.Round(_s.T / g) * g;
                _s.W = Mathf.Max(1, Mathf.Round(_s.W / g) * g);
                _s.H = Mathf.Max(1, Mathf.Round(_s.H / g) * g);
            }
            else if (_s.SnapToPixels)
            {
                _s.L = Mathf.Round(_s.L); _s.T = Mathf.Round(_s.T);
                _s.W = Mathf.Max(1, Mathf.Round(_s.W));
                _s.H = Mathf.Max(1, Mathf.Round(_s.H));
            }
        }

        private void GetScaleFactors(out float sx, out float sy)
        {
            var ps   = _s.Parent.rect.size;
            float margin = _s.ShowRulers ? 22f : 4f;
            float maxW = (_ve.VisualRect.width  - margin - 12) * _s.Zoom;
            float maxH = (_ve.VisualRect.height - margin - 12) * _s.Zoom;
            float aspect = ps.x / Mathf.Max(1, ps.y);
            float cw = maxW, ch = cw / aspect;
            if (ch > maxH) { ch = maxH; cw = ch * aspect; }
            sx = cw / ps.x; sy = ch / ps.y;
        }

        private static int GetHandleAt(Vector2 mouse, Rect objectRect)
        {
            var pos = FigmaVisualEditor.GetHandlePositions(objectRect);
            for (int i = 0; i < pos.Length; i++)
                if (Vector2.Distance(mouse, pos[i]) < 11f) return i;
            return -1;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRAGGABLE FIELD  (ЛКМ + drag по лейблу меняет значение)
    // ═══════════════════════════════════════════════════════════════════════
    public static class FigmaDragField
    {
        private static int   _activeControlId = -1;
        private static float _dragStartX;
        private static float _dragStartValue;
        private static bool  _didDrag;

        /// <summary>
        /// Рисует лейбл+поле. Drag по лейблу = изменение значения.
        /// Возвращает true если значение изменилось.
        /// </summary>
        public static bool Draw(Rect labelRect, Rect fieldRect, string label, ref float value,
            float speed = 1f, float min = 0f, float max = float.MaxValue)
        {
            int id = GUIUtility.GetControlID(label.GetHashCode(), FocusType.Passive, labelRect);
            bool changed = false;
            var e = Event.current;

            // Draw label with drag cursor hint
            bool hover = labelRect.Contains(e.mousePosition);
            EditorGUI.DrawRect(labelRect, hover
                ? new Color(FigmaStyleKit.ColAccent.r, FigmaStyleKit.ColAccent.g, FigmaStyleKit.ColAccent.b, 0.18f)
                : new Color(0.14f, 0.14f, 0.17f, 1f));
            FigmaDrawUtils.DrawBorder(labelRect, hover ? FigmaStyleKit.ColAccent : FigmaStyleKit.ColBorder);

            GUI.Label(labelRect, label, new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9, alignment = TextAnchor.MiddleCenter,
                fontStyle = hover ? FontStyle.Bold : FontStyle.Normal,
                normal    = { textColor = hover ? FigmaStyleKit.ColAccent : FigmaStyleKit.ColTextDim }
            });
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);

            // Drag logic
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && labelRect.Contains(e.mousePosition):
                    _activeControlId = id;
                    _dragStartX      = e.mousePosition.x;
                    _dragStartValue  = value;
                    _didDrag         = false;
                    GUIUtility.hotControl = id;
                    e.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    float delta = (e.mousePosition.x - _dragStartX) * speed;
                    value   = Mathf.Clamp(_dragStartValue + delta, min, max);
                    _didDrag = true;
                    changed  = true;
                    e.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    _activeControlId      = -1;
                    e.Use();
                    break;
            }

            // Float field (right side)
            EditorGUI.DrawRect(fieldRect, new Color(0.1f, 0.1f, 0.12f, 1f));
            FigmaDrawUtils.DrawBorder(fieldRect, FigmaStyleKit.ColBorder);
            EditorGUI.BeginChangeCheck();
            float newVal = EditorGUI.FloatField(fieldRect, value, new GUIStyle(EditorStyles.numberField)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = FigmaStyleKit.ColText }
            });
            if (EditorGUI.EndChangeCheck()) { value = Mathf.Clamp(newVal, min, max); changed = true; }

            return changed;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CONSTRAINTS UI  (панель полей L/R/T/B/W/H)
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaConstraintsUI
{
    private readonly FigmaState    _s;
    private readonly FigmaStyleKit _sk;
    private readonly FigmaSync     _sync;

    private const float Epsilon = 0.001f;

    public FigmaConstraintsUI(FigmaState s, FigmaStyleKit sk, FigmaSync sync)
    {
        _s = s; _sk = sk; _sync = sync;
    }

    public void DrawFieldsPanel(float windowWidth)
    {
        FigmaDrawUtils.SectionHeader("Figma Constraints (L/R/T/B/W/H)", windowWidth);

        EditorGUILayout.LabelField(
            "Drag по названию параметра (◄►) или вводи цифру",
            new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 8 });
        EditorGUILayout.Space(2);

        float pw  = windowWidth - 30;
        float pad = 10f;
        float fw  = (pw - pad * 5) / 3f;
        float lh  = 14f;
        float ih  = 22f;
        float fh  = lh + ih + 2f;

        var baseRect = GUILayoutUtility.GetRect(pw, fh * 2 + 30);
        baseRect.x += 10;

        float row0 = baseRect.y;
        float row1 = row0 + fh + 4;
        float row2 = row1 + fh + 4;
        float col0 = baseRect.x;
        float col1 = col0 + fw + pad;
        float col2 = col1 + fw + pad;

        bool cL = DrawField(col0, row0, fw, lh, ih, "◄► Left",   ref _s.L);
        bool cW = DrawField(col1, row0, fw, lh, ih, "◄► Width",  ref _s.W, 1);
        bool cR = DrawField(col2, row0, fw, lh, ih, "◄► Right",  ref _s.R);

        bool cT = DrawField(col0, row1, fw, lh, ih, "◄► Top",    ref _s.T);
        bool cH = DrawField(col1, row1, fw, lh, ih, "◄► Height", ref _s.H, 1);
        bool cB = DrawField(col2, row1, fw, lh, ih, "◄► Bottom", ref _s.B);

        bool changed = false;
        var  ps      = _s.Parent != null ? _s.Parent.rect.size : Vector2.zero;

        // Горизонталь
        if      (cL && !cW && !cR) { _s.R = Mathf.Max(0, ps.x - _s.L - _s.W); changed = true; }
        else if (cR && !cW && !cL) { _s.L = Mathf.Max(0, ps.x - _s.R - _s.W); changed = true; }
        else if (cW && !cL && !cR) { _s.R = Mathf.Max(0, ps.x - _s.L - _s.W); changed = true; }
        else if (cL || cW || cR)   { changed = true; }

        // Вертикаль
        if      (cT && !cH && !cB) { _s.B = Mathf.Max(0, ps.y - _s.T - _s.H); changed = true; }
        else if (cB && !cH && !cT) { _s.T = Mathf.Max(0, ps.y - _s.B - _s.H); changed = true; }
        else if (cH && !cT && !cB) { _s.B = Mathf.Max(0, ps.y - _s.T - _s.H); changed = true; }
        else if (cT || cH || cB)   { changed = true; }

        // Quick actions
        if (GUI.Button(new Rect(col0, row2, fw, 22), "Center", _sk.Btn))
            _sync.ResetToCenter();
        if (GUI.Button(new Rect(col1, row2, fw, 22), "↻ Sync", _sk.Btn))
            _sync.SyncFromSelection();
        _s.SnapToPixels = GUI.Toggle(new Rect(col2, row2, fw, 22), _s.SnapToPixels, " Snap px");

        if (changed)
        {
            _s.Clamp();
            if (_s.LivePreview) _sync.Apply();
        }
    }

    private static bool DrawField(float x, float y, float fw, float lh, float ih,
        string label, ref float value, float minVal = 0f)
    {
        var labelR = new Rect(x, y,          fw, lh);
        var fieldR = new Rect(x, y + lh + 2, fw, ih);
        return FigmaDragField.Draw(labelR, fieldR, label, ref value, speed: 1f, min: minVal);
    }

    // ── Constraints tab ───────────────────────────────────────────────────
    public void DrawConstraintsTab(float windowWidth)
    {
        FigmaDrawUtils.SectionHeader("Anchor Presets", windowWidth);
        EditorGUILayout.Space(4);
        DrawAnchorGrid(windowWidth);

        EditorGUILayout.Space(8);
        FigmaDrawUtils.SectionHeader("Pivot Presets", windowWidth);
        EditorGUILayout.Space(4);
        DrawPivotGrid(windowWidth);

        EditorGUILayout.Space(8);
        FigmaDrawUtils.SectionHeader("Tools", windowWidth);
        EditorGUILayout.Space(4);
        DrawToolsSection(windowWidth);
    }

    // ── Anchor grid ───────────────────────────────────────────────────────
    private void DrawAnchorGrid(float windowWidth)
    {
        float bw = Mathf.Clamp((windowWidth - 60) / 4f, 55, 80);

        string[,] labels =
        {
            { "TL","TC","TR","TS" },
            { "ML","MC","MR","MS" },
            { "BL","BC","BR","BS" },
            { "SL","SC","SR","SS" }
        };
        Vector2[,] amins =
        {
            { new(0,1),   new(.5f,1),   new(1,1),  new(0,1)   },
            { new(0,.5f), new(.5f,.5f), new(1,.5f),new(0,.5f) },
            { new(0,0),   new(.5f,0),   new(1,0),  new(0,0)   },
            { new(0,0),   new(.5f,0),   new(1,0),  new(0,0)   }
        };
        Vector2[,] amaxs =
        {
            { new(0,1),   new(.5f,1),   new(1,1),  new(1,1)   },
            { new(0,.5f), new(.5f,.5f), new(1,.5f),new(1,.5f) },
            { new(0,0),   new(.5f,0),   new(1,0),  new(1,0)   },
            { new(0,1),   new(.5f,1),   new(1,1),  new(1,1)   }
        };

        for (int row = 0; row < 4; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                for (int col = 0; col < 4; col++)
                {
                    bool active = _s.Target != null &&
                                  _s.Target.anchorMin == amins[row, col] &&
                                  _s.Target.anchorMax == amaxs[row, col];

                    if (GUILayout.Button(labels[row, col],
                            active ? _sk.TabActive : _sk.Tab,
                            GUILayout.Width(bw), GUILayout.Height(24)))
                    {
                        if (_s.Target != null)
                        {
                            Undo.RecordObject(_s.Target, "Anchor Preset");
                            _s.Target.anchorMin = amins[row, col];
                            _s.Target.anchorMax = amaxs[row, col];
                            _sync.Calculate();
                            EditorUtility.SetDirty(_s.Target);
                        }
                    }
                    if (col < 3) GUILayout.Space(2);
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.Space(2);
        }
    }

    // ── Pivot grid ────────────────────────────────────────────────────────
    private void DrawPivotGrid(float windowWidth)
    {
        float bw = Mathf.Clamp((windowWidth - 60) / 3f, 60, 90);

        string[]  lbs = { "↖","↑","↗","←","⊙","→","↙","↓","↘" };
        Vector2[] pvs =
        {
            new(0,1),   new(.5f,1),   new(1,1),
            new(0,.5f), new(.5f,.5f), new(1,.5f),
            new(0,0),   new(.5f,0),   new(1,0)
        };

        for (int row = 0; row < 3; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                for (int col = 0; col < 3; col++)
                {
                    int  idx    = row * 3 + col;
                    bool active = _s.Target != null && _s.Target.pivot == pvs[idx];

                    if (GUILayout.Button(lbs[idx],
                            active ? _sk.TabActive : _sk.Tab,
                            GUILayout.Width(bw), GUILayout.Height(28)))
                    {
                        if (_s.Target != null)
                        {
                            Undo.RecordObject(_s.Target, "Pivot Preset");
                            _s.Target.pivot = pvs[idx];
                            _sync.Calculate();
                            _sync.Apply(true);
                            EditorUtility.SetDirty(_s.Target);
                        }
                    }
                    if (col < 2) GUILayout.Space(2);
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.Space(2);
        }
    }

    // ── Tools section ─────────────────────────────────────────────────────
    private void DrawToolsSection(float windowWidth)
    {
        // ── Anchors to Corners ───────────────────────────────────────────
        using (new EditorGUILayout.VerticalScope())
        {
            // Описание
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                var descRect = GUILayoutUtility.GetRect(windowWidth - 20, 32);
                EditorGUI.DrawRect(descRect, new Color(0.14f, 0.14f, 0.18f, 1f));
                FigmaDrawUtils.DrawBorder(descRect, FigmaStyleKit.ColBorder);
                GUI.Label(new Rect(descRect.x + 8, descRect.y + 4, descRect.width - 16, 12),
                    "Anchors to Corners — перемещает anchor points в углы объекта",
                    new GUIStyle(GUI.skin.label)
                        { fontSize = 9, normal = { textColor = FigmaStyleKit.ColTextDim } });
                GUI.Label(new Rect(descRect.x + 8, descRect.y + 18, descRect.width - 16, 12),
                    "offsetMin и offsetMax обнуляются. Работает с множественным выбором.",
                    new GUIStyle(GUI.skin.label)
                        { fontSize = 9, normal = { textColor = FigmaStyleKit.ColTextDim } });
            }

            EditorGUILayout.Space(4);

            // Кнопка
            bool canExecute = CanExecuteAnchorsToCorners();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);

                GUI.enabled = canExecute;

                // Стиль кнопки — акцентный если можно нажать
                var btnStyle = new GUIStyle(_sk.Btn)
                {
                    fontSize  = 11,
                    fontStyle = FontStyle.Bold,
                    normal    =
                    {
                        textColor  = canExecute ? Color.white : FigmaStyleKit.ColTextDim,
                        background = FigmaDrawUtils.MakeTex(2, 2, canExecute
                            ? new Color(0.18f, 0.38f, 0.7f, 1f)
                            : new Color(0.16f, 0.16f, 0.19f, 1f))
                    },
                    hover =
                    {
                        textColor  = Color.white,
                        background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.28f, 0.52f, 1f, 1f))
                    },
                    active =
                    {
                        textColor  = Color.white,
                        background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.12f, 0.28f, 0.55f, 1f))
                    }
                };

                if (GUILayout.Button("⚓  Anchors to Corners", btnStyle, GUILayout.Height(36)))
                {
                    ExecuteAnchorsToCorners();
                    // После применения пересчитываем значения плагина
                    _sync.SyncFromSelection();
                }

                GUI.enabled = true;
                GUILayout.Space(10);
            }

            // Статус
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                string statusText = canExecute
                    ? $"Готово: {_s.MultiTargets.Count} объект(ов) выбрано"
                    : "Выберите объект(ы) с RectTransform";
                EditorGUILayout.LabelField(statusText,
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = canExecute
                            ? new Color(0.4f, 0.8f, 0.4f, 1f)
                            : FigmaStyleKit.ColTextDim }
                    });
                GUILayout.Space(10);
            }
        }
    }

    // ── Anchors to Corners logic ──────────────────────────────────────────
    private bool CanExecuteAnchorsToCorners()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            return false;
        foreach (var go in Selection.gameObjects)
            if (go.GetComponent<RectTransform>() != null)
                return true;
        return false;
    }

    private void ExecuteAnchorsToCorners()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogWarning($"[AnchorsToCorners] '{go.name}' — нет RectTransform, пропущен.");
                continue;
            }
            Undo.RecordObject(rt, "Anchors to Corners");
            SnapAnchorsToCorners(rt);
            count++;
        }
        Debug.Log($"[AnchorsToCorners] Обработано: {count} объект(ов).");
    }

    private static void SnapAnchorsToCorners(RectTransform rt)
    {
        var parent = rt.parent as RectTransform;
        if (parent == null)
        {
            Debug.LogWarning($"[AnchorsToCorners] '{rt.name}' — нет родителя с RectTransform.");
            return;
        }

        var parentSize = parent.rect.size;
        if (parentSize.x <= Epsilon || parentSize.y <= Epsilon)
        {
            Debug.LogWarning($"[AnchorsToCorners] Родитель '{parent.name}' имеет нулевой размер.");
            return;
        }

        // Получаем мировые углы объекта
        var worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);

        // Переводим в локальное пространство родителя
        Vector2 min = parent.InverseTransformPoint(worldCorners[0]);
        Vector2 max = parent.InverseTransformPoint(worldCorners[2]);

        // Нормализуем относительно размера родителя с учётом его pivot
        var anchorMin = new Vector2(
            min.x / parentSize.x + parent.pivot.x,
            min.y / parentSize.y + parent.pivot.y);
        var anchorMax = new Vector2(
            max.x / parentSize.x + parent.pivot.x,
            max.y / parentSize.y + parent.pivot.y);

        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        EditorUtility.SetDirty(rt);
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  ALIGN UI
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaAlignUI
{
    private readonly FigmaState    _s;
    private readonly FigmaStyleKit _sk;
    private readonly FigmaSync     _sync;
    private float _fillMargin = 16f;
    private float _posMargin  = 0f;

    public FigmaAlignUI(FigmaState s, FigmaStyleKit sk, FigmaSync sync)
    {
        _s = s; _sk = sk; _sync = sync;
    }

    public void Draw(float windowWidth)
    {
        FigmaDrawUtils.SectionHeader("Align & Distribute", windowWidth);
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Horizontal alignment", _sk.Mini);
        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            DrawAlignBtn(AlignDir.Left,    windowWidth);
            GUILayout.Space(4);
            DrawAlignBtn(AlignDir.CenterH, windowWidth);
            GUILayout.Space(4);
            DrawAlignBtn(AlignDir.Right,   windowWidth);
            GUILayout.Space(10);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Vertical alignment", _sk.Mini);
        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            DrawAlignBtn(AlignDir.Top,     windowWidth);
            GUILayout.Space(4);
            DrawAlignBtn(AlignDir.CenterV, windowWidth);
            GUILayout.Space(4);
            DrawAlignBtn(AlignDir.Bottom,  windowWidth);
            GUILayout.Space(10);
        }

        EditorGUILayout.Space(10);

        // ── Position Presets ──────────────────────────────────────────────
        FigmaDrawUtils.SectionHeader("Position Presets", windowWidth);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            GUILayout.Label("Margin:", new GUIStyle(GUI.skin.label)
                { fontSize = 9, normal = { textColor = FigmaStyleKit.ColTextDim }, fixedWidth = 48 });
            _posMargin = EditorGUILayout.FloatField(_posMargin, GUILayout.Width(54));
            GUILayout.Space(4);
            if (GUILayout.Button("0", _sk.Tab, GUILayout.Width(22), GUILayout.Height(18)))
                _posMargin = 0f;
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
        }

        EditorGUILayout.Space(4);
        DrawPositionPresetGrid(windowWidth);

        EditorGUILayout.Space(10);

        // ── Fill ──────────────────────────────────────────────────────────
        FigmaDrawUtils.SectionHeader("Fill Parent", windowWidth);
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Fill (no margin)", _sk.Btn, GUILayout.Height(28)))
                FillParent(0, 0, 0, 0);
            GUILayout.Space(4);
            _fillMargin = EditorGUILayout.FloatField(_fillMargin, GUILayout.Width(44), GUILayout.Height(28));
            GUILayout.Space(4);
            if (GUILayout.Button("Fill + Margin", _sk.Btn, GUILayout.Height(28)))
                FillParent(_fillMargin, _fillMargin, _fillMargin, _fillMargin);
            GUILayout.Space(10);
        }

        EditorGUILayout.Space(10);

        // ── Distribute ────────────────────────────────────────────────────
        bool canDist = _s.MultiTargets.Count >= 3;
        FigmaDrawUtils.SectionHeader("Distribute (3+ objects)", windowWidth);
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            GUI.enabled = canDist;
            if (GUILayout.Button("Distribute H", _sk.Btn, GUILayout.Height(28)))
                Distribute(true);
            GUILayout.Space(4);
            if (GUILayout.Button("Distribute V", _sk.Btn, GUILayout.Height(28)))
                Distribute(false);
            GUI.enabled = true;
            GUILayout.Space(10);
        }
    }

    // ── Position Preset Grid ──────────────────────────────────────────────
    private void DrawPositionPresetGrid(float windowWidth)
    {
        float bw = Mathf.Clamp((windowWidth - 60) / 3f, 55, 90);

        string[,] icons = { { "↖","↑","↗" }, { "←","⊙","→" }, { "↙","↓","↘" } };
        string[,] tips  =
        {
            { "Top-Left",    "Top-Center",    "Top-Right"    },
            { "Middle-Left", "Center",        "Middle-Right" },
            { "Bot-Left",    "Bot-Center",    "Bot-Right"    }
        };

        for (int row = 0; row < 3; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                for (int col = 0; col < 3; col++)
                {
                    int  r       = row, c = col;
                    var  btnRect = GUILayoutUtility.GetRect(bw, 36, GUILayout.Width(bw), GUILayout.Height(36));
                    bool hover   = btnRect.Contains(Event.current.mousePosition);

                    EditorGUI.DrawRect(btnRect, hover
                        ? new Color(0.22f, 0.25f, 0.32f, 1f) : FigmaStyleKit.ColPanel);
                    FigmaDrawUtils.DrawBorder(btnRect,
                        hover ? FigmaStyleKit.ColAccent : FigmaStyleKit.ColBorder);

                    GUI.Label(new Rect(btnRect.x, btnRect.y + 2, btnRect.width, 18),
                        icons[r, c], new GUIStyle(GUI.skin.label)
                        {
                            fontSize  = 14, alignment = TextAnchor.MiddleCenter,
                            normal    = { textColor   = hover ? FigmaStyleKit.ColAccent : FigmaStyleKit.ColText }
                        });
                    GUI.Label(new Rect(btnRect.x, btnRect.y + 19, btnRect.width, 14),
                        tips[r, c], new GUIStyle(GUI.skin.label)
                        {
                            fontSize  = 7, alignment = TextAnchor.MiddleCenter,
                            normal    = { textColor  = FigmaStyleKit.ColTextDim }
                        });

                    if (hover) EditorGUIUtility.AddCursorRect(btnRect, MouseCursor.Link);

                    if (Event.current.type == EventType.MouseDown &&
                        btnRect.Contains(Event.current.mousePosition))
                    {
                        ApplyPositionPreset(r, c, _posMargin);
                        Event.current.Use();
                    }

                    if (col < 2) GUILayout.Space(2);
                }
                GUILayout.Space(10);
            }
            if (row < 2) EditorGUILayout.Space(2);
        }
    }

    private void ApplyPositionPreset(int row, int col, float margin)
    {
        if (_s.Target == null || _s.Parent == null) return;
        Undo.RecordObject(_s.Target, "Position Preset");
        var ps = _s.Parent.rect.size;

        switch (col)
        {
            case 0: _s.L = margin;              _s.R = Mathf.Max(0, ps.x - _s.W - margin); break;
            case 1: _s.L = (ps.x - _s.W)*0.5f; _s.R = _s.L;                               break;
            case 2: _s.R = margin;              _s.L = Mathf.Max(0, ps.x - _s.W - margin); break;
        }
        switch (row)
        {
            case 0: _s.T = margin;              _s.B = Mathf.Max(0, ps.y - _s.H - margin); break;
            case 1: _s.T = (ps.y - _s.H)*0.5f; _s.B = _s.T;                               break;
            case 2: _s.B = margin;              _s.T = Mathf.Max(0, ps.y - _s.H - margin); break;
        }

        _sync.Apply(true);
    }

    // ── Align ─────────────────────────────────────────────────────────────
    private enum AlignDir { Left, CenterH, Right, Top, CenterV, Bottom }

    private void DrawAlignBtn(AlignDir dir, float windowWidth)
    {
        float  bw    = Mathf.Clamp((windowWidth - 60) / 3f, 70, 120);
        string label = dir switch
        {
            AlignDir.Left    => "Align Left",
            AlignDir.CenterH => "Center H",
            AlignDir.Right   => "Align Right",
            AlignDir.Top     => "Align Top",
            AlignDir.CenterV => "Center V",
            AlignDir.Bottom  => "Align Bottom",
            _                => ""
        };

        var  btnRect = GUILayoutUtility.GetRect(bw, 52, GUILayout.Width(bw), GUILayout.Height(52));
        bool hover   = btnRect.Contains(Event.current.mousePosition);

        EditorGUI.DrawRect(btnRect, hover
            ? new Color(0.22f, 0.25f, 0.32f, 1f) : FigmaStyleKit.ColPanel);
        FigmaDrawUtils.DrawBorder(btnRect,
            hover ? FigmaStyleKit.ColAccent : FigmaStyleKit.ColBorder);

        if (Event.current.type == EventType.Repaint)
            DrawAlignIcon(btnRect, dir);

        GUI.Label(new Rect(btnRect.x, btnRect.yMax - 15, btnRect.width, 14), label,
            new GUIStyle(GUI.skin.label)
            {
                fontSize  = 8, alignment = TextAnchor.MiddleCenter,
                normal    = { textColor  = hover ? FigmaStyleKit.ColText : FigmaStyleKit.ColTextDim }
            });

        if (hover) EditorGUIUtility.AddCursorRect(btnRect, MouseCursor.Link);

        if (Event.current.type == EventType.MouseDown &&
            btnRect.Contains(Event.current.mousePosition))
        {
            Align(dir);
            Event.current.Use();
        }
    }

    private static void DrawAlignIcon(Rect r, AlignDir dir)
    {
        float pad = 8f;
        var   a   = new Rect(r.x + pad, r.y + 5, r.width - pad * 2, r.height - 22);
        float cx  = a.x + a.width  * 0.5f;
        float cy  = a.y + a.height * 0.5f;
        float bw1 = a.width  * 0.55f, bw2 = a.width  * 0.35f;
        float bh1 = a.height * 0.42f, bh2 = a.height * 0.28f;
        var   c1  = FigmaStyleKit.ColAccent;
        var   c2  = new Color(0.4f, 0.5f, 0.7f, 0.7f);
        var   lc  = new Color(0.55f, 0.55f, 0.65f, 1f);

        switch (dir)
        {
            case AlignDir.Left:
                EditorGUI.DrawRect(new Rect(a.x, a.y, 2, a.height), lc);
                EditorGUI.DrawRect(new Rect(a.x + 3, cy - bh1 - 2, bw1, bh1), c1);
                EditorGUI.DrawRect(new Rect(a.x + 3, cy + 2,        bw2, bh2), c2);
                break;
            case AlignDir.CenterH:
                EditorGUI.DrawRect(new Rect(cx - 1, a.y, 2, a.height), lc);
                EditorGUI.DrawRect(new Rect(cx - bw1*0.5f, cy - bh1 - 2, bw1, bh1), c1);
                EditorGUI.DrawRect(new Rect(cx - bw2*0.5f, cy + 2,        bw2, bh2), c2);
                break;
            case AlignDir.Right:
                EditorGUI.DrawRect(new Rect(a.xMax - 2, a.y, 2, a.height), lc);
                EditorGUI.DrawRect(new Rect(a.xMax - bw1 - 3, cy - bh1 - 2, bw1, bh1), c1);
                EditorGUI.DrawRect(new Rect(a.xMax - bw2 - 3, cy + 2,        bw2, bh2), c2);
                break;
            case AlignDir.Top:
                EditorGUI.DrawRect(new Rect(a.x, a.y, a.width, 2), lc);
                EditorGUI.DrawRect(new Rect(cx - bw1*0.5f - 3, a.y + 3, bh1, bh1), c1);
                EditorGUI.DrawRect(new Rect(cx + 4,             a.y + 3, bh2, bh2), c2);
                break;
            case AlignDir.CenterV:
                EditorGUI.DrawRect(new Rect(a.x, cy - 1, a.width, 2), lc);
                EditorGUI.DrawRect(new Rect(cx - bw1*0.5f - 3, cy - bh1*0.5f, bh1, bh1), c1);
                EditorGUI.DrawRect(new Rect(cx + 4,             cy - bh2*0.5f, bh2, bh2), c2);
                break;
            case AlignDir.Bottom:
                EditorGUI.DrawRect(new Rect(a.x, a.yMax - 2, a.width, 2), lc);
                EditorGUI.DrawRect(new Rect(cx - bw1*0.5f - 3, a.yMax - bh1 - 3, bh1, bh1), c1);
                EditorGUI.DrawRect(new Rect(cx + 4,             a.yMax - bh2 - 3, bh2, bh2), c2);
                break;
        }
    }

    private void Align(AlignDir dir)
    {
        if (_s.MultiTargets.Count < 1) return;

        foreach (var rt in _s.MultiTargets)
        {
            Undo.RecordObject(rt, "Align");

            var parent = rt.parent as RectTransform;
            if (parent == null) continue;

            var   ps = parent.rect.size;
            float w  = rt.rect.width;
            float h  = rt.rect.height;

            // Читаем текущие figma-значения этого объекта
            var fv = new FigmaValues(rt, parent);

            switch (dir)
            {
                // Выравнивание по левому краю родителя
                case AlignDir.Left:
                    fv.L = 0;
                    fv.R = Mathf.Max(0, ps.x - w);
                    break;

                // Выравнивание по правому краю родителя
                case AlignDir.Right:
                    fv.R = 0;
                    fv.L = Mathf.Max(0, ps.x - w);
                    break;

                // Центрирование по горизонтали в родителе
                case AlignDir.CenterH:
                    fv.L = (ps.x - w) * 0.5f;
                    fv.R = fv.L;
                    break;

                // Выравнивание по верхнему краю родителя
                // T = отступ сверху, B = отступ снизу
                case AlignDir.Top:
                    fv.T = 0;
                    fv.B = Mathf.Max(0, ps.y - h);
                    break;

                // Выравнивание по нижнему краю родителя
                case AlignDir.Bottom:
                    fv.B = 0;
                    fv.T = Mathf.Max(0, ps.y - h);
                    break;

                // Центрирование по вертикали в родителе
                case AlignDir.CenterV:
                    fv.T = (ps.y - h) * 0.5f;
                    fv.B = fv.T;
                    break;
            }

            fv.Apply(rt, parent);
            EditorUtility.SetDirty(rt);
        }

        _sync.Calculate();
    }

    private void FillParent(float l, float r, float t, float b)
    {
        if (_s.Target == null || _s.Parent == null) return;
        Undo.RecordObject(_s.Target, "Fill Parent");
        var ps = _s.Parent.rect.size;
        _s.L = l; _s.R = r; _s.T = t; _s.B = b;
        _s.W = ps.x - l - r;
        _s.H = ps.y - t - b;
        _sync.Apply(true);
    }

    private void Distribute(bool horizontal)
    {
        var targets = _s.MultiTargets;
        if (targets.Count < 3) return;

        if (horizontal)
        {
            targets.Sort((a, b) =>
                new FigmaValues(a, a.parent as RectTransform).L
                .CompareTo(new FigmaValues(b, b.parent as RectTransform).L));

            var   first = new FigmaValues(targets[0],  targets[0].parent  as RectTransform);
            var   last  = new FigmaValues(targets[^1], targets[^1].parent as RectTransform);
            float total = (last.L + last.W) - first.L;
            float sumW  = 0; foreach (var rt in targets) sumW += rt.rect.width;
            float gap   = (total - sumW) / (targets.Count - 1);
            float cur   = first.L;

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Distribute H");
                var fv = new FigmaValues(rt, rt.parent as RectTransform);
                var ps = (rt.parent as RectTransform)!.rect.size;
                fv.L = cur;
                fv.R = Mathf.Max(0, ps.x - cur - rt.rect.width);
                fv.Apply(rt, rt.parent as RectTransform);
                EditorUtility.SetDirty(rt);
                cur += rt.rect.width + gap;
            }
        }
        else
        {
            targets.Sort((a, b) =>
                new FigmaValues(a, a.parent as RectTransform).T
                .CompareTo(new FigmaValues(b, b.parent as RectTransform).T));

            var   first = new FigmaValues(targets[0],  targets[0].parent  as RectTransform);
            var   last  = new FigmaValues(targets[^1], targets[^1].parent as RectTransform);
            float total = (last.T + last.H) - first.T;
            float sumH  = 0; foreach (var rt in targets) sumH += rt.rect.height;
            float gap   = (total - sumH) / (targets.Count - 1);
            float cur   = first.T;

            foreach (var rt in targets)
            {
                Undo.RecordObject(rt, "Distribute V");
                var fv = new FigmaValues(rt, rt.parent as RectTransform);
                var ps = (rt.parent as RectTransform)!.rect.size;
                fv.T = cur;
                fv.B = Mathf.Max(0, ps.y - cur - rt.rect.height);
                fv.Apply(rt, rt.parent as RectTransform);
                EditorUtility.SetDirty(rt);
                cur += rt.rect.height + gap;
            }
        }

        _sync.Calculate();
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  SETTINGS UI
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaSettingsUI
{
    private readonly FigmaState    _s;
    private readonly FigmaStyleKit _sk;

    public FigmaSettingsUI(FigmaState s, FigmaStyleKit sk) { _s = s; _sk = sk; }

    public void Draw()
    {
        FigmaDrawUtils.SectionHeader("Visual Editor", 0);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            _s.LivePreview    = EditorGUILayout.Toggle("Live Preview (apply on drag)", _s.LivePreview);
            _s.ShowGrid       = EditorGUILayout.Toggle("Grid",                         _s.ShowGrid);
            _s.SnapToGrid     = EditorGUILayout.Toggle("Snap to Grid",                 _s.SnapToGrid);
            _s.GridSize       = EditorGUILayout.Slider("Grid Size",  _s.GridSize, 1, 64);
            _s.SnapToPixels   = EditorGUILayout.Toggle("Snap to Pixels",               _s.SnapToPixels);
            _s.ShowRulers     = EditorGUILayout.Toggle("Rulers",                       _s.ShowRulers);
            _s.ShowSpacing    = EditorGUILayout.Toggle("Spacing Arrows",               _s.ShowSpacing);
            _s.ShowDimensions = EditorGUILayout.Toggle("Dimensions",                   _s.ShowDimensions);
            _s.ShowCrosshair  = EditorGUILayout.Toggle("Crosshair",                    _s.ShowCrosshair);
            _s.ShowSafeZone   = EditorGUILayout.Toggle("Safe Zone",                    _s.ShowSafeZone);
            _s.MaintainAspect = EditorGUILayout.Toggle("Maintain Aspect",              _s.MaintainAspect);
        }

        EditorGUILayout.Space(6);
        FigmaDrawUtils.SectionHeader("Colors", 0);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            _s.ContainerColor    = EditorGUILayout.ColorField("Container BG",  _s.ContainerColor);
            _s.ObjectColor       = EditorGUILayout.ColorField("Object Fill",    _s.ObjectColor);
            _s.ObjectBorderColor = EditorGUILayout.ColorField("Object Border",  _s.ObjectBorderColor);
            _s.HandleColor       = EditorGUILayout.ColorField("Handle",         _s.HandleColor);
            _s.HandleHover       = EditorGUILayout.ColorField("Handle Hover",   _s.HandleHover);
            _s.HandleActive      = EditorGUILayout.ColorField("Handle Active",  _s.HandleActive);

            if (GUILayout.Button("Reset Colors", _sk.Btn))
                ResetColors();
        }

        EditorGUILayout.Space(6);
        FigmaDrawUtils.SectionHeader("Hints", 0);
        EditorGUILayout.LabelField("Zoom: scroll wheel внутри visual editor",       _sk.Mini);
        EditorGUILayout.LabelField("Pan: правая кнопка мыши внутри visual editor",  _sk.Mini);
        EditorGUILayout.LabelField("Move: стрелки клавиатуры (+Shift × 10)",        _sk.Mini);

        EditorGUILayout.Space(12);

        // ── Reset ALL ────────────────────────────────────────────────────
        var dangerStyle = new GUIStyle(_sk.Btn)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            normal    = { textColor  = new Color(1f, 0.4f, 0.4f, 1f),
                          background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.22f, 0.1f, 0.1f, 1f)) },
            hover     = { textColor  = Color.white,
                          background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.55f, 0.15f, 0.15f, 1f)) },
            active    = { textColor  = Color.white,
                          background = FigmaDrawUtils.MakeTex(2, 2, new Color(0.7f, 0.2f, 0.2f, 1f)) },
        };

        if (GUILayout.Button("⚠  Reset ALL Settings to Default", dangerStyle, GUILayout.Height(34)))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Reset Settings",
                "Сбросить все настройки плагина к значениям по умолчанию?",
                "Сбросить", "Отмена");

            if (confirm) ResetAll();
        }
    }

    private void ResetColors()
    {
        _s.ContainerColor    = new Color(0.11f, 0.11f, 0.13f, 1f);
        _s.ObjectColor       = new Color(0.25f, 0.55f, 0.95f, 0.25f);
        _s.ObjectBorderColor = new Color(0.35f, 0.65f, 1f,    1f);
        _s.HandleColor       = new Color(0.92f, 0.92f, 0.95f, 1f);
        _s.HandleHover       = new Color(1f,    0.8f,  0.15f, 1f);
        _s.HandleActive      = new Color(0.2f,  0.92f, 0.45f, 1f);
    }

    private void ResetAll()
    {
        _s.LivePreview      = true;
        _s.ShowGrid         = true;
        _s.SnapToGrid       = false;
        _s.GridSize         = 8f;
        _s.SnapToPixels     = true;
        _s.ShowRulers       = true;
        _s.ShowSpacing      = true;
        _s.ShowDimensions   = true;
        _s.ShowCrosshair    = true;
        _s.ShowSafeZone     = false;
        _s.MaintainAspect   = false;
        _s.VisualHeight     = 280f;
        _s.Zoom             = 1f;
        _s.PanOffset        = Vector2.zero;
        _s.ShowVisualEditor = true;
        ResetColors();
    }
}

    // ═══════════════════════════════════════════════════════════════════════
    //  STYLE KIT  (все стили и цвета)
    // ═══════════════════════════════════════════════════════════════════════
    public class FigmaStyleKit
    {
        // Palette
        public static readonly Color ColBg       = new(0.11f, 0.11f, 0.13f, 1f);
        public static readonly Color ColPanel    = new(0.16f, 0.16f, 0.19f, 1f);
        public static readonly Color ColBorder   = new(0.28f, 0.28f, 0.32f, 1f);
        public static readonly Color ColAccent   = new(0.28f, 0.56f, 1f,    1f);
        public static readonly Color ColText     = new(0.88f, 0.88f, 0.92f, 1f);
        public static readonly Color ColTextDim  = new(0.52f, 0.52f, 0.57f, 1f);
        public static readonly Color ColRuler    = new(0.20f, 0.20f, 0.23f, 1f);
        public static readonly Color ColRulerText= new(0.48f, 0.48f, 0.53f, 1f);

        // Styles
        public GUIStyle Title, Mini, Tab, TabActive, Btn, Label, Section;
        private bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColText }
            };
            Mini = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 9, normal = { textColor = ColTextDim }
            };
            Tab = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                normal   = { textColor = ColTextDim, background = FigmaDrawUtils.MakeTex(2,2,ColPanel) },
                hover    = { textColor = ColText,    background = FigmaDrawUtils.MakeTex(2,2,new Color(0.22f,0.22f,0.26f)) },
                active   = { textColor = ColText,    background = FigmaDrawUtils.MakeTex(2,2,ColBorder) },
                border   = new RectOffset(1,1,1,1), margin = new RectOffset(0,0,0,0)
            };
            TabActive = new GUIStyle(Tab)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = FigmaDrawUtils.MakeTex(2,2,ColAccent) },
                hover  = { textColor = Color.white, background = FigmaDrawUtils.MakeTex(2,2,new Color(0.4f,0.65f,1f)) }
            };
            Btn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                normal   = { textColor = ColText,    background = FigmaDrawUtils.MakeTex(2,2,ColPanel) },
                hover    = { textColor = Color.white, background = FigmaDrawUtils.MakeTex(2,2,ColBorder) },
                active   = { textColor = Color.white, background = FigmaDrawUtils.MakeTex(2,2,ColAccent) },
                border   = new RectOffset(1,1,1,1)
            };
            Label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9, normal = { textColor = ColTextDim }
            };
            Section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = ColText }
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRAW UTILS  (статические хелперы рисования)
    // ═══════════════════════════════════════════════════════════════════════
    public static class FigmaDrawUtils
    {
        public static void DrawBorder(Rect r, Color c)
        {
            Handles.BeginGUI();
            Handles.color = c;
            HandlesRect(r);
            Handles.EndGUI();
        }

        public static void HandlesRect(Rect r)
        {
            Handles.DrawLine(new Vector3(r.x,    r.y,    0), new Vector3(r.xMax, r.y,    0));
            Handles.DrawLine(new Vector3(r.xMax, r.y,    0), new Vector3(r.xMax, r.yMax, 0));
            Handles.DrawLine(new Vector3(r.xMax, r.yMax, 0), new Vector3(r.x,    r.yMax, 0));
            Handles.DrawLine(new Vector3(r.x,    r.yMax, 0), new Vector3(r.x,    r.y,    0));
        }

        public static void DrawDashedRect(Rect r, Color c, float dashSize = 4f)
        {
            Handles.color = c;
            var p1 = new Vector3(r.x, r.y, 0);
            var p2 = new Vector3(r.xMax, r.y, 0);
            var p3 = new Vector3(r.xMax, r.yMax, 0);
            var p4 = new Vector3(r.x, r.yMax, 0);
            Handles.DrawDottedLine(p1, p2, dashSize);
            Handles.DrawDottedLine(p2, p3, dashSize);
            Handles.DrawDottedLine(p3, p4, dashSize);
            Handles.DrawDottedLine(p4, p1, dashSize);
        }

        public static void SectionHeader(string title, float windowWidth)
        {
            var r = GUILayoutUtility.GetRect(Mathf.Max(0, windowWidth - 20), 24);
            r.x += 10; if (r.width < 1) r.width = 200;
            EditorGUI.DrawRect(r, FigmaStyleKit.ColPanel);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), FigmaStyleKit.ColBorder);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), FigmaStyleKit.ColAccent);
            GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 10, 16), title,
                new GUIStyle(GUI.skin.label)
                    { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = FigmaStyleKit.ColText } });
        }

        public static Texture2D MakeTex(int w, int h, Color c)
        {
            var t = new Texture2D(w, h);
            var p = new Color[w * h];
            for (int i = 0; i < p.Length; i++) p[i] = c;
            t.SetPixels(p); t.Apply();
            return t;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FIGMA VALUES  (вспомогательная структура для multi-select)
    // ═══════════════════════════════════════════════════════════════════════
    public struct FigmaValues
    {
        public float L, R, T, B, W, H;

        public FigmaValues(RectTransform rt, RectTransform parent)
        {
            if (rt == null || parent == null) { L=R=T=B=W=H=0; return; }

            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            Vector2 localMin = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 localMax = parent.InverseTransformPoint(worldCorners[2]);

            var ps = parent.rect.size;
            W = localMax.x - localMin.x;
            H = localMax.y - localMin.y;

            float leftEdge = -parent.pivot.x * ps.x;
            float topEdge  = (1f - parent.pivot.y) * ps.y;

            L = localMin.x - leftEdge;
            R = ps.x - W - L;
            T = topEdge - localMax.y;
            B = ps.y - H - T;
        }

        public void Apply(RectTransform rt, RectTransform parent)
        {
            if (rt == null || parent == null) return;

            bool isStretchedH = rt.anchorMin.x != rt.anchorMax.x;
            bool isStretchedV = rt.anchorMin.y != rt.anchorMax.y;
            if (isStretchedH || isStretchedV)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
            }

            var ps = parent.rect.size;
            float leftEdge = -parent.pivot.x * ps.x;
            float topEdge  = (1f - parent.pivot.y) * ps.y;

            float minX = leftEdge + L;
            float maxX = minX + W;
            float maxY = topEdge - T;
            float minY = maxY - H;

            Vector2 parentLocalCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            Vector3 worldCenter = parent.TransformPoint(parentLocalCenter);

            var dp = rt.parent as RectTransform;
            Vector2 dpLocalCenter = dp.InverseTransformPoint(worldCenter);

            float dpAnchorX = (rt.anchorMin.x - dp.pivot.x) * dp.rect.size.x;
            float dpAnchorY = (rt.anchorMin.y - dp.pivot.y) * dp.rect.size.y;

            rt.anchoredPosition = new Vector2(dpLocalCenter.x - dpAnchorX, dpLocalCenter.y - dpAnchorY);
            rt.sizeDelta = new Vector2(W, H);
        }
    }

}
