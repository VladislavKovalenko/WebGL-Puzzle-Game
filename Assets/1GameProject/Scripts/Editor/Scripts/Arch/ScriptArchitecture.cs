using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace _1GameProject.Scripts.Editor.Scripts.Arch
{
    [Flags]
    public enum RiskFlags : ushort
    {
        None          = 0,
        Alloc         = 1 << 0,
        FindObjects   = 1 << 1,
        SendMessage   = 1 << 2,
        PhysicsCast   = 1 << 3,
        StringOps     = 1 << 4,
        Linq          = 1 << 5,
        EarlyReturn   = 1 << 6,
        Loop          = 1 << 7,
        NestedLoop    = 1 << 8,
        MethodCalls   = 1 << 9,
    }

    public sealed class PollingHit
    {
        public string    ScriptName;
        public string    RelativePath;
        public string    MethodName;
        public int       MethodLine;
        public int       MethodBodyLines;
        public int       LoadScore;
        public RiskFlags Flags;
        public bool      IsSelected;
        public int       CalledMethodsCount;
        public int       EffectiveBodyLines;
        public List<string> CalledMethods;
    }

    public class ScriptArchitectureWindow : EditorWindow
    {
        #region Enums & Constants

        private enum Tab { Finder }

        private enum SortMode
        {
            LoadScore,
            Name,
            Method,
            LineCount
        }

        private const string PrefPrefix = "ScriptArchitecture_";
        private const float  RowHeight  = 24f;
        private const float  ColIcon   = 22f;
        private const float  ColName   = 140f;
        private const float  ColMethod = 110f;
        private const float  ColLines  = 44f;
        private const float  ColRisk   = 130f;
        private const float  ColFlags  = 120f;
        private const float  ColPing   = 26f;

        #endregion

        #region Inner Types

        private readonly struct PollingMethodMeta
        {
            public readonly int    BaseWeight;
            public readonly string Description;
            public readonly Color  AccentColor;

            public PollingMethodMeta(int weight, string desc, Color color)
            {
                BaseWeight  = weight;
                Description = desc;
                AccentColor = color;
            }
        }

        #endregion

        #region Pre-compiled Regex

        private static readonly RxP AllocRx = new(
            @"(?:\bnew\s+\w|\binstantiate\b|\bgetcomponent\s*[<(]|" +
            @"\bgetcomponents\s*[<(]|\baddcomponent\s*[<(])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP FindRx = new(
            @"(?:\bfindobjectsoftype\b|\bfindobjectoftype\b|\bfindobjectsbytag\b|" +
            @"\bfindobjectwithtag\b|\bgameobject\.find\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP SendRx = new(
            @"(?:\bsendmessage\s*\(|\bbroadcastmessage\s*\(|\bsendmessageupwards\s*\()",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP PhysicsRx = new(
            @"(?:\bphysics\.raycast\b|\bphysics\.overlapsphere\b|" +
            @"\bphysics2d\.[a-z]+cast\b|\bphysics\.[a-z]+cast\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP StringRx = new(
            @"(?:\bstring\.format\b|\btostring\s*\(|""[^""]*""\s*\+|\+\s*"")",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP LinqRx = new(
            @"\.(?:where|select|tolist|toarray|firstordefault|any|count)\s*\(",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP EarlyReturnRx = new(
            @"if\s*\(.+?\)\s*(?:return|continue)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP LoopRx = new(
            @"(?:\bfor\s*\(|\bforeach\s*\(|\bwhile\s*\(|\bdo\s*\{)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly RxP MethodCallRx = new(
            @"\b[a-z_][a-zA-Z0-9_]*\s*\(",
            RegexOptions.Compiled);

        private readonly struct RxP
        {
            private readonly Regex _rx;
            public RxP(string pattern, RegexOptions options) => _rx = new Regex(pattern, options);
            public bool IsMatch(string input) => _rx.IsMatch(input);
        }

        #endregion

        #region Polling Method Database

        private static readonly Dictionary<string, PollingMethodMeta> KnownMethods = new(8)
        {
            { "Update",             new(3, "Every frame. FPS-dependent.",                              new Color(0.9f, 0.7f, 0.1f)) },
            { "LateUpdate",         new(3, "Every frame, after Update.",                               new Color(0.9f, 0.7f, 0.1f)) },
            { "FixedUpdate",        new(4, "Fixed tick (~50/s). Physics-heavy.",                        new Color(0.9f, 0.4f, 0.1f)) },
            { "OnGUI",              new(5, "2+ times/frame (Layout + Repaint). Most expensive.",        new Color(0.9f, 0.1f, 0.1f)) },
            { "OnAnimatorMove",     new(3, "Every animation frame (when Animator is active).",          new Color(0.9f, 0.7f, 0.1f)) },
            { "OnAnimatorIK",       new(3, "Every animation IK pass.",                                  new Color(0.9f, 0.7f, 0.1f)) },
            { "OnRenderObject",     new(4, "Called after object rendering each frame.",                  new Color(0.9f, 0.4f, 0.1f)) },
            { "OnWillRenderObject", new(4, "Called before render if object is visible.",                 new Color(0.9f, 0.4f, 0.1f)) },
        };

        #endregion

        #region Fields

        private readonly List<PollingHit> _allHits      = new(128);
        private readonly List<PollingHit> _filteredHits = new(128);
        private PollingHit _selectedHit;
        private int        _cachedHighRiskCount;
        private int        _cachedScriptCount;

        private bool               _isScanning;
        private string             _scanStatus = "Press ▶ Scan Project to start.";
        private CancellationTokenSource _scanCts;

        private string       _searchQuery  = string.Empty;
        private string       _scanFolder   = "Assets";
        private HashSet<string> _methodFilter = new()
        {
            "Update", "LateUpdate", "FixedUpdate",
            "OnGUI", "OnAnimatorMove", "OnAnimatorIK",
            "OnRenderObject", "OnWillRenderObject"
        };
        private bool    _methodFilterDropdownOpen;
        private Rect    _methodFilterButtonRect;
        private SortMode     _sortMode     = SortMode.LoadScore;
        private bool         _sortAsc;
        private bool         _onlyHighRisk;
        private bool         _filterDirty;

        private Vector2 _scrollFinder;
        private bool    _showDetail = true;
        private float   _detailWidth = 320f;

        private GUIStyle _rowStyle;
        private GUIStyle _rowAltStyle;
        private GUIStyle _rowSelectedStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _scriptNameNormal;
        private GUIStyle _scriptNameHighRisk;
        private GUIStyle _methodLabel;
        private GUIStyle _riskBarLabel;
        private GUIStyle _flagLabelNormal;
        private GUIStyle _flagLabelActive;
        private GUIStyle _flagLabelDim;
        private GUIStyle _detailTitle;

        private bool _stylesReady;

        private Texture2D _scriptIcon;

        private readonly List<Texture2D> _ownedTextures = new(8);

        private static readonly ConcurrentDictionary<string, MethodInfo> _projectMethods
            = new(StringComparer.Ordinal);

        private readonly struct MethodInfo
        {
            public readonly string   ScriptName;
            public readonly string   MethodName;
            public readonly int      BodyLines;
            public readonly RiskFlags Flags;

            public MethodInfo(string script, string method, int bodyLines, RiskFlags flags)
            {
                ScriptName = script;
                MethodName = method;
                BodyLines  = bodyLines;
                Flags      = flags;
            }
        }

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Megxlord Toolbox/Scripts/Script Architecture", priority = 200)]
        public static void ShowWindow()
        {
            const float minW = ColIcon + ColName + ColMethod + ColLines +
                               ColRisk + ColFlags + ColPing + 360f;
            const float minH = 480f;

            var win = GetWindow<ScriptArchitectureWindow>("Script Architecture");
            win.minSize = new Vector2(minW, minH);

            if (win.position.width < minW || win.position.height < minH)
            {
                var screen = new Vector2(
                    Screen.currentResolution.width,
                    Screen.currentResolution.height);

                win.position = new Rect(
                    (screen.x - minW) * 0.5f,
                    (screen.y - minH) * 0.5f,
                    minW,
                    minH);
            }
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            _stylesReady = false;
            CacheIcons();
            LoadPrefs();
        }

        private void OnDisable()
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;

            SavePrefs();
            DestroyOwnedTextures();
        }

        private void CacheIcons()
        {
            _scriptIcon = EditorGUIUtility.FindTexture("cs Script Icon");
        }

        #endregion

        #region GUI Root

        private void OnGUI()
        {
            InitStyles();

            if (_filterDirty)
            {
                _filterDirty = false;
                ApplyFilterAndSort();
            }

            DrawToolbar();
            EditorGUILayout.Space(2);
            DrawFinderTab();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Toggle(true, "🔍  Finder",
                EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (_allHits.Count > 0)
            {
                GUILayout.Label(
                    $"Scripts: {_cachedScriptCount}  |  " +
                    $"Hits: {_allHits.Count}  |  " +
                    $"High-Risk: {_cachedHighRiskCount}",
                    EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label(_scanStatus, EditorStyles.miniLabel);
            }

            GUILayout.Space(6);

            if (_isScanning)
            {
                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
                if (GUILayout.Button("■ Stop", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    _scanCts?.Cancel();
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("▶ Scan", EditorStyles.toolbarButton, GUILayout.Width(64)))
                    RunScan();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Finder Tab

        private void DrawFinderTab()
        {
            DrawFilterBar();
            EditorGUILayout.Space(2);

            if (_isScanning)
                EditorGUILayout.HelpBox("Scanning project… please wait.", MessageType.Info);

            if (_allHits.Count == 0 && !_isScanning)
            {
                EditorGUILayout.HelpBox(
                    "Press ▶ Scan to analyze all C# scripts in Assets/ for polling methods.",
                    MessageType.Info);
                return;
            }

            if (_allHits.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                DrawColumnHeaders();
                DrawHitList();
                EditorGUILayout.EndVertical();

                if (_showDetail)
                {
                    GUILayout.Space(4);
                    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(_detailWidth));

                    if (_selectedHit != null)
                        DrawDetailPanel(_selectedHit);
                    else
                        DrawDetailEmpty();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();

            _searchQuery = EditorGUILayout.TextField(
                _searchQuery, EditorStyles.toolbarSearchField, GUILayout.MinWidth(100));

            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchQuery = "";

            GUILayout.Space(6);

            int activeCount = _methodFilter.Count;
            int totalCount  = KnownMethods.Count;
            string btnLabel = activeCount == totalCount
                ? "Methods: All"
                : activeCount == 0
                    ? "Methods: None"
                    : $"Methods: {activeCount}/{totalCount}";

            if (GUILayout.Button(btnLabel, EditorStyles.toolbarDropDown, GUILayout.Width(120)))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("All"), activeCount == totalCount, () =>
                {
                    if (_methodFilter.Count == totalCount)
                        _methodFilter.Clear();
                    else
                    {
                        _methodFilter.Clear();
                        foreach (var k in KnownMethods.Keys)
                            _methodFilter.Add(k);
                    }
                    _filterDirty = true;
                });

                menu.AddSeparator("");

                foreach (var methodName in KnownMethods.Keys)
                {
                    string captured = methodName;
                    bool   isOn     = _methodFilter.Contains(captured);

                    menu.AddItem(new GUIContent(captured), isOn, () =>
                    {
                        if (_methodFilter.Contains(captured))
                            _methodFilter.Remove(captured);
                        else
                            _methodFilter.Add(captured);

                        _filterDirty = true;
                    });
                }

                menu.ShowAsContext();
            }

            GUILayout.Space(6);

            _onlyHighRisk = GUILayout.Toggle(_onlyHighRisk, "High-Risk",
                EditorStyles.toolbarButton, GUILayout.Width(74));

            GUILayout.Space(6);

            string folderLabel = _scanFolder == "Assets"
                ? "All"
                : Path.GetFileName(_scanFolder);

            if (GUILayout.Button(folderLabel, EditorStyles.toolbarDropDown, GUILayout.Width(80)))
                ShowFolderMenu();

            if (_scanFolder != "Assets")
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    _scanFolder  = "Assets";
                    _filterDirty = true;
                }
            }

            GUILayout.Space(6);

            _sortMode = (SortMode)EditorGUILayout.EnumPopup(
                _sortMode, EditorStyles.toolbarPopup, GUILayout.Width(80));

            if (GUILayout.Button(_sortAsc ? "▲" : "▼",
                    EditorStyles.toolbarButton, GUILayout.Width(22)))
                _sortAsc = !_sortAsc;

            if (EditorGUI.EndChangeCheck())
                _filterDirty = true;

            GUILayout.FlexibleSpace();

            _showDetail = GUILayout.Toggle(_showDetail, "Detail",
                EditorStyles.toolbarButton, GUILayout.Width(46));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(_headerStyle);
            GUILayout.Space(ColIcon);
            GUILayout.Label("Script",  EditorStyles.boldLabel, GUILayout.Width(ColName));
            GUILayout.Label("Method",  EditorStyles.boldLabel, GUILayout.Width(ColMethod));
            GUILayout.Label("Lines",   EditorStyles.boldLabel, GUILayout.Width(ColLines));
            GUILayout.Label("Risk",    EditorStyles.boldLabel, GUILayout.Width(ColRisk));
            GUILayout.Label("Flags",   EditorStyles.boldLabel, GUILayout.Width(ColFlags));
            GUILayout.Space(ColPing);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHitList()
        {
            _scrollFinder = EditorGUILayout.BeginScrollView(_scrollFinder);

            int count = _filteredHits.Count;
            if (count == 0)
            {
                EditorGUILayout.HelpBox("Nothing found. Adjust filters.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    DrawHitRow(_filteredHits[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHitRow(PollingHit hit, int index)
        {
            if (hit == null) return;

            if (_rowStyle == null || _scriptNameNormal == null || _methodLabel == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(hit.ScriptName ?? "?", GUILayout.Width(ColName));
                EditorGUILayout.LabelField(hit.MethodName ?? "?", GUILayout.Width(ColMethod));
                EditorGUILayout.EndHorizontal();
                return;
            }

            bool isHigh = hit.LoadScore >= 60;
            GUIStyle bg = hit.IsSelected
                ? _rowSelectedStyle
                : (index & 1) == 0 ? _rowStyle : _rowAltStyle;

            EditorGUILayout.BeginHorizontal(bg, GUILayout.Height(RowHeight));

            if (_scriptIcon != null)
                GUILayout.Label(_scriptIcon,
                    GUILayout.Width(ColIcon), GUILayout.Height(RowHeight));
            else
                GUILayout.Space(ColIcon);

            if (GUILayout.Button(hit.ScriptName,
                    isHigh ? _scriptNameHighRisk : _scriptNameNormal,
                    GUILayout.Width(ColName)))
            {
                if (_selectedHit == hit)
                    OpenScriptAtLine(hit);
                else
                    SelectHit(hit);

                Repaint();
            }

            Color methodColor = KnownMethods.TryGetValue(hit.MethodName, out var meta)
                ? meta.AccentColor
                : Color.gray;
            GUI.color = methodColor;
            GUILayout.Label(hit.MethodName, _methodLabel, GUILayout.Width(ColMethod));
            GUI.color = Color.white;

            int displayLines = hit.EffectiveBodyLines > 0 ? hit.EffectiveBodyLines : hit.MethodBodyLines;
            GUILayout.Label(displayLines.ToString() + (hit.EffectiveBodyLines > 0 ? "*" : ""),
                EditorStyles.centeredGreyMiniLabel, GUILayout.Width(ColLines));

            DrawRiskBar(hit.LoadScore, ColRisk);
            DrawFlags(hit);

            GUILayout.Space(4);

            if (GUILayout.Button("→",
                    GUILayout.Width(ColPing - 4),
                    GUILayout.Height(RowHeight - 2)))
            {
                SelectHit(hit);
                PingScript(hit);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SelectHit(PollingHit hit)
        {
            if (_selectedHit != null) _selectedHit.IsSelected = false;
            hit.IsSelected = true;
            _selectedHit   = hit;
            Repaint();
        }

        private void DrawRiskBar(int score, float width)
        {
            Rect outer = GUILayoutUtility.GetRect(width, RowHeight - 4);
            outer.y      += 4;
            outer.height -= 4;

            EditorGUI.DrawRect(outer, new Color(0.15f, 0.15f, 0.15f));

            float frac = Mathf.Clamp01(score / 100f);

            Color barColor = score switch
            {
                < 30 => new Color(0.2f, 0.8f, 0.3f),
                < 60 => new Color(0.9f, 0.75f, 0.1f),
                < 85 => new Color(0.9f, 0.4f, 0.1f),
                _    => new Color(0.9f, 0.1f, 0.1f),
            };

            EditorGUI.DrawRect(
                new Rect(outer.x, outer.y, outer.width * frac, outer.height),
                barColor);

            _riskBarLabel.normal.textColor = Color.white;
            GUI.Label(outer,
                score switch
                {
                    < 30 => $"{score} LOW",
                    < 60 => $"{score} MED",
                    < 85 => $"{score} HIGH",
                    _    => $"{score} CRIT"
                },
                _riskBarLabel);
        }

        private void DrawFlags(PollingHit hit)
        {
            const float W = 18f;
            DrawFlag(hit.Flags, RiskFlags.Alloc,
                "A", new Color(0.9f, 0.5f, 0.1f), W,
                "ALLOCATION\n" +
                "new / Instantiate / GetComponent detected.\n\n" +
                "Creates heap garbage every frame → GC spikes.\n" +
                "Fix: cache references in Awake, use Object Pool.");

            DrawFlag(hit.Flags, RiskFlags.FindObjects,
                "F", new Color(0.9f, 0.2f, 0.2f), W,
                "FIND OBJECTS  ⛔ CRITICAL\n" +
                "FindObjectsOfType / GameObject.Find detected.\n\n" +
                "Scans ALL scene objects every frame. O(N) cost.\n" +
                "Fix: cache reference once in Awake/Start.");

            DrawFlag(hit.Flags, RiskFlags.SendMessage,
                "SM", new Color(0.8f, 0.3f, 0.8f), W,
                "SEND MESSAGE\n" +
                "SendMessage / BroadcastMessage detected.\n\n" +
                "Uses slow C# Reflection (10-100x slower than direct call).\n" +
                "Fix: use direct call, interface, or UnityEvent.");

            DrawFlag(hit.Flags, RiskFlags.PhysicsCast,
                "RC", new Color(0.3f, 0.6f, 0.9f), W,
                "PHYSICS CAST\n" +
                "Raycast / OverlapSphere / CapsuleCast detected.\n\n" +
                "Traverses physics BVH structure every frame.\n" +
                "Fix: reduce frequency via coroutine, use NonAlloc variants.");

            DrawFlag(hit.Flags, RiskFlags.Linq,
                "LQ", new Color(0.9f, 0.7f, 0.2f), W,
                "LINQ USAGE\n" +
                ".Where / .Select / .ToList detected.\n\n" +
                "Allocates enumerator objects on heap every frame.\n" +
                "Fix: replace with indexed for-loop, pre-cache results.");

            DrawFlag(hit.Flags, RiskFlags.EarlyReturn,
                "ER", new Color(0.3f, 0.9f, 0.4f), W,
                "EARLY RETURN  ✅ GOOD\n" +
                "Guard clause (if ... return/continue) detected.\n\n" +
                "Skips method body when conditions not met.\n" +
                "Keep it! Add more guard clauses at the top.");

            DrawFlag(hit.Flags, RiskFlags.NestedLoop,
                "N²", new Color(1f, 0.2f, 0.5f), W,
                "NESTED LOOP  ⛔ CRITICAL\n" +
                "Loop inside loop detected.\n\n" +
                "O(N²) complexity — quadratic cost per frame.\n" +
                "Fix: cache lookup structures (Dictionary), break early.");

            DrawFlag(hit.Flags, RiskFlags.Loop,
                "LP", new Color(0.5f, 0.5f, 1f), W,
                "LOOP\n" +
                "for / foreach / while detected.\n\n" +
                "Linear cost O(N) per frame. Acceptable for small N.\n" +
                "Fix: cap iteration count, use spatial data structures for large N.");

            DrawFlag(hit.Flags, RiskFlags.MethodCalls,
                "MC", new Color(0.6f, 0.8f, 0.6f), W,
                "METHOD CALLS\n" +
                "Calls other methods — real complexity may be hidden.\n\n" +
                "Check called methods for their own issues.\n" +
                "Score includes estimated effective line count.");
        }

        private void DrawFlag(RiskFlags flags, RiskFlags flag,
            string text, Color color, float width, string tooltip)
        {
            bool active = (flags & flag) != 0;

            if (!active)
            {
                GUILayout.Label(
                    new GUIContent(text, tooltip),
                    _flagLabelDim,
                    GUILayout.Width(width));
                return;
            }

            GUI.backgroundColor = color;
            GUILayout.Label(
                new GUIContent(text, tooltip),
                _flagLabelActive,
                GUILayout.Width(width));
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Detail Panel

        private void DrawDetailPanel(PollingHit hit)
        {
            EditorGUILayout.LabelField("📄  Detail", _detailTitle);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Script", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(hit.ScriptName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(hit.RelativePath, EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            KnownMethods.TryGetValue(hit.MethodName, out var meta);

            EditorGUILayout.LabelField("Method", EditorStyles.miniLabel);
            if (meta.AccentColor != default)
                _methodLabel.normal.textColor = meta.AccentColor;
            EditorGUILayout.LabelField(hit.MethodName, _methodLabel);

            if (meta.Description != null)
                EditorGUILayout.HelpBox(meta.Description, MessageType.None);

            EditorGUILayout.Space(4);
            int showLines = hit.EffectiveBodyLines > 0
                ? hit.EffectiveBodyLines
                : hit.MethodBodyLines;
            EditorGUILayout.LabelField(
                $"Line: {hit.MethodLine}    Body: {showLines} lines{(hit.EffectiveBodyLines > 0 ? " (effective)" : "")}" +
                (hit.CalledMethodsCount > 0 ? $"    Calls: {hit.CalledMethodsCount}" : ""),
                EditorStyles.miniLabel);

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Load Score Breakdown", _detailTitle);
            DrawRiskBar(hit.LoadScore, _detailWidth - 16);
            EditorGUILayout.Space(4);

            int effectiveLines = hit.EffectiveBodyLines > 0
                ? hit.EffectiveBodyLines
                : hit.MethodBodyLines;
            int basePts = meta.BaseWeight * 10;
            DrawScoreRow("Base weight (method type)",     basePts);
            DrawScoreRow("Body size (lines)",              BodySizeScore(effectiveLines));
            DrawScoreRow("Allocation / GetComponent",     (hit.Flags & RiskFlags.Alloc)       != 0 ? 15 : 0);
            DrawScoreRow("FindObjects",                    (hit.Flags & RiskFlags.FindObjects) != 0 ? 30 : 0);
            DrawScoreRow("SendMessage",                    (hit.Flags & RiskFlags.SendMessage) != 0 ? 15 : 0);
            DrawScoreRow("Physics Cast",                   (hit.Flags & RiskFlags.PhysicsCast) != 0 ? 10 : 0);
            DrawScoreRow("LINQ usage",                     (hit.Flags & RiskFlags.Linq)        != 0 ? 10 : 0);
            DrawScoreRow("String operations",              (hit.Flags & RiskFlags.StringOps)   != 0 ? 5  : 0);
            DrawScoreRow("Early Return (bonus −5)",        (hit.Flags & RiskFlags.EarlyReturn) != 0 ? -5 : 0);
            DrawScoreRow("Loop (O(N))",                    (hit.Flags & RiskFlags.Loop)       != 0 ? 8  : 0);
            DrawScoreRow("Nested Loop (O(N²))",            (hit.Flags & RiskFlags.NestedLoop) != 0 ? 20 : 0);
            DrawScoreRow("Method Calls (hidden cost)",     (hit.Flags & RiskFlags.MethodCalls) != 0 ? 5  : 0);

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("💡  Recommendations", _detailTitle);
            DrawRecommendations(hit);

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Open Script", GUILayout.Height(24)))
                OpenScriptAtLine(hit);

            if (GUILayout.Button("Ping in Project", GUILayout.Height(24)))
                PingScript(hit);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("📋  Summarize Problems", GUILayout.Height(28)))
                ScriptSummaryWindow.Show(hit);
        }

        private void DrawDetailEmpty()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                "Select a script\nfrom the list",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize  = 11,
                    wordWrap  = true,
                    alignment = TextAnchor.MiddleCenter,
                },
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
        }

        private static void DrawScoreRow(string label, int value)
        {
            if (value == 0) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  " + label, EditorStyles.miniLabel, GUILayout.MinWidth(180));

            Color c = value > 0
                ? (value >= 20 ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.8f, 0.3f))
                : new Color(0.3f, 1f, 0.4f);
            GUI.color = c;
            EditorGUILayout.LabelField(
                (value > 0 ? "+" : "") + value.ToString(),
                EditorStyles.boldLabel, GUILayout.Width(35));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRecommendations(PollingHit hit)
        {
            var f = hit.Flags;

            if ((f & RiskFlags.FindObjects) != 0)
                DrawRec("⛔ FindObjects in Update — cache the reference in Awake/Start.");

            if ((f & RiskFlags.Alloc) != 0)
                DrawRec("⚠ Avoid new/Instantiate/GetComponent in Update. " +
                         "Use Object Pool and cache components.");

            if ((f & RiskFlags.SendMessage) != 0)
                DrawRec("⚠ SendMessage uses reflection. " +
                         "Replace with direct method call or UnityEvent.");

            if ((f & RiskFlags.Linq) != 0)
                DrawRec("⚠ LINQ creates garbage (allocation). " +
                         "Replace with for-loop or pre-cache results.");

            if ((f & RiskFlags.StringOps) != 0)
                DrawRec("💡 String concatenation creates allocation. " +
                         "Use StringBuilder or cache.");

            if ((f & RiskFlags.PhysicsCast) != 0)
                DrawRec("💡 Physics casts are relatively expensive. " +
                         "Consider reducing check frequency via coroutine.");

            if ((f & RiskFlags.NestedLoop) != 0)
                DrawRec("⛔ Nested loop — O(N²) cost. Use Dictionary/HashSet to flatten.");

            if ((f & RiskFlags.Loop) != 0 && (f & RiskFlags.NestedLoop) == 0)
                DrawRec("⚠ Loop (O(N)) in Update. Cap iteration count or use spatial structures for large N.");

            if ((f & RiskFlags.MethodCalls) != 0)
                DrawRec("💡 Calls other methods — their cost adds up. Consider inlining trivial ones.");

            int bodyCheck = hit.EffectiveBodyLines > 0 ? hit.EffectiveBodyLines : hit.MethodBodyLines;
            if ((f & RiskFlags.EarlyReturn) == 0 && bodyCheck > 15)
                DrawRec("💡 Add Early Return at the start of the method " +
                         "to skip logic when conditions are not met.");

            if (hit.LoadScore < 30)
                DrawRec("✅ Method looks relatively safe.");
        }

        private static void DrawRec(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scanning

        private void RunScan()
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = new CancellationTokenSource();

            _isScanning  = true;
            _scanStatus  = $"Scanning {_scanFolder}…";
            _allHits.Clear();
            _filteredHits.Clear();
            _selectedHit = null;
            _cachedHighRiskCount = 0;
            _cachedScriptCount   = 0;
            Repaint();

            var token = _scanCts.Token;

            string projPath = Application.dataPath.Substring(
                0, Application.dataPath.Length - "Assets".Length);
            string absoluteScanRoot = Path.Combine(projPath, _scanFolder)
                .Replace('/', Path.DirectorySeparatorChar);

            Task.Run(() => ScanInBackground(absoluteScanRoot, token), token);
        }

        private void ScanInBackground(string root, CancellationToken token)
        {
            try
            {
                string[] csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

                var bag = new ConcurrentBag<PollingHit>();

                Parallel.ForEach(csFiles,
                    new ParallelOptions { CancellationToken = token },
                    file =>
                    {
                        token.ThrowIfCancellationRequested();

                        string fileName = Path.GetFileNameWithoutExtension(file);
                        if (fileName == "ScriptArchitecture" || fileName == "ScriptArchitectureWindow")
                            return;

                        string[] lines;
                        try { lines = File.ReadAllLines(file); }
                        catch { return; }

                        var hits = AnalyzeFile(file, lines);
                        foreach (var h in hits)
                            bag.Add(h);
                    });

                _projectMethods.Clear();
                Parallel.ForEach(csFiles,
                    new ParallelOptions { CancellationToken = token },
                    file =>
                    {
                        token.ThrowIfCancellationRequested();
                        string[] lines;
                        try { lines = File.ReadAllLines(file); }
                        catch { return; }
                        IndexAllMethods(file, lines);
                    });

                var result = bag.ToList();
                foreach (var h in result)
                    EnrichWithCallDepth(h);

                result.Sort((a, b) => b.LoadScore.CompareTo(a.LoadScore));

                int highRisk = 0;
                var names    = new HashSet<string>(result.Count);
                foreach (var h in result)
                {
                    names.Add(h.ScriptName);
                    if (h.LoadScore >= 60) highRisk++;
                }

                string status =
                    $"Done. {csFiles.Length} files, {result.Count} hits. " +
                    $"({DateTime.UtcNow:HH:mm:ss} UTC)";

                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;

                    _allHits.Clear();
                    _allHits.AddRange(result);

                    _cachedHighRiskCount = highRisk;
                    _cachedScriptCount   = names.Count;
                    _scanStatus          = status;
                    _isScanning          = false;

                    _filterDirty = true;
                    Repaint();
                };
            }
            catch (OperationCanceledException)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    _scanStatus = "Scan cancelled.";
                    _isScanning = false;
                    Repaint();
                };
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    _scanStatus = $"Error: {ex.Message}";
                    _isScanning = false;
                    Repaint();
                };
            }
        }

        private static List<PollingHit> AnalyzeFile(string fullPath, string[] lines)
        {
            string scriptName = Path.GetFileNameWithoutExtension(fullPath);

            string normalizedFull     = fullPath.Replace('\\', '/');
            string normalizedDataPath = Application.dataPath.Replace('\\', '/');

            string relPath;
            if (normalizedFull.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
                relPath = "Assets" + normalizedFull.Substring(normalizedDataPath.Length);
            else
                relPath = normalizedFull;

            var hits = new List<PollingHit>(4);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                string strippedLine = StripStringLiterals(line);

                foreach (var kvp in KnownMethods)
                {
                    if (!MatchesSignature(strippedLine, kvp.Key)) continue;

                    int bodyStart = FindOpenBrace(lines, i);
                    if (bodyStart < 0) break;

                    int bodyEnd = FindMatchingCloseBrace(lines, bodyStart);
                    int bodyLen = bodyEnd - bodyStart + 1;
                    if (bodyLen <= 0) break;

                    var hit = new PollingHit
                    {
                        ScriptName      = scriptName,
                        RelativePath    = relPath,
                        MethodName      = kvp.Key,
                        MethodLine      = i + 1,
                        MethodBodyLines = bodyLen,
                    };

                    AnalyzeBody(hit, lines, bodyStart, bodyEnd);
                    hit.LoadScore = CalculateScore(hit, kvp.Value);

                    hits.Add(hit);
                    break;
                }
            }

            return hits;
        }

        private static void IndexAllMethods(string fullPath, string[] lines)
        {
            string scriptName = Path.GetFileNameWithoutExtension(fullPath);

            for (int i = 0; i < lines.Length; i++)
            {
                string stripped = StripStringLiterals(lines[i]);

                int voidPos = IndexOfVoid(stripped, 0);
                if (voidPos < 0) continue;

                int nameStart = voidPos + 5;
                int len = stripped.Length;
                while (nameStart < len && stripped[nameStart] == ' ') nameStart++;

                int nameEnd = nameStart;
                while (nameEnd < len && (char.IsLetterOrDigit(stripped[nameEnd]) || stripped[nameEnd] == '_'))
                    nameEnd++;

                if (nameEnd <= nameStart) continue;

                string methodName = stripped.Substring(nameStart, nameEnd - nameStart);

                if (KnownMethods.ContainsKey(methodName)) continue;

                int bodyStart = FindOpenBrace(lines, i);
                if (bodyStart < 0) continue;

                int bodyEnd   = FindMatchingCloseBrace(lines, bodyStart);
                int bodyLines = bodyEnd - bodyStart + 1;

                var tempHit = new PollingHit
                {
                    ScriptName      = scriptName,
                    MethodName      = methodName,
                    MethodBodyLines = bodyLines,
                };
                AnalyzeBody(tempHit, lines, bodyStart, bodyEnd);

                string key = $"{scriptName}.{methodName}";
                _projectMethods.TryAdd(key,
                    new MethodInfo(scriptName, methodName, bodyLines, tempHit.Flags));
            }
        }

        private static void EnrichWithCallDepth(PollingHit hit)
        {
            if ((hit.Flags & RiskFlags.MethodCalls) != 0)
            {
                int extraLines = 0;
                RiskFlags extraFlags = RiskFlags.None;

                foreach (var kv in _projectMethods)
                {
                    if (!kv.Key.StartsWith(hit.ScriptName + ".", StringComparison.Ordinal))
                        continue;

                    extraLines += kv.Value.BodyLines;
                    extraFlags |= kv.Value.Flags;
                }

                hit.EffectiveBodyLines = hit.MethodBodyLines + extraLines;
                hit.Flags             |= extraFlags;
            }
            else
            {
                hit.EffectiveBodyLines = hit.MethodBodyLines;
            }
        }

        private static bool MatchesSignature(string line, string methodName)
        {
            int len = line.Length;
            int i   = 0;

            while (i < len && (line[i] == ' ' || line[i] == '\t')) i++;

            if (i + 1 < len && line[i] == '/' && line[i + 1] == '/') return false;
            if (i < len && line[i] == '*') return false;

            int voidPos = IndexOfVoid(line, i);
            if (voidPos < 0) return false;

            int nameStart = voidPos + 5;
            while (nameStart < len && line[nameStart] == ' ') nameStart++;

            int mNameLen = methodName.Length;
            if (nameStart + mNameLen >= len) return false;

            for (int j = 0; j < mNameLen; j++)
            {
                if (line[nameStart + j] != methodName[j]) return false;
            }

            int afterName = nameStart + mNameLen;
            while (afterName < len && line[afterName] == ' ') afterName++;

            return afterName < len && line[afterName] == '(';
        }

        private static int IndexOfVoid(string line, int start)
        {
            for (int i = start; i < line.Length - 4; i++)
            {
                if (line[i] == 'v' && line[i + 1] == 'o' &&
                    line[i + 2] == 'i' && line[i + 3] == 'd' &&
                    (i + 4 >= line.Length || line[i + 4] == ' ' || line[i + 4] == '('))
                {
                    if (i > 0 && char.IsLetterOrDigit(line[i - 1])) continue;
                    return i;
                }
            }
            return -1;
        }

        private static int FindOpenBrace(string[] lines, int fromLine)
        {
            int limit = Mathf.Min(lines.Length, fromLine + 5);
            for (int i = fromLine; i < limit; i++)
            {
                string line = lines[i];
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    if (c == '/' && j + 1 < line.Length && line[j + 1] == '/')
                        break;
                    if (c == '{') return i;
                }
            }
            return -1;
        }

        private static int FindMatchingCloseBrace(string[] lines, int openBraceLine)
        {
            int  depth   = 0;
            bool inStr   = false;
            bool inChar  = false;
            bool escaped = false;

            for (int i = openBraceLine; i < lines.Length; i++)
            {
                string line = lines[i];
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];

                    if (escaped) { escaped = false; continue; }
                    if (c == '\\') { escaped = true; continue; }

                    if (inStr)
                    {
                        if (c == '"') inStr = false;
                        continue;
                    }
                    if (inChar)
                    {
                        if (c == '\'') inChar = false;
                        continue;
                    }

                    if (c == '/' && j + 1 < line.Length && line[j + 1] == '/')
                        break;

                    if (c == '"')  { inStr = true;  continue; }
                    if (c == '\'') { inChar = true; continue; }
                    if (c == '{')  depth++;
                    if (c == '}')  { depth--; if (depth <= 0) return i; }
                }
            }

            return Mathf.Min(openBraceLine + 500, lines.Length - 1);
        }

        private static void AnalyzeBody(PollingHit hit, string[] lines, int startLine, int endLine)
        {
            RiskFlags flags    = RiskFlags.None;
            int       loopDepth    = 0;
            int       maxLoopDepth = 0;

            for (int i = startLine; i <= endLine && i < lines.Length; i++)
            {
                string line = lines[i];

                int trimmed = 0;
                while (trimmed < line.Length &&
                       (line[trimmed] == ' ' || line[trimmed] == '\t'))
                    trimmed++;

                if (trimmed + 1 < line.Length &&
                    line[trimmed] == '/' && line[trimmed + 1] == '/')
                    continue;

                if (trimmed < line.Length && line[trimmed] == '*')
                    continue;

                string stripped = StripStringLiterals(line);

                if (LoopRx.IsMatch(stripped))
                {
                    loopDepth++;
                    if (loopDepth > maxLoopDepth) maxLoopDepth = loopDepth;
                    flags |= RiskFlags.Loop;
                    if (loopDepth >= 2) flags |= RiskFlags.NestedLoop;
                }

                foreach (char c in stripped)
                    if (c == '{') { }

                if (MethodCallRx.IsMatch(stripped))
                    flags |= RiskFlags.MethodCalls;

                if (!MayContainRisk(stripped)) continue;

                if ((flags & RiskFlags.Alloc)      == 0 && AllocRx.IsMatch(stripped))      flags |= RiskFlags.Alloc;
                if ((flags & RiskFlags.FindObjects) == 0 && FindRx.IsMatch(stripped))       flags |= RiskFlags.FindObjects;
                if ((flags & RiskFlags.SendMessage) == 0 && SendRx.IsMatch(stripped))       flags |= RiskFlags.SendMessage;
                if ((flags & RiskFlags.PhysicsCast) == 0 && PhysicsRx.IsMatch(stripped))    flags |= RiskFlags.PhysicsCast;
                if ((flags & RiskFlags.StringOps)   == 0 && StringRx.IsMatch(stripped))     flags |= RiskFlags.StringOps;
                if ((flags & RiskFlags.Linq)        == 0 && LinqRx.IsMatch(stripped))       flags |= RiskFlags.Linq;
                if ((flags & RiskFlags.EarlyReturn) == 0 && EarlyReturnRx.IsMatch(stripped))flags |= RiskFlags.EarlyReturn;

                if (flags == (RiskFlags)0x3FF) break;
            }

            hit.Flags = flags;
        }

        private static string StripStringLiterals(string line)
        {
            if (line.IndexOf('"') < 0) return line;

            var sb = new System.Text.StringBuilder(line.Length);
            int i = 0;
            int len = line.Length;

            while (i < len)
            {
                char c = line[i];

                if (c == '/' && i + 1 < len && line[i + 1] == '/')
                {
                    sb.Append(' ', len - i);
                    break;
                }

                if (c == '@' && i + 1 < len && line[i + 1] == '"')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < len)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < len && line[i + 1] == '"')
                            {
                                sb.Append("  ");
                                i += 2;
                                continue;
                            }
                            sb.Append(' ');
                            i++;
                            break;
                        }
                        sb.Append(' ');
                        i++;
                    }
                    continue;
                }

                if (c == '"')
                {
                    sb.Append(' ');
                    i++;
                    while (i < len)
                    {
                        char sc = line[i];
                        if (sc == '\\')
                        {
                            sb.Append("  ");
                            i += 2;
                            continue;
                        }
                        if (sc == '"')
                        {
                            sb.Append(' ');
                            i++;
                            break;
                        }
                        sb.Append(' ');
                        i++;
                    }
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private static bool MayContainRisk(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == 'n' || c == 'N' ||
                    c == 'g' || c == 'G' ||
                    c == 's' || c == 'S' ||
                    c == 'p' || c == 'P' ||
                    c == 'i' || c == 'I' ||
                    c == 'r' || c == 'R' ||
                    c == '.')
                    return true;
            }
            return false;
        }

        private static int CalculateScore(PollingHit hit, PollingMethodMeta meta)
        {
            int score = meta.BaseWeight * 10;

            int effectiveLines = hit.EffectiveBodyLines > 0
                ? hit.EffectiveBodyLines
                : hit.MethodBodyLines;

            score += BodySizeScore(effectiveLines);

            var f = hit.Flags;
            if ((f & RiskFlags.Alloc)       != 0) score += 15;
            if ((f & RiskFlags.FindObjects) != 0) score += 30;
            if ((f & RiskFlags.SendMessage) != 0) score += 15;
            if ((f & RiskFlags.PhysicsCast) != 0) score += 10;
            if ((f & RiskFlags.Linq)        != 0) score += 10;
            if ((f & RiskFlags.StringOps)   != 0) score += 5;
            if ((f & RiskFlags.EarlyReturn) != 0) score -= 5;

            if ((f & RiskFlags.NestedLoop)   != 0) score += 20;
            else if ((f & RiskFlags.Loop)    != 0) score += 8;
            if ((f & RiskFlags.MethodCalls)  != 0) score += 5;

            return Mathf.Clamp(score, 0, 100);
        }

        private static int BodySizeScore(int lines)
        {
            if (lines <= 5)  return 0;
            if (lines <= 15) return 5;
            if (lines <= 30) return 10;
            if (lines <= 60) return 15;
            return 20;
        }

        #endregion

        #region Filter & Sort

        private void ApplyFilterAndSort()
        {
            _filteredHits.Clear();

            foreach (var hit in _allHits)
            {
                if (_methodFilter.Count > 0 && !_methodFilter.Contains(hit.MethodName))
                    continue;

                if (_methodFilter.Count == 0)
                    continue;

                if (_onlyHighRisk && hit.LoadScore < 60)
                    continue;

                if (_searchQuery.Length > 0)
                {
                    string q = _searchQuery;
                    if (!ContainsIgnoreCase(hit.ScriptName, q) &&
                        !ContainsIgnoreCase(hit.MethodName, q) &&
                        !ContainsIgnoreCase(hit.RelativePath, q))
                        continue;
                }

                _filteredHits.Add(hit);
            }

            switch (_sortMode)
            {
                case SortMode.LoadScore:
                    _filteredHits.Sort(_sortAsc ? ComparisonAscScore  : ComparisonDescScore);
                    break;
                case SortMode.Name:
                    _filteredHits.Sort(_sortAsc ? ComparisonAscName   : ComparisonDescName);
                    break;
                case SortMode.Method:
                    _filteredHits.Sort(_sortAsc ? ComparisonAscMethod : ComparisonDescMethod);
                    break;
                case SortMode.LineCount:
                    _filteredHits.Sort(_sortAsc ? ComparisonAscLines  : ComparisonDescLines);
                    break;
            }
        }

        private static readonly Comparison<PollingHit> ComparisonDescScore =
            (a, b) => b.LoadScore.CompareTo(a.LoadScore);
        private static readonly Comparison<PollingHit> ComparisonAscScore =
            (a, b) => a.LoadScore.CompareTo(b.LoadScore);
        private static readonly Comparison<PollingHit> ComparisonDescName =
            (a, b) => string.Compare(b.ScriptName, a.ScriptName, StringComparison.Ordinal);
        private static readonly Comparison<PollingHit> ComparisonAscName =
            (a, b) => string.Compare(a.ScriptName, b.ScriptName, StringComparison.Ordinal);
        private static readonly Comparison<PollingHit> ComparisonDescMethod =
            (a, b) => string.Compare(b.MethodName, a.MethodName, StringComparison.Ordinal);
        private static readonly Comparison<PollingHit> ComparisonAscMethod =
            (a, b) => string.Compare(a.MethodName, b.MethodName, StringComparison.Ordinal);
        private static readonly Comparison<PollingHit> ComparisonDescLines =
            (a, b) => b.MethodBodyLines.CompareTo(a.MethodBodyLines);
        private static readonly Comparison<PollingHit> ComparisonAscLines =
            (a, b) => a.MethodBodyLines.CompareTo(b.MethodBodyLines);

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region Helpers

        private static void OpenScriptAtLine(PollingHit hit)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(hit.RelativePath);
            if (asset != null)
                AssetDatabase.OpenAsset(asset, hit.MethodLine);
        }

        private static void PingScript(PollingHit hit)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(hit.RelativePath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        private void ShowFolderMenu()
        {
            string selected = EditorUtility.OpenFolderPanel(
                "Select Scan Folder",
                _scanFolder,
                "");

            if (string.IsNullOrEmpty(selected)) return;

            string dataPath = Application.dataPath;
            string projPath = dataPath.Substring(0, dataPath.Length - "Assets".Length);

            if (selected.StartsWith(projPath, StringComparison.OrdinalIgnoreCase))
            {
                string relative = selected.Substring(projPath.Length).Replace('\\', '/');

                if (relative.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    _scanFolder  = relative;
                    _filterDirty = true;
                    ShowNotification(new GUIContent($"Scan folder: {_scanFolder}"));
                }
                else
                {
                    ShowNotification(new GUIContent("⚠ Please select a folder inside Assets/"));
                }
            }
            else
            {
                ShowNotification(new GUIContent("⚠ Selected folder is outside the project!"));
            }
        }

        #endregion

        #region Styles & Prefs

        private void InitStyles()
        {
            if (_stylesReady) return;

            _rowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(2, 2, 1, 1),
                margin  = new RectOffset(0, 0, 0, 0),
            };
            _rowStyle.normal.background = CreateOwnedTexture(new Color(0.22f, 0.22f, 0.22f));

            _rowAltStyle = new GUIStyle(_rowStyle);
            _rowAltStyle.normal.background = CreateOwnedTexture(new Color(0.19f, 0.19f, 0.19f));

            _rowSelectedStyle = new GUIStyle(_rowStyle);
            _rowSelectedStyle.normal.background = CreateOwnedTexture(new Color(0.17f, 0.36f, 0.53f));

            _headerStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(4, 4, 3, 3),
            };
            _headerStyle.normal.background = CreateOwnedTexture(new Color(0.15f, 0.15f, 0.15f));

            _scriptNameNormal = new GUIStyle(EditorStyles.label);

            _scriptNameHighRisk = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
            };
            _scriptNameHighRisk.normal.textColor = new Color(1f, 0.6f, 0.3f);

            _methodLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
            };

            _riskBarLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontStyle = FontStyle.Bold,
            };
            _riskBarLabel.normal.textColor = Color.white;

            _flagLabelDim = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            _flagLabelDim.normal.textColor = new Color(1f, 1f, 1f, 0.12f);

            _flagLabelActive = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _flagLabelActive.normal.textColor = Color.white;

            _detailTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
            };

            _stylesReady = true;
        }

        private Texture2D CreateOwnedTexture(Color color)
        {
            var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _ownedTextures.Add(tex);
            return tex;
        }

        private void DestroyOwnedTextures()
        {
            for (int i = 0; i < _ownedTextures.Count; i++)
            {
                if (_ownedTextures[i] != null)
                    DestroyImmediate(_ownedTextures[i]);
            }
            _ownedTextures.Clear();
        }

        private void LoadPrefs()
        {
            _sortMode     = (SortMode)EditorPrefs.GetInt(PrefPrefix + "SortMode", (int)SortMode.LoadScore);
            _sortAsc      = EditorPrefs.GetBool(PrefPrefix + "SortAsc", false);
            _onlyHighRisk = EditorPrefs.GetBool(PrefPrefix + "HighRisk", false);
            _showDetail   = EditorPrefs.GetBool(PrefPrefix + "ShowDetail", true);

            string saved = EditorPrefs.GetString(PrefPrefix + "MethodFilter", "");
            if (!string.IsNullOrEmpty(saved))
            {
                _methodFilter.Clear();
                foreach (var s in saved.Split(','))
                    if (!string.IsNullOrWhiteSpace(s))
                        _methodFilter.Add(s.Trim());
            }

            _scanFolder = EditorPrefs.GetString(PrefPrefix + "ScanFolder", "Assets");
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefPrefix  + "SortMode",  (int)_sortMode);
            EditorPrefs.SetBool(PrefPrefix + "SortAsc",   _sortAsc);
            EditorPrefs.SetBool(PrefPrefix + "HighRisk",  _onlyHighRisk);
            EditorPrefs.SetBool(PrefPrefix + "ShowDetail", _showDetail);
            EditorPrefs.SetString(PrefPrefix + "MethodFilter",
                string.Join(",", _methodFilter));
            EditorPrefs.SetString(PrefPrefix + "ScanFolder", _scanFolder);
        }

        #endregion
    }

    // ── ScriptSummaryWindow ──────────────────────────────────────────────────────

    public class ScriptSummaryWindow : EditorWindow
    {
        private PollingHit _hit;
        private Vector2    _scroll;

        private string     _fullSummaryText = "";
        private bool       _summaryDirty    = true;

        private GUIStyle _titleStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _labelBold;
        private GUIStyle _selectableLabel;
        private GUIStyle _selectableCode;
        private bool     _stylesReady;

        public static void Show(PollingHit hit)
        {
            var win = CreateInstance<ScriptSummaryWindow>();
            win.titleContent  = new GUIContent($"Summary — {hit.ScriptName}.{hit.MethodName}");
            win._hit          = hit;
            win._summaryDirty = true;

            const float w = 520f;
            const float h = 640f;
            var res = Screen.currentResolution;
            win.position = new Rect(
                (res.width  - w) * 0.5f,
                (res.height - h) * 0.5f,
                w, h);

            win.minSize = new Vector2(420f, 400f);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            InitStyles();
            if (_hit == null) { Close(); return; }

            if (_summaryDirty)
            {
                _fullSummaryText = BuildSummaryText();
                _summaryDirty    = false;
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown &&
                e.keyCode == KeyCode.A &&
                (e.control || e.command))
            {
                GUIUtility.systemCopyBuffer = _fullSummaryText;
                ShowNotification(new GUIContent("✔ All text copied!"));
                e.Use();
            }

            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawBody();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📋 Copy All", GUILayout.Width(90), GUILayout.Height(26)))
            {
                GUIUtility.systemCopyBuffer = _fullSummaryText;
                ShowNotification(new GUIContent("Copied to clipboard!"));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Script", GUILayout.Width(110), GUILayout.Height(26)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(_hit.RelativePath);
                if (asset != null) AssetDatabase.OpenAsset(asset, _hit.MethodLine);
            }

            if (GUILayout.Button("Close", GUILayout.Width(80), GUILayout.Height(26)))
                Close();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            string verdict;
            Color  verdictColor;
            switch (_hit.LoadScore)
            {
                case < 30:
                    verdict      = "✅  LOW RISK — Method looks clean.";
                    verdictColor = new Color(0.3f, 1f, 0.4f);
                    break;
                case < 60:
                    verdict      = "⚠️  MEDIUM RISK — Some issues worth reviewing.";
                    verdictColor = new Color(1f, 0.85f, 0.2f);
                    break;
                case < 85:
                    verdict      = "🔶  HIGH RISK — Refactoring recommended.";
                    verdictColor = new Color(1f, 0.5f, 0.1f);
                    break;
                default:
                    verdict      = "🔴  CRITICAL — Significant performance impact!";
                    verdictColor = new Color(1f, 0.2f, 0.2f);
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(
                $"{_hit.ScriptName}.cs",
                _titleStyle,
                GUILayout.Height(22));
            GUILayout.FlexibleSpace();
            var scoreStyle = new GUIStyle(_titleStyle)
                { normal = { textColor = verdictColor } };
            EditorGUILayout.SelectableLabel(
                $"Score: {_hit.LoadScore}",
                scoreStyle,
                GUILayout.Width(90), GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();

            int showEff = _hit.EffectiveBodyLines > 0 ? _hit.EffectiveBodyLines : _hit.MethodBodyLines;
            EditorGUILayout.SelectableLabel(
                $"void {_hit.MethodName}()   Line: {_hit.MethodLine}   " +
                $"Body: {showEff} lines{(_hit.EffectiveBodyLines > 0 ? " (effective)" : "")}",
                EditorStyles.miniLabel,
                GUILayout.Height(16));

            EditorGUILayout.Space(4);

            GUI.color = verdictColor;
            EditorGUILayout.SelectableLabel(verdict, _selectableLabel,
                GUILayout.Height(20));
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private void DrawBody()
        {
            var  f   = _hit.Flags;
            bool any = false;

            EditorGUILayout.LabelField("⚑  Detected Issues", _labelBold);
            EditorGUILayout.Space(4);

            any |= TryDrawTag(f, RiskFlags.FindObjects,
                "F  — FindObjects",
                new Color(0.9f, 0.2f, 0.2f),
                "FindObjectsOfType / GameObject.Find / FindWithTag",
                "Scans ALL objects in the scene every single frame.\n" +
                "Cost is O(N) where N = total scene object count.\n" +
                "On large scenes this alone can cost several milliseconds per frame.",
                "❌ Don't:\n  var e = FindObjectOfType<Enemy>();  // in Update\n\n" +
                "✅ Do:\n  // In Awake or Start:\n  _enemy = FindObjectOfType<Enemy>();\n" +
                "  // In Update:\n  _enemy.DoSomething();");

            any |= TryDrawTag(f, RiskFlags.Alloc,
                "A  — Allocation",
                new Color(0.9f, 0.5f, 0.1f),
                "new T() / Instantiate() / GetComponent<T>()",
                "Creates heap objects → Garbage Collector pressure → GC spikes.\n" +
                "GetComponent() traverses the component list on every call.\n" +
                "Visible as frame hitches / stuttering, especially on mobile.",
                "❌ Don't:\n  void Update() {\n    var rb = GetComponent<Rigidbody2D>();\n    var list = new List<int>();\n  }\n\n" +
                "✅ Do:\n  Rigidbody2D _rb;\n  void Awake() => _rb = GetComponent<Rigidbody2D>();\n" +
                "  // Use Object Pool for Instantiate");

            any |= TryDrawTag(f, RiskFlags.SendMessage,
                "SM — SendMessage",
                new Color(0.8f, 0.3f, 0.8f),
                "SendMessage() / BroadcastMessage() / SendMessageUpwards()",
                "Uses C# Reflection to find method by name string at runtime.\n" +
                "Reflection is 10–100× slower than a direct method call.\n" +
                "Also silently fails if the method name is misspelled.",
                "❌ Don't:\n  gameObject.SendMessage(\"TakeDamage\", 10);\n\n" +
                "✅ Do (direct call):\n  target.TakeDamage(10);\n\n" +
                "✅ Do (interface):\n  ((IDamageable)target).TakeDamage(10);\n\n" +
                "✅ Do (UnityEvent):\n  onHit.Invoke();");

            any |= TryDrawTag(f, RiskFlags.PhysicsCast,
                "RC — Physics Cast",
                new Color(0.3f, 0.6f, 0.9f),
                "Physics.Raycast / OverlapSphere / OverlapBox / CapsuleCast etc.",
                "Physics queries traverse the BVH acceleration structure every call.\n" +
                "Expensive when called multiple times per frame or with large radii.\n" +
                "Non-alloc variants still have the traversal cost.",
                "❌ Don't:\n  void Update() { Physics.Raycast(transform.position, Vector3.down); }\n\n" +
                "✅ Do (reduce frequency):\n  IEnumerator CheckLoop() {\n    while(true) {\n      DoRaycast();\n      yield return new WaitForSeconds(0.1f);\n    }\n  }\n\n" +
                "✅ Do (NonAlloc):\n  Physics.RaycastNonAlloc(ray, _resultsBuffer);");

            any |= TryDrawTag(f, RiskFlags.Linq,
                "LQ — LINQ",
                new Color(0.9f, 0.7f, 0.2f),
                ".Where() / .Select() / .ToList() / .ToArray() / .Any() / .Count()",
                "Every LINQ call allocates enumerator objects on the managed heap.\n" +
                ".ToList() / .ToArray() allocate new collections every frame.\n" +
                "All of this creates garbage that the GC must eventually collect.",
                "❌ Don't:\n  void Update() {\n    var alive = _units.Where(u => u.IsAlive).ToList();\n  }\n\n" +
                "✅ Do:\n  // Pre-filter and cache, or use indexed for-loop:\n" +
                "  for (int i = 0; i < _units.Count; i++) {\n    if (!_units[i].IsAlive) continue;\n    // ...\n  }");

            any |= TryDrawTag(f, RiskFlags.StringOps,
                "ST — String Operations",
                new Color(0.7f, 0.7f, 0.3f),
                "string.Format() / ToString() / string concatenation (+)",
                "Strings are immutable in C#.\n" +
                "Every concatenation or format call creates a new string object on the heap.\n" +
                "In Update this happens every frame — constant GC pressure.",
                "❌ Don't:\n  void Update() {\n    _label.text = \"Score: \" + _score;\n  }\n\n" +
                "✅ Do (dirty flag):\n  void Update() {\n    if (_scoreDirty) {\n      _label.text = _scoreStr;\n      _scoreDirty = false;\n    }\n  }\n  void AddScore(int v) {\n    _score += v;\n    _scoreStr = \"Score: \" + _score;\n    _scoreDirty = true;\n  }");

            any |= TryDrawTag(f, RiskFlags.EarlyReturn,
                "ER — Early Return  ✅",
                new Color(0.3f, 0.9f, 0.4f),
                "if (condition) return / continue detected",
                "GOOD PRACTICE.\n" +
                "Guard clauses at the top of Update skip the rest of the method body\n" +
                "when conditions are not met, saving CPU time every frame.\n" +
                "Unity still calls the method, but it exits immediately.",
                "✅ Keep it! Add more guard clauses:\n\n" +
                "  void Update() {\n    if (!_isActive)     return;\n" +
                "    if (_target == null)  return;\n" +
                "    if (Time.timeScale == 0f) return;\n" +
                "    // ... rest of logic\n  }");

            if (!any)
            {
                EditorGUILayout.SelectableLabel(
                    "No risk flags detected. Method body appears clean.",
                    _selectableLabel,
                    GUILayout.Height(20));
            }

            int warnBody = _hit.EffectiveBodyLines > 0 ? _hit.EffectiveBodyLines : _hit.MethodBodyLines;
            if (warnBody > 30)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    $"Method body is {_hit.MethodBodyLines} lines long." +
                    (_hit.EffectiveBodyLines > 0 ? $" (effective: {_hit.EffectiveBodyLines})" : "") + "\n" +
                    "Consider splitting into smaller private methods for readability and testability.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.SelectableLabel(
                $"📍  {_hit.RelativePath}  :  line {_hit.MethodLine}",
                EditorStyles.miniLabel,
                GUILayout.Height(16));
            EditorGUILayout.EndHorizontal();
        }

        private bool TryDrawTag(RiskFlags flags, RiskFlags flag,
            string title, Color color,
            string what, string why, string fix)
        {
            if ((flags & flag) == 0) return false;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.color = color;
            EditorGUILayout.SelectableLabel(title, _tagStyle, GUILayout.Height(20));
            GUI.color = Color.white;

            EditorGUILayout.Space(2);

            EditorGUILayout.LabelField("WHAT:", _labelBold);
            EditorGUILayout.SelectableLabel(what, _selectableLabel,
                GUILayout.Height(CalcSelectableHeight(what, _selectableLabel)));

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("WHY:", _labelBold);
            EditorGUILayout.SelectableLabel(why, _selectableLabel,
                GUILayout.Height(CalcSelectableHeight(why, _selectableLabel)));

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("HOW TO FIX:", _labelBold);
            DrawSelectableCode(fix);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);

            return true;
        }

        private string BuildSummaryText()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== Script Architecture Summary ===");
            sb.AppendLine();
            sb.AppendLine($"Script  : {_hit.ScriptName}.cs");
            sb.AppendLine($"Method  : void {_hit.MethodName}()");
            sb.AppendLine($"Line    : {_hit.MethodLine}");
            int effectiveLines = _hit.EffectiveBodyLines > 0
                ? _hit.EffectiveBodyLines
                : _hit.MethodBodyLines;
            sb.AppendLine($"Body    : {_hit.MethodBodyLines} lines" +
                (_hit.EffectiveBodyLines > 0 ? $" (effective: {_hit.EffectiveBodyLines})" : ""));
            sb.AppendLine($"Path    : {_hit.RelativePath}");
            sb.AppendLine();

            string verdict = _hit.LoadScore switch
            {
                < 30 => "LOW RISK — Method looks clean.",
                < 60 => "MEDIUM RISK — Some issues worth reviewing.",
                < 85 => "HIGH RISK — Refactoring recommended.",
                _    => "CRITICAL — Significant performance impact!"
            };
            sb.AppendLine($"Load Score : {_hit.LoadScore} / 100");
            sb.AppendLine($"Verdict    : {verdict}");
            sb.AppendLine();

            sb.AppendLine("--- Load Score Breakdown ---");
            AppendScoreRow(sb, "Base weight (method type)",
                GetBaseScore(_hit.MethodName));
            AppendScoreRow(sb, "Body size (lines)",
                BodySizeScore(_hit.MethodBodyLines));
            AppendScoreRow(sb, "Allocation / GetComponent",
                (_hit.Flags & RiskFlags.Alloc)       != 0 ? 15 : 0);
            AppendScoreRow(sb, "FindObjects",
                (_hit.Flags & RiskFlags.FindObjects) != 0 ? 30 : 0);
            AppendScoreRow(sb, "SendMessage",
                (_hit.Flags & RiskFlags.SendMessage) != 0 ? 15 : 0);
            AppendScoreRow(sb, "Physics Cast",
                (_hit.Flags & RiskFlags.PhysicsCast) != 0 ? 10 : 0);
            AppendScoreRow(sb, "LINQ usage",
                (_hit.Flags & RiskFlags.Linq)        != 0 ? 10 : 0);
            AppendScoreRow(sb, "String operations",
                (_hit.Flags & RiskFlags.StringOps)   != 0 ?  5 : 0);
            AppendScoreRow(sb, "Early Return (bonus)",
                (_hit.Flags & RiskFlags.EarlyReturn) != 0 ? -5 : 0);
            AppendScoreRow(sb, "Loop (O(N))",
                (_hit.Flags & RiskFlags.Loop)       != 0 ? 8  : 0);
            AppendScoreRow(sb, "Nested Loop (O(N²))",
                (_hit.Flags & RiskFlags.NestedLoop) != 0 ? 20 : 0);
            AppendScoreRow(sb, "Method Calls (hidden cost)",
                (_hit.Flags & RiskFlags.MethodCalls) != 0 ? 5  : 0);
            sb.AppendLine();

            sb.AppendLine("--- Detected Issues ---");
            var f = _hit.Flags;

            AppendIssue(sb, f, RiskFlags.FindObjects,
                "F — FindObjects",
                "FindObjectsOfType / GameObject.Find / FindWithTag",
                "Scans ALL objects in the scene every single frame.\n" +
                "Cost is O(N) where N = total scene object count.\n" +
                "On large scenes this alone can cost several milliseconds per frame.",
                "Cache the reference once in Awake/Start instead of calling Find in Update.");

            AppendIssue(sb, f, RiskFlags.Alloc,
                "A — Allocation",
                "new T() / Instantiate() / GetComponent<T>()",
                "Creates heap objects → Garbage Collector pressure → GC spikes.\n" +
                "GetComponent() traverses the component list on every call.",
                "Cache component references in Awake. Use Object Pool for Instantiate.");

            AppendIssue(sb, f, RiskFlags.SendMessage,
                "SM — SendMessage",
                "SendMessage() / BroadcastMessage() / SendMessageUpwards()",
                "Uses C# Reflection — 10–100x slower than a direct method call.\n" +
                "Silently fails if the method name is misspelled.",
                "Use direct call, interface (IDamageable), or UnityEvent instead.");

            AppendIssue(sb, f, RiskFlags.PhysicsCast,
                "RC — Physics Cast",
                "Physics.Raycast / OverlapSphere / CapsuleCast etc.",
                "Traverses the BVH acceleration structure every call.\n" +
                "Expensive when called multiple times per frame.",
                "Reduce frequency via coroutine. Use NonAlloc variants (RaycastNonAlloc).");

            AppendIssue(sb, f, RiskFlags.Linq,
                "LQ — LINQ",
                ".Where() / .Select() / .ToList() / .ToArray() / .Any() / .Count()",
                "Every LINQ call allocates enumerator objects on the managed heap.\n" +
                ".ToList() / .ToArray() allocate new collections every frame.",
                "Replace with indexed for-loop or pre-cache filtered results.");

            AppendIssue(sb, f, RiskFlags.StringOps,
                "ST — String Operations",
                "string.Format() / ToString() / string concatenation (+)",
                "Strings are immutable. Every concatenation creates a new heap object.\n" +
                "In Update this happens every frame — constant GC pressure.",
                "Use a dirty-flag pattern: update the string only when the value changes.");

            AppendIssue(sb, f, RiskFlags.EarlyReturn,
                "ER — Early Return  (GOOD)",
                "if (condition) return / continue detected",
                "Guard clauses skip method body when conditions are not met.",
                "Keep it! Add more guard clauses at the top of the method.");

            AppendIssue(sb, f, RiskFlags.NestedLoop,
                "N² — Nested Loop  (CRITICAL)",
                "Loop inside loop detected",
                "O(N²) complexity — quadratic cost per frame.",
                "Use Dictionary/HashSet to flatten nested loops.");

            AppendIssue(sb, f, RiskFlags.Loop,
                "LP — Loop",
                "for / foreach / while detected",
                "Linear cost O(N) per frame. Acceptable for small N.",
                "Cap iteration count. Use spatial data structures for large collections.");

            AppendIssue(sb, f, RiskFlags.MethodCalls,
                "MC — Method Calls",
                "Calls other methods",
                "Real complexity may be hidden in called methods.",
                "Check called methods for allocations and loops.");

            if (f == RiskFlags.None)
                sb.AppendLine("No risk flags detected. Method body appears clean.");

            int bodyWarning = _hit.EffectiveBodyLines > 0 ? _hit.EffectiveBodyLines : _hit.MethodBodyLines;
            if (bodyWarning > 30)
            {
                sb.AppendLine();
                sb.AppendLine($"[WARNING] Method body is {_hit.MethodBodyLines} lines long." +
                    (_hit.EffectiveBodyLines > 0 ? $" (effective: {_hit.EffectiveBodyLines})" : ""));
                sb.AppendLine("Consider splitting into smaller private methods.");
            }

            return sb.ToString();
        }

        private static void AppendScoreRow(System.Text.StringBuilder sb, string label, int value)
        {
            if (value == 0) return;
            string sign = value > 0 ? "+" : "";
            sb.AppendLine($"  {label,-38} {sign}{value}");
        }

        private static void AppendIssue(System.Text.StringBuilder sb,
            RiskFlags flags, RiskFlags flag,
            string title, string what, string why, string fix)
        {
            if ((flags & flag) == 0) return;

            sb.AppendLine();
            sb.AppendLine($"[{title}]");
            sb.AppendLine($"WHAT : {what}");
            sb.AppendLine($"WHY  : {why}");
            sb.AppendLine($"FIX  : {fix}");
        }

        private static int GetBaseScore(string methodName)
        {
            return methodName switch
            {
                "Update"             => 30,
                "LateUpdate"         => 30,
                "FixedUpdate"        => 40,
                "OnGUI"              => 50,
                "OnAnimatorMove"     => 30,
                "OnAnimatorIK"       => 30,
                "OnRenderObject"     => 40,
                "OnWillRenderObject" => 40,
                _                    => 0
            };
        }

        private static int BodySizeScore(int lines)
        {
            if (lines <= 5)  return 0;
            if (lines <= 15) return 5;
            if (lines <= 30) return 10;
            if (lines <= 60) return 15;
            return 20;
        }

        private float CalcSelectableHeight(string text, GUIStyle style)
        {
            float availableWidth = position.width - DetailPadding;
            return Mathf.Max(
                style.CalcHeight(new GUIContent(text), availableWidth),
                16f);
        }

        private const float DetailPadding = 40f;

        private void DrawSelectableCode(string code)
        {
            float h = _selectableCode.CalcHeight(
                new GUIContent(code),
                position.width - DetailPadding);
            h = Mathf.Max(h, 32f);

            EditorGUI.BeginDisabledGroup(false);
            string result = EditorGUILayout.TextArea(code, _selectableCode,
                GUILayout.Height(h),
                GUILayout.ExpandWidth(true));

            if (result != code)
                GUI.FocusControl(null);
            EditorGUI.EndDisabledGroup();
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
            };

            _tagStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
            };

            _labelBold = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
            };

            _selectableLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                wordWrap = true,
            };

            _selectableCode = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 10,
                font     = EditorStyles.boldLabel.font,
                wordWrap = true,
                richText = false,
                normal   =
                {
                    textColor   = new Color(0.7f, 0.9f, 0.7f),
                    background  = MakeTex(1, 1, new Color(0.12f, 0.12f, 0.12f)),
                },
                focused =
                {
                    textColor  = new Color(0.7f, 0.9f, 0.7f),
                    background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f)),
                },
                hover =
                {
                    textColor  = new Color(0.8f, 1f, 0.8f),
                    background = MakeTex(1, 1, new Color(0.14f, 0.14f, 0.14f)),
                },
            };
        }
    }

    // ── Toolbar Button ───────────────────────────────────────────────────────────

    public static class ScriptArchitectureToolbar
    {
        private const string ElementId = "MyTools/ScriptArchitecture";

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateButton() =>
            new MainToolbarButton(
                new MainToolbarContent("SA", tooltip: "Open Script Architecture"),
                ScriptArchitectureWindow.ShowWindow);
    }
}