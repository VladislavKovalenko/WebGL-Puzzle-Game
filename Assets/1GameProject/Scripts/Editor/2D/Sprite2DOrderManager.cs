using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace _1GameProject.Scripts.Editor._2D
{
    /// <summary>
    /// Менеджер порядка отрисовки 2D объектов сцены.
    /// Base: Sorting Layer, Order in Layer, Rendering Layer Mask, Enable/Disable.
    /// Advanced: Статистика, конфликты пересечений, визуализация, цветовые инструменты, авто-сортировка.
    /// </summary>
    public class Sprite2DOrderManager : EditorWindow
    {
        #region Enums & Constants

        private enum Tab { Base, Advanced }
        private enum SortMode { Name, SortingLayer, OrderInLayer, Type }
        private enum FilterMode { All, Selected, Enabled, Disabled }

        private const float RowHeight   = 22f;
        private const string PrefPrefix = "Sprite2DOrderManager_";

        #endregion

        #region Inner Types

        private class SpriteEntry
        {
            public GameObject   Go;
            public SpriteRenderer Sr;
            public bool         IsSelected;
            public bool         IsEnabled;

            private string _snapshotSortingLayer;
            private int    _snapshotOrder;
            private uint   _snapshotRenderingMask;
            private bool   _snapshotEnabled;

            public bool HasExternalChanges =>
                _snapshotSortingLayer  != Sr.sortingLayerName    ||
                _snapshotOrder         != Sr.sortingOrder         ||
                _snapshotRenderingMask != Sr.renderingLayerMask   ||
                _snapshotEnabled       != Go.activeSelf;

            public void TakeSnapshot()
            {
                _snapshotSortingLayer  = Sr.sortingLayerName;
                _snapshotOrder         = Sr.sortingOrder;
                _snapshotRenderingMask = Sr.renderingLayerMask;
                _snapshotEnabled       = Go.activeSelf;
            }

            /// <summary>Мировой AABB спрайта с учётом трансформации.</summary>
            public Bounds GetWorldBounds()
            {
                if (Sr.sprite == null) return new Bounds(Go.transform.position, Vector3.zero);
                return Sr.bounds;
            }
        }

        #endregion

        #region Fields

        private Tab _activeTab = Tab.Base;

        private readonly List<SpriteEntry> _entries  = new();
        private          List<SpriteEntry> _filtered = new();

        private Vector2 _scrollBase;
        private Vector2 _scrollAdvanced;
        private double  _lastRefreshTime;
        private const double RefreshInterval = 0.5;

        // Search & Filter
        private string     _searchQuery   = "";
        private SortMode   _sortMode      = SortMode.SortingLayer;
        private FilterMode _filterMode    = FilterMode.All;
        private bool       _sortAscending = true;
        private string     _layerFilter   = "All";

        // Batch edit
        private bool   _showBatchPanel;
        private string _batchSortingLayer = "Default";
        private int    _batchOrderOffset;
        private Color  _batchColor      = Color.white;
        private bool   _batchApplyColor;
        private bool   _batchApplyLayer;
        private bool   _batchApplyOrder;

        // Advanced panels
        private bool _showStats              = true;
        private bool _showConflicts          = true;
        private bool _showColorTools         = true;
        private bool _showOrderVisualization = true;

        // Order visualization
        private float   _vizZoom   = 1f;
        private Vector2 _vizScroll;

        // Conflict detection — только реальные пересечения
        private List<(SpriteEntry A, SpriteEntry B)> _conflicts = new();

        // Color tools
        private Gradient _gradientPreset  = new();
        private bool     _gradientByOrder = true;

        // Selection tracking
        private int _lastClickedIndex = -1;

        // Styles
        private GUIStyle _rowStyle;
        private GUIStyle _rowAltStyle;
        private GUIStyle _rowSelectedStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _conflictStyle;
        private bool     _stylesReady;

        // Icons
        private Texture2D _warnIcon;

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Megxlord Toolbox/2D/Sprite2D Order Manager", priority = 101)]
        public static void ShowWindow() => GetWindow<Sprite2DOrderManager>("Sprite2D Order Manager");

        private void OnEnable()
        {
            wantsMouseMove = true;
            EditorApplication.hierarchyChanged       += OnHierarchyChanged;
            EditorApplication.update                 += OnEditorUpdate;
            Selection.selectionChanged               += OnSelectionChanged;

            LoadPrefs();
            RefreshEntries();
            CacheIcons();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged       -= OnHierarchyChanged;
            EditorApplication.update                 -= OnEditorUpdate;
            Selection.selectionChanged               -= OnSelectionChanged;

            SavePrefs();
        }

        private void OnHierarchyChanged() => ScheduleRefresh();
        private void OnSelectionChanged()  => SyncSelectionFromEditor();

        private bool _needsRefresh;
        private void ScheduleRefresh() => _needsRefresh = true;

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_needsRefresh || now - _lastRefreshTime > RefreshInterval)
            {
                _needsRefresh    = false;
                _lastRefreshTime = now;
                RefreshEntries();
                DetectConflicts();
                Repaint();
                return;
            }

            // Внешние изменения (Inspector, другие окна)
            bool changed = false;
            foreach (var e in _entries)
            {
                if (e.Sr == null || !e.HasExternalChanges) continue;
                e.IsEnabled = e.Go.activeSelf;
                e.TakeSnapshot();
                changed = true;
            }

            if (changed)
            {
                DetectConflicts();
                Repaint();
            }
        }

        private void CacheIcons()
        {
            _warnIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
        }

        #endregion

        #region Data Management

        private void RefreshEntries()
        {
            var allSr    = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var existing = new HashSet<SpriteRenderer>(_entries.Select(e => e.Sr));
            var found    = new HashSet<SpriteRenderer>(allSr);

            _entries.RemoveAll(e => e.Sr == null || !found.Contains(e.Sr));

            foreach (var sr in allSr)
            {
                if (existing.Contains(sr)) continue;

                var entry = new SpriteEntry
                {
                    Go        = sr.gameObject,
                    Sr        = sr,
                    IsEnabled = sr.gameObject.activeSelf,
                };
                entry.TakeSnapshot();
                _entries.Add(entry);
            }

            foreach (var entry in _entries)
            {
                if (entry.Sr == null) continue;
                entry.Go        = entry.Sr.gameObject;
                entry.IsEnabled = entry.Go.activeSelf;
            }

            SyncSelectionFromEditor();
            ApplyFilterAndSort();
        }

        private void SyncSelectionFromEditor()
        {
            var sel = new HashSet<GameObject>(Selection.gameObjects);
            foreach (var e in _entries)
                e.IsSelected = sel.Contains(e.Go);
        }

        private void ApplyFilterAndSort()
        {
            IEnumerable<SpriteEntry> result = _entries.Where(e => e.Sr != null);

            result = _filterMode switch
            {
                FilterMode.Selected => result.Where(e => e.IsSelected),
                FilterMode.Enabled  => result.Where(e => e.IsEnabled),
                FilterMode.Disabled => result.Where(e => !e.IsEnabled),
                _                   => result
            };

            if (_layerFilter != "All")
                result = result.Where(e => e.Sr.sortingLayerName == _layerFilter);

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.ToLower();
                result = result.Where(e =>
                    e.Go.name.ToLower().Contains(q)                             ||
                    e.Sr.sortingLayerName.ToLower().Contains(q)                 ||
                    (e.Sr.sprite != null && e.Sr.sprite.name.ToLower().Contains(q)));
            }

            result = _sortMode switch
            {
                SortMode.Name => _sortAscending
                    ? result.OrderBy(e => e.Go.name)
                    : result.OrderByDescending(e => e.Go.name),

                SortMode.SortingLayer => _sortAscending
                    ? result.OrderBy(e => e.Sr.sortingLayerID).ThenBy(e => e.Sr.sortingOrder)
                    : result.OrderByDescending(e => e.Sr.sortingLayerID)
                            .ThenByDescending(e => e.Sr.sortingOrder),

                SortMode.OrderInLayer => _sortAscending
                    ? result.OrderBy(e => e.Sr.sortingOrder)
                    : result.OrderByDescending(e => e.Sr.sortingOrder),

                SortMode.Type => result.OrderBy(e => e.Sr.sprite != null ? e.Sr.sprite.name : ""),
                _             => result
            };

            _filtered = result.ToList();
        }

        /// <summary>
        /// Конфликт = одинаковый Sorting Layer + одинаковый Order + AABB пересекаются.
        /// Объекты на одном Order, но не перекрывающиеся — не конфликт.
        /// </summary>
        private void DetectConflicts()
        {
            _conflicts.Clear();

            // Группируем только по layer+order — кандидаты на конфликт
            var candidates = _entries
                .Where(e => e.Sr != null)
                .GroupBy(e => (e.Sr.sortingLayerName, e.Sr.sortingOrder))
                .Where(g => g.Count() > 1);

            foreach (var group in candidates)
            {
                var list = group.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        if (BoundsIntersect2D(list[i].GetWorldBounds(), list[j].GetWorldBounds()))
                            _conflicts.Add((list[i], list[j]));
                    }
                }
            }
        }

        /// <summary>Проверяем пересечение только по X и Y (игнорируем Z — это 2D).</summary>
        private static bool BoundsIntersect2D(Bounds a, Bounds b)
        {
            bool overlapX = a.min.x < b.max.x && a.max.x > b.min.x;
            bool overlapY = a.min.y < b.max.y && a.max.y > b.min.y;
            return overlapX && overlapY;
        }

        #endregion

        #region GUI Root

        private void OnGUI()
        {
            InitStyles();
            ApplyFilterAndSort();

            DrawToolbar();
            EditorGUILayout.Space(2);

            switch (_activeTab)
            {
                case Tab.Base:     DrawBaseTab();     break;
                case Tab.Advanced: DrawAdvancedTab(); break;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_activeTab == Tab.Base, "⊞  Base",
                    EditorStyles.toolbarButton, GUILayout.Width(80)))
                _activeTab = Tab.Base;

            if (GUILayout.Toggle(_activeTab == Tab.Advanced, "⚙  Advanced",
                    EditorStyles.toolbarButton, GUILayout.Width(90)))
                _activeTab = Tab.Advanced;

            GUILayout.FlexibleSpace();

            int selCount = _entries.Count(e => e.IsSelected);
            GUILayout.Label(
                $"Total: {_entries.Count}  |  Filtered: {_filtered.Count}  |  Selected: {selCount}",
                EditorStyles.miniLabel);

            GUILayout.Space(8);

            if (GUILayout.Button("↺", EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                RefreshEntries();
                DetectConflicts();
            }

            if (_conflicts.Count > 0)
            {
                GUI.color = new Color(1f, 0.7f, 0.2f);
                GUILayout.Label($"⚠ {_conflicts.Count}", EditorStyles.toolbarButton, GUILayout.Width(40));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Base Tab

        private void DrawBaseTab()
        {
            DrawSearchAndFilterBar();
            EditorGUILayout.Space(2);
            DrawColumnHeaders();
            DrawSpriteList();
            EditorGUILayout.Space(4);
            DrawBaseActionBar();
        }

        private void DrawSearchAndFilterBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string newQuery = EditorGUILayout.TextField(
                _searchQuery, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
            if (newQuery != _searchQuery) _searchQuery = newQuery;

            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchQuery = "";

            GUILayout.Space(6);

            _filterMode = (FilterMode)EditorGUILayout.EnumPopup(
                _filterMode, EditorStyles.toolbarPopup, GUILayout.Width(80));

            GUILayout.Space(6);

            string[] layerNames = new[] { "All" }
                .Concat(SortingLayer.layers.Select(l => l.name))
                .ToArray();

            int li    = System.Array.IndexOf(layerNames, _layerFilter);
            if (li < 0) li = 0;
            int newLi = EditorGUILayout.Popup(li, layerNames,
                EditorStyles.toolbarPopup, GUILayout.Width(80));
            _layerFilter = layerNames[newLi];

            GUILayout.Space(6);

            _sortMode = (SortMode)EditorGUILayout.EnumPopup(
                _sortMode, EditorStyles.toolbarPopup, GUILayout.Width(85));

            if (GUILayout.Button(_sortAscending ? "▲" : "▼",
                    EditorStyles.toolbarButton, GUILayout.Width(22)))
                _sortAscending = !_sortAscending;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(_headerStyle);
            // Колонка On/Off
            GUILayout.Label("On",           EditorStyles.boldLabel, GUILayout.Width(26));
            GUILayout.Label("Name",         EditorStyles.boldLabel, GUILayout.MinWidth(100));
            GUILayout.Label("Sorting Layer", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Order",        EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Render Mask",  EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Color",        EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Space(28);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpriteList()
        {
            _scrollBase = EditorGUILayout.BeginScrollView(_scrollBase);

            for (int i = 0; i < _filtered.Count; i++)
                DrawSpriteRow(_filtered[i], i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpriteRow(SpriteEntry entry, int index)
        {
            if (entry.Sr == null) return;

            bool isConflict = _conflicts.Any(c => c.A == entry || c.B == entry);

            GUIStyle rowBg = entry.IsSelected
                ? _rowSelectedStyle
                : isConflict
                    ? _conflictStyle
                    : index % 2 == 0 ? _rowStyle : _rowAltStyle;

            EditorGUILayout.BeginHorizontal(rowBg, GUILayout.Height(RowHeight));

            // ── On / Off галочка ──────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            bool newActive = EditorGUILayout.Toggle(entry.IsEnabled, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
                SetEntryEnabled(entry, newActive);

            // ── Имя объекта ───────────────────────────────────────────────────
            var nameStyle = new GUIStyle(EditorStyles.label);
            if (!entry.IsEnabled) nameStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            if (isConflict)       nameStyle.normal.textColor = new Color(1f, 0.6f, 0.1f);

            if (GUILayout.Button(entry.Go.name, nameStyle, GUILayout.MinWidth(100)))
                HandleRowClick(entry, index);

            // ── Sorting Layer ─────────────────────────────────────────────────
            string[] layerNames  = SortingLayer.layers.Select(l => l.name).ToArray();
            int      curLayerIdx = System.Array.IndexOf(layerNames, entry.Sr.sortingLayerName);
            if (curLayerIdx < 0) curLayerIdx = 0;

            EditorGUI.BeginChangeCheck();
            int newLayerIdx = EditorGUILayout.Popup(curLayerIdx, layerNames, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(entry.Sr, "Change Sorting Layer");
                entry.Sr.sortingLayerName = layerNames[newLayerIdx];
                entry.TakeSnapshot();
                EditorUtility.SetDirty(entry.Sr);
                DetectConflicts();
            }

            // ── Order in Layer ────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            int newOrder = EditorGUILayout.IntField(entry.Sr.sortingOrder, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(entry.Sr, "Change Order in Layer");
                entry.Sr.sortingOrder = newOrder;
                entry.TakeSnapshot();
                EditorUtility.SetDirty(entry.Sr);
                DetectConflicts();
            }

            // ── Rendering Layer Mask (через SerializedObject) ─────────────────
            var so   = new SerializedObject(entry.Sr);
            var prop = so.FindProperty("m_RenderingLayerMask");
            so.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(90));
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                entry.TakeSnapshot();
            }

            // ── Color ─────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.ColorField(
                GUIContent.none, entry.Sr.color,
                false, true, false,
                GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(entry.Sr, "Change Sprite Color");
                entry.Sr.color = newColor;
                EditorUtility.SetDirty(entry.Sr);
            }

            // ── Иконка конфликта ──────────────────────────────────────────────
            if (isConflict && _warnIcon != null)
                GUILayout.Label(new GUIContent(_warnIcon, "Sprites overlap with same order!"),
                    GUILayout.Width(18));
            else
                GUILayout.Space(18);

            // ── Ping ──────────────────────────────────────────────────────────
            if (GUILayout.Button("→", GUILayout.Width(22), GUILayout.Height(RowHeight - 2)))
            {
                EditorGUIUtility.PingObject(entry.Go);
                Selection.activeGameObject = entry.Go;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Включение / выключение объекта ───────────────────────────────────────

        private static void SetEntryEnabled(SpriteEntry entry, bool enabled)
        {
            Undo.RecordObject(entry.Go, enabled ? "Enable GameObject" : "Disable GameObject");
            entry.Go.SetActive(enabled);
            entry.IsEnabled = enabled;
            entry.TakeSnapshot();
            EditorUtility.SetDirty(entry.Go);
        }

        // ── Клики по строкам ──────────────────────────────────────────────────────

        private void HandleRowClick(SpriteEntry entry, int index)
        {
            Event e = Event.current;

            if (e.control || e.command)
            {
                entry.IsSelected = !entry.IsSelected;
                UpdateEditorSelection();
            }
            else if (e.shift && _lastClickedIndex >= 0)
            {
                int lo = Mathf.Min(index, _lastClickedIndex);
                int hi = Mathf.Max(index, _lastClickedIndex);
                for (int i = lo; i <= hi && i < _filtered.Count; i++)
                    _filtered[i].IsSelected = true;
                UpdateEditorSelection();
            }
            else
            {
                foreach (var en in _entries) en.IsSelected = false;
                entry.IsSelected = true;
                UpdateEditorSelection();
                EditorGUIUtility.PingObject(entry.Go);
            }

            _lastClickedIndex = index;
        }

        private void UpdateEditorSelection()
        {
            Selection.objects = _entries
                .Where(e => e.IsSelected && e.Go != null)
                .Select(e => (Object)e.Go)
                .ToArray();
        }

        // ── Нижняя панель Base ────────────────────────────────────────────────────

        private void DrawBaseActionBar()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Batch
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Batch Edit Selected:", EditorStyles.boldLabel);
            _showBatchPanel = EditorGUILayout.Foldout(_showBatchPanel, "", true);
            EditorGUILayout.EndHorizontal();

            if (_showBatchPanel) DrawBatchPanel();

            EditorGUILayout.Space(4);

            // Строка 1 — выделение
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All", GUILayout.Height(24)))
            {
                foreach (var e in _filtered) e.IsSelected = true;
                UpdateEditorSelection();
            }

            if (GUILayout.Button("Deselect All", GUILayout.Height(24)))
            {
                foreach (var e in _entries) e.IsSelected = false;
                UpdateEditorSelection();
            }

            if (GUILayout.Button("Invert Selection", GUILayout.Height(24)))
            {
                foreach (var e in _filtered) e.IsSelected = !e.IsSelected;
                UpdateEditorSelection();
            }

            EditorGUILayout.EndHorizontal();

            // Строка 2 — включение / выключение
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Enable All", GUILayout.Height(24)))
            {
                foreach (var e in _entries)
                    SetEntryEnabled(e, true);
            }

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("Disable Unselected", GUILayout.Height(24)))
            {
                foreach (var e in _entries.Where(en => !en.IsSelected))
                    SetEntryEnabled(e, false);
            }

            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.9f);
            if (GUILayout.Button("Toggle Selected", GUILayout.Height(24)))
            {
                foreach (var e in _entries.Where(en => en.IsSelected))
                    SetEntryEnabled(e, !e.IsEnabled);
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchPanel()
        {
            var selected = _filtered.Where(e => e.IsSelected).ToList();
            if (selected.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites selected. Click rows to select.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Applying to {selected.Count} sprite(s):", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);

            string[] layerNames = SortingLayer.layers.Select(l => l.name).ToArray();

            EditorGUILayout.BeginHorizontal();
            _batchApplyLayer = EditorGUILayout.Toggle(_batchApplyLayer, GUILayout.Width(16));
            int li = System.Array.IndexOf(layerNames, _batchSortingLayer);
            if (li < 0) li = 0;
            _batchSortingLayer = layerNames[EditorGUILayout.Popup("Sorting Layer", li, layerNames)];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _batchApplyOrder  = EditorGUILayout.Toggle(_batchApplyOrder, GUILayout.Width(16));
            _batchOrderOffset = EditorGUILayout.IntField("Order Offset", _batchOrderOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _batchApplyColor = EditorGUILayout.Toggle(_batchApplyColor, GUILayout.Width(16));
            _batchColor      = EditorGUILayout.ColorField("Color", _batchColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button($"▶  Apply to {selected.Count} sprite(s)", GUILayout.Height(28)))
            {
                foreach (var entry in selected)
                {
                    Undo.RecordObject(entry.Sr, "Batch Edit Sprites");

                    if (_batchApplyLayer) entry.Sr.sortingLayerName = _batchSortingLayer;
                    if (_batchApplyOrder) entry.Sr.sortingOrder    += _batchOrderOffset;
                    if (_batchApplyColor) entry.Sr.color            = _batchColor;

                    entry.TakeSnapshot();
                    EditorUtility.SetDirty(entry.Sr);
                }
                DetectConflicts();
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Advanced Tab

        private void DrawAdvancedTab()
        {
            _scrollAdvanced = EditorGUILayout.BeginScrollView(_scrollAdvanced);

            DrawStatsPanel();
            EditorGUILayout.Space(4);
            DrawConflictsPanel();
            EditorGUILayout.Space(4);
            DrawOrderVisualizationPanel();
            EditorGUILayout.Space(4);
            DrawColorToolsPanel();
            EditorGUILayout.Space(4);
            DrawAutoSortPanel();

            EditorGUILayout.EndScrollView();
        }

        // ── Stats ─────────────────────────────────────────────────────────────────

        private void DrawStatsPanel()
        {
            _showStats = DrawFoldout(_showStats, "📊  Scene Statistics");
            if (!_showStats) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            int total         = _entries.Count;
            int enabled       = _entries.Count(e => e.IsEnabled);
            int conflictCount = _conflicts.Count;

            var layerGroups = _entries
                .Where(e => e.Sr != null)
                .GroupBy(e => e.Sr.sortingLayerName)
                .OrderBy(g => SortingLayer.GetLayerValueFromName(g.Key));

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            DrawStatRow("Total sprites",      total.ToString(),             Color.white);
            DrawStatRow("Enabled",            enabled.ToString(),           new Color(0.4f, 1f,   0.4f));
            DrawStatRow("Disabled",           (total - enabled).ToString(), new Color(0.6f, 0.6f, 0.6f));
            DrawStatRow("Overlap Conflicts",  conflictCount.ToString(),
                conflictCount > 0 ? new Color(1f, 0.6f, 0.1f) : new Color(0.4f, 1f, 0.4f));
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Per Layer:", EditorStyles.boldLabel);
            foreach (var g in layerGroups)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {g.Key}", GUILayout.Width(100));
                DrawLayerBar(g.Count(), total);
                EditorGUILayout.LabelField(g.Count().ToString(), GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawStatRow(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            GUI.color = valueColor;
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(50));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLayerBar(int count, int total)
        {
            Rect  r    = GUILayoutUtility.GetRect(80, 14);
            float frac = total > 0 ? count / (float)total : 0;
            EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * frac, r.height), new Color(0.3f, 0.6f, 1f));
        }

        // ── Conflicts ─────────────────────────────────────────────────────────────

        private void DrawConflictsPanel()
        {
            _showConflicts = DrawFoldout(_showConflicts, $"⚠  Overlap Conflicts  ({_conflicts.Count})");
            if (!_showConflicts) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (_conflicts.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No overlap conflicts detected.\n" +
                    "Objects sharing the same Order but not overlapping are fine.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "These sprites share the same Sorting Layer + Order AND their bounds overlap.\n" +
                    "Rendering order between them is undefined.",
                    MessageType.Warning);

                EditorGUILayout.Space(4);

                int shown = Mathf.Min(_conflicts.Count, 10);
                for (int i = 0; i < shown; i++)
                {
                    var (a, b) = _conflicts[i];
                    EditorGUILayout.BeginHorizontal(_conflictStyle);
                    EditorGUILayout.LabelField(
                        $"{a.Go.name}  ↔  {b.Go.name}  [{a.Sr.sortingLayerName} / {a.Sr.sortingOrder}]",
                        EditorStyles.miniLabel);

                    if (GUILayout.Button("Ping A", GUILayout.Width(46), GUILayout.Height(18)))
                        EditorGUIUtility.PingObject(a.Go);

                    if (GUILayout.Button("Ping B", GUILayout.Width(46), GUILayout.Height(18)))
                        EditorGUIUtility.PingObject(b.Go);

                    if (GUILayout.Button("Fix", GUILayout.Width(32), GUILayout.Height(18)))
                        FixConflict(a, b);

                    EditorGUILayout.EndHorizontal();
                }

                if (_conflicts.Count > shown)
                    EditorGUILayout.LabelField($"... and {_conflicts.Count - shown} more",
                        EditorStyles.miniLabel);

                EditorGUILayout.Space(4);

                GUI.backgroundColor = new Color(1f, 0.7f, 0.2f);
                if (GUILayout.Button("Auto-Fix All Overlap Conflicts", GUILayout.Height(26)))
                    AutoFixAllConflicts();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void FixConflict(SpriteEntry a, SpriteEntry b)
        {
            Undo.RecordObject(b.Sr, "Fix Order Conflict");
            b.Sr.sortingOrder = a.Sr.sortingOrder + 1;
            b.TakeSnapshot();
            EditorUtility.SetDirty(b.Sr);
            DetectConflicts();
        }

        private void AutoFixAllConflicts()
        {
            // Обходим только группы с реальными пересечениями
            var processed = new HashSet<SpriteEntry>();

            foreach (var (a, b) in _conflicts.ToList())
            {
                if (processed.Contains(b)) continue;
                Undo.RecordObject(b.Sr, "Auto-Fix Overlap Conflicts");
                b.Sr.sortingOrder = a.Sr.sortingOrder + 1;
                b.TakeSnapshot();
                EditorUtility.SetDirty(b.Sr);
                processed.Add(b);
            }

            DetectConflicts();
        }

        // ── Order Visualization ───────────────────────────────────────────────────

        private void DrawOrderVisualizationPanel()
        {
            _showOrderVisualization = DrawFoldout(_showOrderVisualization, "📈  Order Visualization");
            if (!_showOrderVisualization) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zoom:", GUILayout.Width(40));
            _vizZoom = GUILayout.HorizontalSlider(_vizZoom, 0.3f, 3f, GUILayout.Width(120));
            EditorGUILayout.LabelField($"{_vizZoom:F1}x", GUILayout.Width(35));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            var layerGroups = _entries
                .Where(e => e.Sr != null)
                .GroupBy(e => e.Sr.sortingLayerName)
                .OrderBy(g => SortingLayer.GetLayerValueFromName(g.Key))
                .ToList();

            _vizScroll = EditorGUILayout.BeginScrollView(_vizScroll,
                GUILayout.Height(Mathf.Clamp(layerGroups.Count * 50 * _vizZoom + 20, 80, 300)));

            foreach (var group in layerGroups)
            {
                var items = group.OrderBy(e => e.Sr.sortingOrder).ToList();
                if (items.Count == 0) continue;

                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                Rect lineRect = GUILayoutUtility.GetRect(
                    position.width - 20,
                    Mathf.Max(28 * _vizZoom, 28));

                EditorGUI.DrawRect(lineRect, new Color(0.18f, 0.18f, 0.18f));

                int minOrder = items.Min(e => e.Sr.sortingOrder);
                int maxOrder = items.Max(e => e.Sr.sortingOrder);
                int range    = Mathf.Max(maxOrder - minOrder, 1);

                for (int i = 0; i < items.Count; i++)
                {
                    var   entry = items[i];
                    bool  hasConflict = _conflicts.Any(c => c.A == entry || c.B == entry);
                    float t     = (entry.Sr.sortingOrder - minOrder) / (float)range;
                    float x     = lineRect.x + 10 + t * (lineRect.width - 80);
                    float y     = lineRect.y + lineRect.height * 0.5f;

                    Color nodeColor = hasConflict
                        ? new Color(1f, 0.4f, 0.1f)
                        : entry.IsSelected
                            ? new Color(0.2f, 0.8f, 1f)
                            : new Color(0.3f + t * 0.5f, 0.6f, 0.9f - t * 0.3f);

                    if (!entry.IsEnabled)
                        nodeColor = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.4f);

                    float nodeSize = 10 * _vizZoom;

                    Handles.BeginGUI();
                    Handles.color = nodeColor;
                    Handles.DrawSolidDisc(new Vector3(x, y, 0), Vector3.forward, nodeSize);
                    if (entry.IsSelected)
                    {
                        Handles.color = Color.white;
                        Handles.DrawWireDisc(new Vector3(x, y, 0), Vector3.forward, nodeSize + 2);
                    }
                    Handles.EndGUI();

                    float lblW = 60 * _vizZoom;
                    GUI.Label(
                        new Rect(x - lblW * 0.5f, y - nodeSize - 14, lblW, 14),
                        $"{entry.Go.name}\n({entry.Sr.sortingOrder})",
                        EditorStyles.centeredGreyMiniLabel);

                    if (Event.current.type == EventType.MouseDown)
                    {
                        float d = Vector2.Distance(Event.current.mousePosition, new Vector2(x, y));
                        if (d < nodeSize + 4)
                        {
                            HandleRowClick(entry, _filtered.IndexOf(entry));
                            Event.current.Use();
                        }
                    }
                }

                GUI.Label(new Rect(lineRect.x + 2,     lineRect.y + 2, 40, 14),
                    minOrder.ToString(), EditorStyles.miniLabel);
                GUI.Label(new Rect(lineRect.xMax - 40, lineRect.y + 2, 40, 14),
                    maxOrder.ToString(), EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Color Tools ───────────────────────────────────────────────────────────

        private void DrawColorToolsPanel()
        {
            _showColorTools = DrawFoldout(_showColorTools, "🎨  Color Tools");
            if (!_showColorTools) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("Apply Gradient to Selection", EditorStyles.boldLabel);
            _gradientPreset  = EditorGUILayout.GradientField("Gradient", _gradientPreset);
            _gradientByOrder = EditorGUILayout.Toggle("Map by Order in Layer", _gradientByOrder);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Gradient", GUILayout.Height(24))) ApplyGradientToSelected();
            if (GUILayout.Button("Reset Colors",   GUILayout.Height(24))) ResetColorsToWhite();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Bulk Transparency", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("25%",  GUILayout.Height(22))) SetAlphaToSelected(0.25f);
            if (GUILayout.Button("50%",  GUILayout.Height(22))) SetAlphaToSelected(0.5f);
            if (GUILayout.Button("75%",  GUILayout.Height(22))) SetAlphaToSelected(0.75f);
            if (GUILayout.Button("100%", GUILayout.Height(22))) SetAlphaToSelected(1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Debug Colors", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize",     GUILayout.Height(22))) ApplyRandomColors();
            if (GUILayout.Button("by Layer",      GUILayout.Height(22))) ApplyColorByLayer();
            if (GUILayout.Button("by Order",      GUILayout.Height(22))) ApplyColorByOrder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyGradientToSelected()
        {
            var targets = _filtered.Where(e => e.IsSelected && e.Sr != null).ToList();
            if (targets.Count == 0) { ShowNotification(new GUIContent("No sprites selected!")); return; }

            var ordered = _gradientByOrder
                ? targets.OrderBy(e => e.Sr.sortingOrder).ToList()
                : targets;

            for (int i = 0; i < ordered.Count; i++)
            {
                float t = ordered.Count > 1 ? i / (float)(ordered.Count - 1) : 0f;
                Undo.RecordObject(ordered[i].Sr, "Apply Gradient");
                ordered[i].Sr.color = _gradientPreset.Evaluate(t);
                EditorUtility.SetDirty(ordered[i].Sr);
            }
        }

        private void ResetColorsToWhite()
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                Undo.RecordObject(e.Sr, "Reset Color");
                e.Sr.color = Color.white;
                EditorUtility.SetDirty(e.Sr);
            }
        }

        private void SetAlphaToSelected(float alpha)
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                Undo.RecordObject(e.Sr, "Set Alpha");
                Color c = e.Sr.color;
                c.a        = alpha;
                e.Sr.color = c;
                EditorUtility.SetDirty(e.Sr);
            }
        }

        private void ApplyRandomColors()
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                Undo.RecordObject(e.Sr, "Random Color");
                e.Sr.color = new Color(Random.value, Random.value, Random.value, 1f);
                EditorUtility.SetDirty(e.Sr);
            }
        }

        private void ApplyColorByLayer()
        {
            SortingLayer[] layers  = SortingLayer.layers;
            Color[]        palette = GeneratePalette(layers.Length);

            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                int idx = System.Array.FindIndex(layers, l => l.name == e.Sr.sortingLayerName);
                if (idx < 0) continue;
                Undo.RecordObject(e.Sr, "Color by Layer");
                e.Sr.color = palette[idx % palette.Length];
                EditorUtility.SetDirty(e.Sr);
            }
        }

        private void ApplyColorByOrder()
        {
            var targets = _filtered.Where(e => e.IsSelected && e.Sr != null).ToList();
            if (targets.Count == 0) return;

            int min   = targets.Min(e => e.Sr.sortingOrder);
            int max   = targets.Max(e => e.Sr.sortingOrder);
            int range = Mathf.Max(max - min, 1);

            foreach (var e in targets)
            {
                float t = (e.Sr.sortingOrder - min) / (float)range;
                Undo.RecordObject(e.Sr, "Color by Order");
                e.Sr.color = Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(1f, 0.3f, 0.3f), t);
                EditorUtility.SetDirty(e.Sr);
            }
        }

        private static Color[] GeneratePalette(int count)
        {
            var result = new Color[Mathf.Max(count, 1)];
            for (int i = 0; i < result.Length; i++)
                result[i] = Color.HSVToRGB(i / (float)result.Length, 0.7f, 0.9f);
            return result;
        }

        // ── Auto Sort ─────────────────────────────────────────────────────────────

        private void DrawAutoSortPanel()
        {
            DrawFoldout(true, "🔃  Auto Sort & Normalize");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.HelpBox(
                "Normalize Order: reassigns sequential integers (0, 1, 2...) " +
                "preserving relative order within each Sorting Layer.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Normalize ALL Layers",     GUILayout.Height(28))) NormalizeOrders(true);
            if (GUILayout.Button("Normalize Selected Layer", GUILayout.Height(28))) NormalizeOrders(false);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Quick Order Shift for Selected:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("–10",   GUILayout.Height(24))) ShiftOrderSelected(-10);
            if (GUILayout.Button("–1",    GUILayout.Height(24))) ShiftOrderSelected(-1);
            if (GUILayout.Button("+1",    GUILayout.Height(24))) ShiftOrderSelected(1);
            if (GUILayout.Button("+10",   GUILayout.Height(24))) ShiftOrderSelected(10);
            if (GUILayout.Button("Set 0", GUILayout.Height(24))) SetOrderSelected(0);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void NormalizeOrders(bool allEntries)
        {
            IEnumerable<SpriteEntry> source = allEntries
                ? _entries
                : _entries.Where(e => e.IsSelected);

            var groups = source
                .Where(e => e.Sr != null)
                .GroupBy(e => e.Sr.sortingLayerName);

            foreach (var g in groups)
            {
                var sorted = g.OrderBy(e => e.Sr.sortingOrder).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    Undo.RecordObject(sorted[i].Sr, "Normalize Order");
                    sorted[i].Sr.sortingOrder = i;
                    sorted[i].TakeSnapshot();
                    EditorUtility.SetDirty(sorted[i].Sr);
                }
            }

            DetectConflicts();
            ShowNotification(new GUIContent("Orders normalized!"));
        }

        private void ShiftOrderSelected(int delta)
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                Undo.RecordObject(e.Sr, "Shift Order");
                e.Sr.sortingOrder += delta;
                e.TakeSnapshot();
                EditorUtility.SetDirty(e.Sr);
            }
            DetectConflicts();
        }

        private void SetOrderSelected(int value)
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Sr != null))
            {
                Undo.RecordObject(e.Sr, "Set Order");
                e.Sr.sortingOrder = value;
                e.TakeSnapshot();
                EditorUtility.SetDirty(e.Sr);
            }
            DetectConflicts();
        }

        #endregion

        #region Styles & Prefs

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _rowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(2, 2, 1, 1),
                margin  = new RectOffset(0, 0, 0, 0),
                normal  = { background = MakeTexture(new Color(0.22f, 0.22f, 0.22f)) }
            };

            _rowAltStyle = new GUIStyle(_rowStyle)
            {
                normal = { background = MakeTexture(new Color(0.19f, 0.19f, 0.19f)) }
            };

            _rowSelectedStyle = new GUIStyle(_rowStyle)
            {
                normal = { background = MakeTexture(new Color(0.17f, 0.36f, 0.53f)) }
            };

            _headerStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(4, 4, 3, 3),
                normal  = { background = MakeTexture(new Color(0.15f, 0.15f, 0.15f)) }
            };

            _conflictStyle = new GUIStyle(_rowAltStyle)
            {
                normal = { background = MakeTexture(new Color(0.35f, 0.22f, 0.05f)) }
            };

            _sectionStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
            };
        }

        private bool DrawFoldout(bool state, string label) =>
            EditorGUILayout.Foldout(state, label, true,
                _sectionStyle ?? EditorStyles.foldoutHeader);

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void LoadPrefs()
        {
            _sortMode      = (SortMode)EditorPrefs.GetInt(PrefPrefix + "SortMode", 0);
            _sortAscending = EditorPrefs.GetBool(PrefPrefix + "SortAsc", true);
            _layerFilter   = EditorPrefs.GetString(PrefPrefix + "LayerFilter", "All");
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefPrefix + "SortMode",    (int)_sortMode);
            EditorPrefs.SetBool(PrefPrefix + "SortAsc",    _sortAscending);
            EditorPrefs.SetString(PrefPrefix + "LayerFilter", _layerFilter);
        }

        #endregion
    }

    // ── Toolbar ──────────────────────────────────────────────────────────────────

    [InitializeOnLoad]
    public static class Sprite2DOrderManagerToolbar
    {
        private const string ElementId = "MyTools/Sprite2DOrderManager";

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateButton() =>
            new MainToolbarButton(
                new MainToolbarContent("2D Order", tooltip: "Open Sprite2D Order Manager"),
                Sprite2DOrderManager.ShowWindow);
    }
}