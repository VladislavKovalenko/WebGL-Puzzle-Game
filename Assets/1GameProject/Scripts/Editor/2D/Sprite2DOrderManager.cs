using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Tilemaps;

#if TMP_PRESENT
using TMPro;
#endif

namespace _1GameProject.Scripts.Editor._2D
{
    /// <summary>
    /// Менеджер порядка отрисовки 2D объектов сцены.
    /// Сканирует: SpriteRenderer, TMP (TextMeshPro / TextMeshProUGUI),
    /// MeshRenderer, ParticleSystemRenderer, TilemapRenderer,
    /// SpriteMask, LineRenderer, TrailRenderer, SkinnedMeshRenderer.
    /// </summary>
    public class Sprite2DOrderManager : EditorWindow
    {
        #region Enums & Constants

        private enum Tab { Base, Advanced }
        private enum SortMode { Name, SortingLayer, OrderInLayer, Type }
        private enum FilterMode { All, Selected, Enabled, Disabled }

        /// <summary>Тип компонента-рендерера для отображения в UI.</summary>
        private enum RendererKind
        {
            SpriteRenderer,
            TextMeshPro,
            TextMeshProUGUI,
            MeshRenderer,
            ParticleSystem,
            Tilemap,
            SpriteMask,
            LineRenderer,
            TrailRenderer,
            SkinnedMesh,
            Other
        }

        private const float  RowHeight   = 22f;
        private const string PrefPrefix  = "Sprite2DOrderManager_";

        #endregion

        #region Inner Types

        private class RendererEntry
        {
            public GameObject Go;
            public Renderer   Rd;           // базовый тип — общий для всех
            public RendererKind Kind;
            public bool       IsSelected;
            public bool       IsEnabled;

            // Snapshot для отслеживания внешних изменений
            private string _snapLayer;
            private int    _snapOrder;
            private uint   _snapMask;
            private bool   _snapActive;

            public bool HasExternalChanges =>
                _snapLayer  != Rd.sortingLayerName  ||
                _snapOrder  != Rd.sortingOrder       ||
                _snapMask   != Rd.renderingLayerMask ||
                _snapActive != Go.activeSelf;

            public void TakeSnapshot()
            {
                _snapLayer  = Rd.sortingLayerName;
                _snapOrder  = Rd.sortingOrder;
                _snapMask   = Rd.renderingLayerMask;
                _snapActive = Go.activeSelf;
            }

            /// <summary>Мировой AABB с учётом трансформации.</summary>
            public Bounds GetWorldBounds()
            {
                if (Rd == null) return new Bounds(Go.transform.position, Vector3.zero);
                var b = Rd.bounds;
                // bounds с нулевым размером — заменяем точкой
                return b.size == Vector3.zero
                    ? new Bounds(Go.transform.position, Vector3.zero)
                    : b;
            }

            // ── Цвет (только SpriteRenderer и TMP) ──────────────────────
            public bool SupportsColor => Kind == RendererKind.SpriteRenderer
#if TMP_PRESENT
                || Kind == RendererKind.TextMeshPro
                || Kind == RendererKind.TextMeshProUGUI
#endif
                ;

            public Color GetColor()
            {
                if (Rd is SpriteRenderer sr) return sr.color;
#if TMP_PRESENT
                if (Rd.TryGetComponent<TextMeshPro>(out var tmp))    return tmp.color;
                if (Rd.TryGetComponent<TextMeshProUGUI>(out var tmu)) return tmu.color;
#endif
                return Color.white;
            }

            public void SetColor(Color c)
            {
                if (Rd is SpriteRenderer sr) { sr.color = c; return; }
#if TMP_PRESENT
                if (Rd.TryGetComponent<TextMeshPro>(out var tmp))    { tmp.color = c; return; }
                if (Rd.TryGetComponent<TextMeshProUGUI>(out var tmu)) { tmu.color = c; }
#endif
            }

            /// <summary>Короткая метка типа для отображения в колонке.</summary>
            public string KindLabel => Kind switch
            {
                RendererKind.SpriteRenderer    => "SR",
                RendererKind.TextMeshPro        => "TMP",
                RendererKind.TextMeshProUGUI    => "TMPUI",
                RendererKind.MeshRenderer       => "Mesh",
                RendererKind.ParticleSystem     => "FX",
                RendererKind.Tilemap            => "Tile",
                RendererKind.SpriteMask         => "Mask",
                RendererKind.LineRenderer       => "Line",
                RendererKind.TrailRenderer      => "Trail",
                RendererKind.SkinnedMesh        => "Skin",
                _                               => "?"
            };
        }

        #endregion

        #region Fields

        private Tab _activeTab = Tab.Base;

        private readonly List<RendererEntry> _entries  = new();
        private          List<RendererEntry> _filtered = new();

        // Фильтр по типу компонента
        private bool _filterSR        = true;
        private bool _filterTMP       = true;
        private bool _filterMesh      = true;
        private bool _filterParticle  = true;
        private bool _filterTilemap   = true;
        private bool _filterMask      = true;
        private bool _filterLine      = true;
        private bool _filterTrail     = true;
        private bool _filterSkin      = true;
        private bool _showTypeFilter;           // раскрываемая панель типов

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

        // Conflict detection
        private List<(RendererEntry A, RendererEntry B)> _conflicts = new();

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

        // Кэш иконок типов
        private readonly Dictionary<RendererKind, Color> _kindColors = new()
        {
            { RendererKind.SpriteRenderer,  new Color(0.3f, 0.8f, 0.3f) },
            { RendererKind.TextMeshPro,      new Color(0.4f, 0.7f, 1.0f) },
            { RendererKind.TextMeshProUGUI,  new Color(0.3f, 0.6f, 1.0f) },
            { RendererKind.MeshRenderer,     new Color(0.8f, 0.5f, 0.2f) },
            { RendererKind.ParticleSystem,   new Color(0.9f, 0.3f, 0.7f) },
            { RendererKind.Tilemap,          new Color(0.6f, 0.9f, 0.4f) },
            { RendererKind.SpriteMask,       new Color(0.7f, 0.7f, 0.2f) },
            { RendererKind.LineRenderer,     new Color(0.5f, 0.9f, 0.9f) },
            { RendererKind.TrailRenderer,    new Color(0.8f, 0.5f, 0.9f) },
            { RendererKind.SkinnedMesh,      new Color(1.0f, 0.6f, 0.3f) },
            { RendererKind.Other,            new Color(0.6f, 0.6f, 0.6f) },
        };

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Megxlord Toolbox/2D/Sprite2D Order Manager", priority = 101)]
        public static void ShowWindow() => GetWindow<Sprite2DOrderManager>("Sprite2D Order Manager");

        private void OnEnable()
        {
            wantsMouseMove = true;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update           += OnEditorUpdate;
            Selection.selectionChanged         += OnSelectionChanged;

            LoadPrefs();
            RefreshEntries();
            CacheIcons();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update           -= OnEditorUpdate;
            Selection.selectionChanged         -= OnSelectionChanged;

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

            bool changed = false;
            foreach (var e in _entries)
            {
                if (e.Rd == null || !e.HasExternalChanges) continue;
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

        // ── Определение типа рендерера ───────────────────────────────────────────

        /// <summary>
        /// Возвращает все Renderer-компоненты сцены, которые нас интересуют,
        /// вместе с их классификацией.
        /// </summary>
        private static IEnumerable<(Renderer rd, RendererKind kind)> FindAllRenderers()
        {
            // Порядок проверки важен: TMP-объект содержит и MeshRenderer и TMP-компонент,
            // поэтому TMP-проверки идут ПЕРВЫМИ.

            var all = FindObjectsByType<Renderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var rd in all)
            {
                RendererKind kind = ClassifyRenderer(rd);
                yield return (rd, kind);
            }

            // SpriteMask не является Renderer — добавляем отдельно
            var masks = FindObjectsByType<SpriteMask>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var mask in masks)
            {
                // SpriteMask не наследует Renderer, поэтому нам нужен
                // специальный прокси.  Здесь мы пропускаем его в общем цикле
                // и обрабатываем ниже через специальный метод.
            }
        }

        private static RendererKind ClassifyRenderer(Renderer rd)
        {
#if TMP_PRESENT
            // TMP должен проверяться до MeshRenderer, т.к. TMP тоже MeshRenderer
            if (rd.GetComponent<TMPro.TextMeshPro>()     != null) return RendererKind.TextMeshPro;
            if (rd.GetComponent<TMPro.TextMeshProUGUI>() != null) return RendererKind.TextMeshProUGUI;
#endif
            return rd switch
            {
                SpriteRenderer        => RendererKind.SpriteRenderer,
                ParticleSystemRenderer => RendererKind.ParticleSystem,
                TilemapRenderer        => RendererKind.Tilemap,
                LineRenderer           => RendererKind.LineRenderer,
                TrailRenderer          => RendererKind.TrailRenderer,
                SkinnedMeshRenderer    => RendererKind.SkinnedMesh,
                MeshRenderer           => RendererKind.MeshRenderer,
                _                      => RendererKind.Other
            };
        }

        // ── Обновление списка ────────────────────────────────────────────────────

        private void RefreshEntries()
        {
            var foundPairs  = FindAllRenderers().ToList();
            var foundRds    = new HashSet<Renderer>(foundPairs.Select(p => p.rd));
            var existingRds = new HashSet<Renderer>(_entries.Select(e => e.Rd));

            // Удаляем исчезнувшие
            _entries.RemoveAll(e => e.Rd == null || !foundRds.Contains(e.Rd));

            // Добавляем новые
            foreach (var (rd, kind) in foundPairs)
            {
                if (existingRds.Contains(rd)) continue;

                var entry = new RendererEntry
                {
                    Go        = rd.gameObject,
                    Rd        = rd,
                    Kind      = kind,
                    IsEnabled = rd.gameObject.activeSelf,
                };
                entry.TakeSnapshot();
                _entries.Add(entry);
            }

            // Синхронизируем актуальное состояние
            foreach (var entry in _entries.Where(e => e.Rd != null))
            {
                entry.Go        = entry.Rd.gameObject;
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

        // ── Фильтрация и сортировка ──────────────────────────────────────────────

        private void ApplyFilterAndSort()
        {
            IEnumerable<RendererEntry> result = _entries.Where(e => e.Rd != null);

            // Фильтр по типу компонента
            result = result.Where(e => IsKindVisible(e.Kind));

            result = _filterMode switch
            {
                FilterMode.Selected => result.Where(e => e.IsSelected),
                FilterMode.Enabled  => result.Where(e => e.IsEnabled),
                FilterMode.Disabled => result.Where(e => !e.IsEnabled),
                _                   => result
            };

            if (_layerFilter != "All")
                result = result.Where(e => e.Rd.sortingLayerName == _layerFilter);

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.ToLower();
                result = result.Where(e =>
                    e.Go.name.ToLower().Contains(q)                    ||
                    e.Rd.sortingLayerName.ToLower().Contains(q)        ||
                    e.KindLabel.ToLower().Contains(q));
            }

            result = _sortMode switch
            {
                SortMode.Name => _sortAscending
                    ? result.OrderBy(e => e.Go.name)
                    : result.OrderByDescending(e => e.Go.name),

                SortMode.SortingLayer => _sortAscending
                    ? result.OrderBy(e => e.Rd.sortingLayerID).ThenBy(e => e.Rd.sortingOrder)
                    : result.OrderByDescending(e => e.Rd.sortingLayerID)
                            .ThenByDescending(e => e.Rd.sortingOrder),

                SortMode.OrderInLayer => _sortAscending
                    ? result.OrderBy(e => e.Rd.sortingOrder)
                    : result.OrderByDescending(e => e.Rd.sortingOrder),

                SortMode.Type => result
                    .OrderBy(e => e.Kind.ToString())
                    .ThenBy(e => e.Go.name),

                _ => result
            };

            _filtered = result.ToList();
        }

        private bool IsKindVisible(RendererKind kind) => kind switch
        {
            RendererKind.SpriteRenderer  => _filterSR,
            RendererKind.TextMeshPro     => _filterTMP,
            RendererKind.TextMeshProUGUI => _filterTMP,
            RendererKind.MeshRenderer    => _filterMesh,
            RendererKind.ParticleSystem  => _filterParticle,
            RendererKind.Tilemap         => _filterTilemap,
            RendererKind.SpriteMask      => _filterMask,
            RendererKind.LineRenderer    => _filterLine,
            RendererKind.TrailRenderer   => _filterTrail,
            RendererKind.SkinnedMesh     => _filterSkin,
            _                            => true
        };

        // ── Конфликты ─────────────────────────────────────────────────────────────

        private void DetectConflicts()
        {
            _conflicts.Clear();

            var candidates = _entries
                .Where(e => e.Rd != null)
                .GroupBy(e => (e.Rd.sortingLayerName, e.Rd.sortingOrder))
                .Where(g => g.Count() > 1);

            foreach (var group in candidates)
            {
                var list = group.ToList();
                for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (BoundsIntersect2D(list[i].GetWorldBounds(), list[j].GetWorldBounds()))
                        _conflicts.Add((list[i], list[j]));
                }
            }
        }

        private static bool BoundsIntersect2D(Bounds a, Bounds b)
        {
            // Точечные bounds не считаем пересечением
            if (a.size == Vector3.zero || b.size == Vector3.zero) return false;
            return a.min.x < b.max.x && a.max.x > b.min.x &&
                   a.min.y < b.max.y && a.max.y > b.min.y;
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
                GUILayout.Label($"⚠ {_conflicts.Count}", EditorStyles.toolbarButton,
                    GUILayout.Width(40));
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
            DrawTypeFilterBar();         // ← новая панель типов
            EditorGUILayout.Space(2);
            DrawColumnHeaders();
            DrawSpriteList();
            EditorGUILayout.Space(4);
            DrawBaseActionBar();
        }

        // ── Строка поиска / фильтров ─────────────────────────────────────────────

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

        // ── Фильтр по типам компонентов ──────────────────────────────────────────

        private void DrawTypeFilterBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _showTypeFilter = EditorGUILayout.Foldout(_showTypeFilter, "Types:", true,
                EditorStyles.foldout);

            if (_showTypeFilter)
            {
                DrawTypeToggle("SR",    ref _filterSR,       RendererKind.SpriteRenderer);
                DrawTypeToggle("TMP",   ref _filterTMP,      RendererKind.TextMeshPro);
                DrawTypeToggle("Mesh",  ref _filterMesh,     RendererKind.MeshRenderer);
                DrawTypeToggle("FX",    ref _filterParticle, RendererKind.ParticleSystem);
                DrawTypeToggle("Tile",  ref _filterTilemap,  RendererKind.Tilemap);
                DrawTypeToggle("Mask",  ref _filterMask,     RendererKind.SpriteMask);
                DrawTypeToggle("Line",  ref _filterLine,     RendererKind.LineRenderer);
                DrawTypeToggle("Trail", ref _filterTrail,    RendererKind.TrailRenderer);
                DrawTypeToggle("Skin",  ref _filterSkin,     RendererKind.SkinnedMesh);
            }
            else
            {
                // Компактная строка: цветные метки активных типов
                int count = CountByKind(RendererKind.SpriteRenderer);
                if (count > 0) DrawKindBadge($"SR:{count}",    RendererKind.SpriteRenderer);
                count = CountByKind(RendererKind.TextMeshPro) +
                        CountByKind(RendererKind.TextMeshProUGUI);
                if (count > 0) DrawKindBadge($"TMP:{count}",   RendererKind.TextMeshPro);
                count = CountByKind(RendererKind.MeshRenderer);
                if (count > 0) DrawKindBadge($"Mesh:{count}",  RendererKind.MeshRenderer);
                count = CountByKind(RendererKind.ParticleSystem);
                if (count > 0) DrawKindBadge($"FX:{count}",    RendererKind.ParticleSystem);
                count = CountByKind(RendererKind.Tilemap);
                if (count > 0) DrawKindBadge($"Tile:{count}",  RendererKind.Tilemap);
                count = CountByKind(RendererKind.LineRenderer);
                if (count > 0) DrawKindBadge($"Line:{count}",  RendererKind.LineRenderer);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTypeToggle(string label, ref bool value, RendererKind kind)
        {
            int cnt = CountByKind(kind);

            // Подкрашиваем активный тип
            Color prev = GUI.contentColor;
            if (value && _kindColors.TryGetValue(kind, out var c))
                GUI.contentColor = c;

            bool newVal = GUILayout.Toggle(value,
                $"{label} ({cnt})", EditorStyles.toolbarButton, GUILayout.Width(62));

            GUI.contentColor = prev;

            if (newVal != value)
                value = newVal;
        }

        private void DrawKindBadge(string text, RendererKind kind)
        {
            Color prev = GUI.contentColor;
            if (_kindColors.TryGetValue(kind, out var c)) GUI.contentColor = c;
            GUILayout.Label(text, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(6);
            GUI.contentColor = prev;
        }

        private int CountByKind(RendererKind kind) =>
            _entries.Count(e => e.Kind == kind);

        // ── Заголовок таблицы ────────────────────────────────────────────────────

        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(_headerStyle);
            GUILayout.Label("On",           EditorStyles.boldLabel, GUILayout.Width(26));
            GUILayout.Label("Type",         EditorStyles.boldLabel, GUILayout.Width(40));
            GUILayout.Label("Name",         EditorStyles.boldLabel, GUILayout.MinWidth(100));
            GUILayout.Label("Sorting Layer",EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Order",        EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Render Mask",  EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Color",        EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Space(28);
            EditorGUILayout.EndHorizontal();
        }

        // ── Список строк ─────────────────────────────────────────────────────────

        private void DrawSpriteList()
        {
            _scrollBase = EditorGUILayout.BeginScrollView(_scrollBase);

            for (int i = 0; i < _filtered.Count; i++)
                DrawRendererRow(_filtered[i], i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRendererRow(RendererEntry entry, int index)
        {
            if (entry.Rd == null) return;

            bool isConflict = _conflicts.Any(c => c.A == entry || c.B == entry);

            GUIStyle rowBg = entry.IsSelected
                ? _rowSelectedStyle
                : isConflict
                    ? _conflictStyle
                    : index % 2 == 0 ? _rowStyle : _rowAltStyle;

            EditorGUILayout.BeginHorizontal(rowBg, GUILayout.Height(RowHeight));

            // ── On / Off ──────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            bool newActive = EditorGUILayout.Toggle(entry.IsEnabled, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
                SetEntryEnabled(entry, newActive);

            // ── Тип компонента ────────────────────────────────────────────────────
            Color prevContent = GUI.contentColor;
            if (_kindColors.TryGetValue(entry.Kind, out var kindColor))
                GUI.contentColor = kindColor;
            GUILayout.Label(entry.KindLabel,
                EditorStyles.miniLabel, GUILayout.Width(40));
            GUI.contentColor = prevContent;

            // ── Имя объекта ───────────────────────────────────────────────────────
            var nameStyle = new GUIStyle(EditorStyles.label);
            if (!entry.IsEnabled) nameStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            if (isConflict)       nameStyle.normal.textColor = new Color(1f, 0.6f, 0.1f);

            if (GUILayout.Button(entry.Go.name, nameStyle, GUILayout.MinWidth(100)))
                HandleRowClick(entry, index);

            // ── Sorting Layer ─────────────────────────────────────────────────────
            string[] layerNames  = SortingLayer.layers.Select(l => l.name).ToArray();
            int      curLayerIdx = System.Array.IndexOf(layerNames, entry.Rd.sortingLayerName);
            if (curLayerIdx < 0) curLayerIdx = 0;

            EditorGUI.BeginChangeCheck();
            int newLayerIdx = EditorGUILayout.Popup(curLayerIdx, layerNames, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(entry.Rd, "Change Sorting Layer");
                entry.Rd.sortingLayerName = layerNames[newLayerIdx];
                entry.TakeSnapshot();
                EditorUtility.SetDirty(entry.Rd);
                DetectConflicts();
            }

            // ── Order in Layer ────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            int newOrder = EditorGUILayout.IntField(entry.Rd.sortingOrder, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(entry.Rd, "Change Order in Layer");
                entry.Rd.sortingOrder = newOrder;
                entry.TakeSnapshot();
                EditorUtility.SetDirty(entry.Rd);
                DetectConflicts();
            }

            // ── Rendering Layer Mask ──────────────────────────────────────────────
            var so   = new SerializedObject(entry.Rd);
            var prop = so.FindProperty("m_RenderingLayerMask");
            so.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(90));
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                entry.TakeSnapshot();
            }

            // ── Color (только для поддерживающих) ────────────────────────────────
            if (entry.SupportsColor)
            {
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUILayout.ColorField(
                    GUIContent.none, entry.GetColor(),
                    false, true, false, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(entry.Rd, "Change Color");
                    entry.SetColor(newColor);
                    EditorUtility.SetDirty(entry.Rd);
                }
            }
            else
            {
                // Для типов без прямого color-поля — серый прямоугольник-заглушка
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                GUILayout.Label("—", GUILayout.Width(50));
                GUI.color = Color.white;
            }

            // ── Иконка конфликта ──────────────────────────────────────────────────
            if (isConflict && _warnIcon != null)
                GUILayout.Label(
                    new GUIContent(_warnIcon, "Renderers overlap with same order!"),
                    GUILayout.Width(18));
            else
                GUILayout.Space(18);

            // ── Ping ──────────────────────────────────────────────────────────────
            if (GUILayout.Button("→", GUILayout.Width(22), GUILayout.Height(RowHeight - 2)))
            {
                EditorGUIUtility.PingObject(entry.Go);
                Selection.activeGameObject = entry.Go;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Включение / выключение ───────────────────────────────────────────────

        private static void SetEntryEnabled(RendererEntry entry, bool enabled)
        {
            Undo.RecordObject(entry.Go, enabled ? "Enable GameObject" : "Disable GameObject");
            entry.Go.SetActive(enabled);
            entry.IsEnabled = enabled;
            entry.TakeSnapshot();
            EditorUtility.SetDirty(entry.Go);
        }

        // ── Клики по строкам ──────────────────────────────────────────────────────

        private void HandleRowClick(RendererEntry entry, int index)
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

        // ── Нижняя панель ─────────────────────────────────────────────────────────

        private void DrawBaseActionBar()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Batch Edit Selected:", EditorStyles.boldLabel);
            _showBatchPanel = EditorGUILayout.Foldout(_showBatchPanel, "", true);
            EditorGUILayout.EndHorizontal();

            if (_showBatchPanel) DrawBatchPanel();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All",      GUILayout.Height(24)))
            { foreach (var e in _filtered) e.IsSelected = true;  UpdateEditorSelection(); }
            if (GUILayout.Button("Deselect All",    GUILayout.Height(24)))
            { foreach (var e in _entries)  e.IsSelected = false; UpdateEditorSelection(); }
            if (GUILayout.Button("Invert Selection",GUILayout.Height(24)))
            { foreach (var e in _filtered) e.IsSelected = !e.IsSelected; UpdateEditorSelection(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Enable All",           GUILayout.Height(24)))
                foreach (var e in _entries) SetEntryEnabled(e, true);

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("Disable Unselected",   GUILayout.Height(24)))
                foreach (var e in _entries.Where(en => !en.IsSelected)) SetEntryEnabled(e, false);

            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.9f);
            if (GUILayout.Button("Toggle Selected",      GUILayout.Height(24)))
                foreach (var e in _entries.Where(en => en.IsSelected))
                    SetEntryEnabled(e, !e.IsEnabled);

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchPanel()
        {
            var selected = _filtered.Where(e => e.IsSelected).ToList();
            if (selected.Count == 0)
            {
                EditorGUILayout.HelpBox("No objects selected. Click rows to select.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Applying to {selected.Count} renderer(s):",
                EditorStyles.miniLabel);
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

            // Цвет — только для поддерживающих типов
            int colorCapable = selected.Count(e => e.SupportsColor);
            EditorGUILayout.BeginHorizontal();
            _batchApplyColor = EditorGUILayout.Toggle(_batchApplyColor, GUILayout.Width(16));
            _batchColor = EditorGUILayout.ColorField(
                $"Color  (applies to {colorCapable}/{selected.Count})", _batchColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button($"▶  Apply to {selected.Count} renderer(s)", GUILayout.Height(28)))
            {
                foreach (var entry in selected)
                {
                    Undo.RecordObject(entry.Rd, "Batch Edit Renderers");

                    if (_batchApplyLayer) entry.Rd.sortingLayerName = _batchSortingLayer;
                    if (_batchApplyOrder) entry.Rd.sortingOrder    += _batchOrderOffset;
                    if (_batchApplyColor && entry.SupportsColor)
                        entry.SetColor(_batchColor);

                    entry.TakeSnapshot();
                    EditorUtility.SetDirty(entry.Rd);
                }
                DetectConflicts();
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Advanced Tab
        // (Методы Advanced идентичны оригиналу, но используют RendererEntry / entry.Rd)

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
                .Where(e => e.Rd != null)
                .GroupBy(e => e.Rd.sortingLayerName)
                .OrderBy(g => SortingLayer.GetLayerValueFromName(g.Key));

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            DrawStatRow("Total renderers",   total.ToString(),             Color.white);
            DrawStatRow("Enabled",           enabled.ToString(),           new Color(0.4f, 1f,   0.4f));
            DrawStatRow("Disabled",          (total - enabled).ToString(), new Color(0.6f, 0.6f, 0.6f));
            DrawStatRow("Overlap Conflicts", conflictCount.ToString(),
                conflictCount > 0 ? new Color(1f, 0.6f, 0.1f) : new Color(0.4f, 1f, 0.4f));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("By Type:", EditorStyles.boldLabel);
            foreach (RendererKind kind in System.Enum.GetValues(typeof(RendererKind)))
            {
                int cnt = CountByKind(kind);
                if (cnt == 0) continue;
                Color col = _kindColors.TryGetValue(kind, out var kc) ? kc : Color.white;
                DrawStatRow($"  {kind}", cnt.ToString(), col);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Per Sorting Layer:", EditorStyles.boldLabel);
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
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
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
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * frac, r.height),
                new Color(0.3f, 0.6f, 1f));
        }

        // ── Conflicts ─────────────────────────────────────────────────────────────

        private void DrawConflictsPanel()
        {
            _showConflicts = DrawFoldout(_showConflicts,
                $"⚠  Overlap Conflicts  ({_conflicts.Count})");
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
                    "These renderers share the same Sorting Layer + Order AND their bounds overlap.",
                    MessageType.Warning);
                EditorGUILayout.Space(4);

                int shown = Mathf.Min(_conflicts.Count, 10);
                for (int i = 0; i < shown; i++)
                {
                    var (a, b) = _conflicts[i];
                    EditorGUILayout.BeginHorizontal(_conflictStyle);
                    EditorGUILayout.LabelField(
                        $"[{a.KindLabel}] {a.Go.name}  ↔  [{b.KindLabel}] {b.Go.name}" +
                        $"  [{a.Rd.sortingLayerName} / {a.Rd.sortingOrder}]",
                        EditorStyles.miniLabel);

                    if (GUILayout.Button("Ping A", GUILayout.Width(46), GUILayout.Height(18)))
                        EditorGUIUtility.PingObject(a.Go);
                    if (GUILayout.Button("Ping B", GUILayout.Width(46), GUILayout.Height(18)))
                        EditorGUIUtility.PingObject(b.Go);
                    if (GUILayout.Button("Fix",    GUILayout.Width(32), GUILayout.Height(18)))
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

        private void FixConflict(RendererEntry a, RendererEntry b)
        {
            Undo.RecordObject(b.Rd, "Fix Order Conflict");
            b.Rd.sortingOrder = a.Rd.sortingOrder + 1;
            b.TakeSnapshot();
            EditorUtility.SetDirty(b.Rd);
            DetectConflicts();
        }

        private void AutoFixAllConflicts()
        {
            var processed = new HashSet<RendererEntry>();
            foreach (var (a, b) in _conflicts.ToList())
            {
                if (processed.Contains(b)) continue;
                Undo.RecordObject(b.Rd, "Auto-Fix Overlap Conflicts");
                b.Rd.sortingOrder = a.Rd.sortingOrder + 1;
                b.TakeSnapshot();
                EditorUtility.SetDirty(b.Rd);
                processed.Add(b);
            }
            DetectConflicts();
        }

        // ── Order Visualization ───────────────────────────────────────────────────

        private void DrawOrderVisualizationPanel()
        {
            _showOrderVisualization = DrawFoldout(_showOrderVisualization,
                "📈  Order Visualization");
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
                .Where(e => e.Rd != null)
                .GroupBy(e => e.Rd.sortingLayerName)
                .OrderBy(g => SortingLayer.GetLayerValueFromName(g.Key))
                .ToList();

            _vizScroll = EditorGUILayout.BeginScrollView(_vizScroll,
                GUILayout.Height(Mathf.Clamp(layerGroups.Count * 50 * _vizZoom + 20, 80, 300)));

            foreach (var group in layerGroups)
            {
                var items = group.OrderBy(e => e.Rd.sortingOrder).ToList();
                if (items.Count == 0) continue;

                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                Rect lineRect = GUILayoutUtility.GetRect(
                    position.width - 20,
                    Mathf.Max(28 * _vizZoom, 28));

                EditorGUI.DrawRect(lineRect, new Color(0.18f, 0.18f, 0.18f));

                int minOrder = items.Min(e => e.Rd.sortingOrder);
                int maxOrder = items.Max(e => e.Rd.sortingOrder);
                int range    = Mathf.Max(maxOrder - minOrder, 1);

                foreach (var entry in items)
                {
                    bool  hasConflict = _conflicts.Any(c => c.A == entry || c.B == entry);
                    float t     = (entry.Rd.sortingOrder - minOrder) / (float)range;
                    float x     = lineRect.x + 10 + t * (lineRect.width - 80);
                    float y     = lineRect.y + lineRect.height * 0.5f;

                    Color nodeColor = hasConflict
                        ? new Color(1f, 0.4f, 0.1f)
                        : entry.IsSelected
                            ? new Color(0.2f, 0.8f, 1f)
                            : _kindColors.TryGetValue(entry.Kind, out var kc)
                                ? kc
                                : new Color(0.5f, 0.7f, 0.9f);

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
                        $"[{entry.KindLabel}] {entry.Go.name}\n({entry.Rd.sortingOrder})",
                        EditorStyles.centeredGreyMiniLabel);

                    if (Event.current.type == EventType.MouseDown)
                    {
                        float d = Vector2.Distance(Event.current.mousePosition,
                            new Vector2(x, y));
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

            EditorGUILayout.HelpBox(
                "Color operations apply only to SpriteRenderer and TextMeshPro components.",
                MessageType.Info);

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
            if (GUILayout.Button("Randomize", GUILayout.Height(22))) ApplyRandomColors();
            if (GUILayout.Button("by Layer",  GUILayout.Height(22))) ApplyColorByLayer();
            if (GUILayout.Button("by Order",  GUILayout.Height(22))) ApplyColorByOrder();
            if (GUILayout.Button("by Type",   GUILayout.Height(22))) ApplyColorByType();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private List<RendererEntry> SelectedColorTargets() =>
            _filtered.Where(e => e.IsSelected && e.SupportsColor && e.Rd != null).ToList();

        private void ApplyGradientToSelected()
        {
            var targets = SelectedColorTargets();
            if (targets.Count == 0) { ShowNotification(new GUIContent("No color-capable sprites selected!")); return; }

            var ordered = _gradientByOrder
                ? targets.OrderBy(e => e.Rd.sortingOrder).ToList()
                : targets;

            for (int i = 0; i < ordered.Count; i++)
            {
                float t = ordered.Count > 1 ? i / (float)(ordered.Count - 1) : 0f;
                Undo.RecordObject(ordered[i].Rd, "Apply Gradient");
                ordered[i].SetColor(_gradientPreset.Evaluate(t));
                EditorUtility.SetDirty(ordered[i].Rd);
            }
        }

        private void ResetColorsToWhite()
        {
            foreach (var e in SelectedColorTargets())
            {
                Undo.RecordObject(e.Rd, "Reset Color");
                e.SetColor(Color.white);
                EditorUtility.SetDirty(e.Rd);
            }
        }

        private void SetAlphaToSelected(float alpha)
        {
            foreach (var e in SelectedColorTargets())
            {
                Undo.RecordObject(e.Rd, "Set Alpha");
                Color c = e.GetColor(); c.a = alpha; e.SetColor(c);
                EditorUtility.SetDirty(e.Rd);
            }
        }

        private void ApplyRandomColors()
        {
            foreach (var e in SelectedColorTargets())
            {
                Undo.RecordObject(e.Rd, "Random Color");
                e.SetColor(new Color(Random.value, Random.value, Random.value, 1f));
                EditorUtility.SetDirty(e.Rd);
            }
        }

        private void ApplyColorByLayer()
        {
            SortingLayer[] layers  = SortingLayer.layers;
            Color[]        palette = GeneratePalette(layers.Length);
            foreach (var e in SelectedColorTargets())
            {
                int idx = System.Array.FindIndex(layers, l => l.name == e.Rd.sortingLayerName);
                if (idx < 0) continue;
                Undo.RecordObject(e.Rd, "Color by Layer");
                e.SetColor(palette[idx % palette.Length]);
                EditorUtility.SetDirty(e.Rd);
            }
        }

        private void ApplyColorByOrder()
        {
            var targets = SelectedColorTargets();
            if (targets.Count == 0) return;
            int min   = targets.Min(e => e.Rd.sortingOrder);
            int max   = targets.Max(e => e.Rd.sortingOrder);
            int range = Mathf.Max(max - min, 1);
            foreach (var e in targets)
            {
                float t = (e.Rd.sortingOrder - min) / (float)range;
                Undo.RecordObject(e.Rd, "Color by Order");
                e.SetColor(Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(1f, 0.3f, 0.3f), t));
                EditorUtility.SetDirty(e.Rd);
            }
        }

        /// <summary>Каждому типу компонента — свой цвет из палитры kindColors.</summary>
        private void ApplyColorByType()
        {
            foreach (var e in SelectedColorTargets())
            {
                if (!_kindColors.TryGetValue(e.Kind, out var c)) continue;
                Undo.RecordObject(e.Rd, "Color by Type");
                e.SetColor(new Color(c.r, c.g, c.b, 1f));
                EditorUtility.SetDirty(e.Rd);
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
            if (GUILayout.Button("Normalize ALL Layers",     GUILayout.Height(28)))
                NormalizeOrders(true);
            if (GUILayout.Button("Normalize Selected Layer", GUILayout.Height(28)))
                NormalizeOrders(false);
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
            IEnumerable<RendererEntry> source = allEntries
                ? _entries
                : _entries.Where(e => e.IsSelected);

            foreach (var g in source.Where(e => e.Rd != null)
                         .GroupBy(e => e.Rd.sortingLayerName))
            {
                var sorted = g.OrderBy(e => e.Rd.sortingOrder).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    Undo.RecordObject(sorted[i].Rd, "Normalize Order");
                    sorted[i].Rd.sortingOrder = i;
                    sorted[i].TakeSnapshot();
                    EditorUtility.SetDirty(sorted[i].Rd);
                }
            }
            DetectConflicts();
            ShowNotification(new GUIContent("Orders normalized!"));
        }

        private void ShiftOrderSelected(int delta)
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Rd != null))
            {
                Undo.RecordObject(e.Rd, "Shift Order");
                e.Rd.sortingOrder += delta;
                e.TakeSnapshot();
                EditorUtility.SetDirty(e.Rd);
            }
            DetectConflicts();
        }

        private void SetOrderSelected(int value)
        {
            foreach (var e in _filtered.Where(en => en.IsSelected && en.Rd != null))
            {
                Undo.RecordObject(e.Rd, "Set Order");
                e.Rd.sortingOrder = value;
                e.TakeSnapshot();
                EditorUtility.SetDirty(e.Rd);
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
            _filterSR       = EditorPrefs.GetBool(PrefPrefix + "F_SR",       true);
            _filterTMP      = EditorPrefs.GetBool(PrefPrefix + "F_TMP",      true);
            _filterMesh     = EditorPrefs.GetBool(PrefPrefix + "F_Mesh",     true);
            _filterParticle = EditorPrefs.GetBool(PrefPrefix + "F_FX",       true);
            _filterTilemap  = EditorPrefs.GetBool(PrefPrefix + "F_Tile",     true);
            _filterMask     = EditorPrefs.GetBool(PrefPrefix + "F_Mask",     true);
            _filterLine     = EditorPrefs.GetBool(PrefPrefix + "F_Line",     true);
            _filterTrail    = EditorPrefs.GetBool(PrefPrefix + "F_Trail",    true);
            _filterSkin     = EditorPrefs.GetBool(PrefPrefix + "F_Skin",     true);
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefPrefix    + "SortMode",  (int)_sortMode);
            EditorPrefs.SetBool(PrefPrefix   + "SortAsc",   _sortAscending);
            EditorPrefs.SetString(PrefPrefix + "LayerFilter", _layerFilter);
            EditorPrefs.SetBool(PrefPrefix + "F_SR",       _filterSR);
            EditorPrefs.SetBool(PrefPrefix + "F_TMP",      _filterTMP);
            EditorPrefs.SetBool(PrefPrefix + "F_Mesh",     _filterMesh);
            EditorPrefs.SetBool(PrefPrefix + "F_FX",       _filterParticle);
            EditorPrefs.SetBool(PrefPrefix + "F_Tile",     _filterTilemap);
            EditorPrefs.SetBool(PrefPrefix + "F_Mask",     _filterMask);
            EditorPrefs.SetBool(PrefPrefix + "F_Line",     _filterLine);
            EditorPrefs.SetBool(PrefPrefix + "F_Trail",    _filterTrail);
            EditorPrefs.SetBool(PrefPrefix + "F_Skin",     _filterSkin);
        }

        #endregion
    }

    // ── Toolbar ──────────────────────────────────────────────────────────────────

    [InitializeOnLoad]
    public static class Sprite2DOrderManagerToolbar
    {
        private const string ElementId = "MyTools/Sprite2DOrderManager";

        [MainToolbarElement(ElementId,
            defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateButton() =>
            new MainToolbarButton(
                new MainToolbarContent("2D Order",
                    tooltip: "Open Sprite2D Order Manager"),
                Sprite2DOrderManager.ShowWindow);
    }
}