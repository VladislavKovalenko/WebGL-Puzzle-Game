using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace _1GameProject.Scripts.Editor.UI
{
    public class ScaleCalculatorWindow : EditorWindow
    {
        #region Enums

        private enum ScaleMode
        {
            FreeScale,
            ProportionalByWidth,
            ProportionalByHeight,
            FitInside,
            FillOutside,
        }

        private enum CalculatorTab
        {
            ScaleCalc,
            RatioCalc,
            ResolutionCalc,
            BatchScale,
        }

        #endregion

        #region Fields

        // ── Global ────────────────────────────────────────────────────────────
        private bool _roundToIntegers = false;

        // ── Tabs ──────────────────────────────────────────────────────────────
        private CalculatorTab _activeTab = CalculatorTab.ScaleCalc;
        private readonly string[] _tabLabels = { "Scale Calc", "Ratio Calc", "Resolution", "Batch" };

        // ── Scale Calc ────────────────────────────────────────────────────────
        private float _origW = 128f;
        private float _origH = 136f;
        private float _targetW = 136f;
        private float _targetH = 136f;
        private ScaleMode _scaleMode = ScaleMode.ProportionalByWidth;
        private float _uniformScale = 1f;
        private float _computedW;
        private float _computedH;
        private float _scaleX;
        private float _scaleY;
        private bool _resultsDirty = true;

        // ── Ratio Calc ────────────────────────────────────────────────────────
        private float _ratioW = 16f;
        private float _ratioH = 9f;
        private float _ratioKnownW = 1920f;
        private float _ratioKnownH = 1080f;
        private bool _solveForHeight = true;

        // ── Resolution Calc ───────────────────────────────────────────────────
        private int _presetIndex = 0;

        private static readonly string[] ResolutionPresetNames =
        {
            "Custom",
            "Full HD  1920×1080",
            "2K       2560×1440",
            "4K       3840×2160",
            "720p     1280×720",
            "1080p    1920×1080",
            "iPhone 14 Pro 1179×2556",
            "iPad Air  820×1180",
            "Galaxy S23 1080×2340",
        };

        private static readonly Vector2Int[] ResolutionPresets =
        {
            new(0, 0),
            new(1920, 1080),
            new(2560, 1440),
            new(3840, 2160),
            new(1280, 720),
            new(1920, 1080),
            new(1179, 2556),
            new(820, 1180),
            new(1080, 2340),
        };

        private Vector2Int _resFrom = new(1920, 1080);
        private Vector2Int _resTo   = new(2560, 1440);

        // ── Batch Scale ───────────────────────────────────────────────────────
        private float _batchScaleX = 1f;
        private float _batchScaleY = 1f;
        private float _batchScaleZ = 1f;
        private bool  _batchUniform           = true;
        private bool  _batchUseRectTransform  = false;
        private bool  _batchPreserveWorldPos  = true;

        // ── Apply To Scene ────────────────────────────────────────────────────
        private bool _showApplySection = true;

        // ── History ───────────────────────────────────────────────────────────
        private readonly List<string> _history = new();
        private bool    _showHistory = false;
        private Vector2 _historyScroll;

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _resultBoxStyle;
        private GUIStyle _bigNumberStyle;
        private GUIStyle _ratioStyle;
        private GUIStyle _historyStyle;
        private GUIStyle _roundBadgeStyle;
        private bool _stylesInited;

        // ── Colors ────────────────────────────────────────────────────────────
        private readonly Color _accentGreen  = new(0.2f, 0.8f, 0.3f, 1f);
        private readonly Color _accentBlue   = new(0.2f, 0.6f, 1.0f, 1f);
        private readonly Color _accentOrange = new(1.0f, 0.6f, 0.1f, 1f);
        private readonly Color _accentYellow = new(1.0f, 0.9f, 0.2f, 1f);

        #endregion

        #region Rounding Helper

        /// <summary>
        /// Rounds value to nearest integer if global toggle is on.
        /// </summary>
        private float R(float v) => _roundToIntegers ? Mathf.Round(v) : v;

        /// <summary>
        /// Format string: integer or 4-decimal depending on toggle.
        /// </summary>
        private string Fmt(float v) => _roundToIntegers ? $"{Mathf.Round(v):F0}" : $"{v:F4}";

        /// <summary>
        /// Short format (2 decimals or integer).
        /// </summary>
        private string FmtShort(float v) => _roundToIntegers ? $"{Mathf.Round(v):F0}" : $"{v:F2}";

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Megxlord uGUI/Scale Calculator", priority = 101)]
        public static void ShowWindow() => GetWindow<ScaleCalculatorWindow>("Scale Calc");

        private void OnEnable() => Recalculate();

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            InitStyles();
            EditorGUILayout.Space(6);

            DrawHeader();
            EditorGUILayout.Space(4);

            // ── Global toggle ─────────────────────────────────────────────────
            DrawGlobalRoundToggle();
            EditorGUILayout.Space(4);

            _activeTab = (CalculatorTab)GUILayout.Toolbar((int)_activeTab, _tabLabels, GUILayout.Height(26));
            EditorGUILayout.Space(6);

            switch (_activeTab)
            {
                case CalculatorTab.ScaleCalc:     DrawScaleCalcTab();    break;
                case CalculatorTab.RatioCalc:     DrawRatioCalcTab();    break;
                case CalculatorTab.ResolutionCalc: DrawResolutionTab();  break;
                case CalculatorTab.BatchScale:    DrawBatchScaleTab();   break;
            }

            EditorGUILayout.Space(6);
            DrawHistory();
        }

        #endregion

        #region Global Round Toggle

        private void DrawGlobalRoundToggle()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            bool prev = _roundToIntegers;
            _roundToIntegers = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "  Round to integers",
                    "All calculated results will be rounded to the nearest whole number.\n" +
                    "Ratios will be approximate."),
                _roundToIntegers,
                _roundToIntegers ? _roundBadgeStyle : EditorStyles.label,
                GUILayout.Height(22));

            if (_roundToIntegers != prev)
                _resultsDirty = true;

            GUILayout.FlexibleSpace();

            if (_roundToIntegers)
            {
                GUI.color = _accentYellow;
                GUILayout.Label("⚠ Approximate", EditorStyles.miniLabel, GUILayout.Width(90));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab — Scale Calc

        private void DrawScaleCalcTab()
        {
            DrawSectionHeader("📐 Input Dimensions");
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUI.BeginChangeCheck();

            // Original
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Original", GUILayout.Width(60));
            EditorGUILayout.LabelField("W", GUILayout.Width(14));
            _origW = EditorGUILayout.FloatField(_origW, GUILayout.Width(70));
            EditorGUILayout.LabelField("H", GUILayout.Width(14));
            _origH = EditorGUILayout.FloatField(_origH, GUILayout.Width(70));
            if (GUILayout.Button("↕ Swap", GUILayout.Width(60)))
            {
                (_origW, _origH) = (_origH, _origW);
                _resultsDirty = true;
            }
            EditorGUILayout.EndHorizontal();

            DrawRatioBadge(_origW, _origH, "Original ratio:");
            EditorGUILayout.Space(4);

            // Scale mode
            _scaleMode = (ScaleMode)EditorGUILayout.EnumPopup("Scale Mode", _scaleMode);
            EditorGUILayout.Space(4);

            // Target
            switch (_scaleMode)
            {
                case ScaleMode.FreeScale:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target", GUILayout.Width(60));
                    EditorGUILayout.LabelField("W", GUILayout.Width(14));
                    _targetW = EditorGUILayout.FloatField(_targetW, GUILayout.Width(70));
                    EditorGUILayout.LabelField("H", GUILayout.Width(14));
                    _targetH = EditorGUILayout.FloatField(_targetH, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                    break;

                case ScaleMode.ProportionalByWidth:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target W", GUILayout.Width(70));
                    _targetW = EditorGUILayout.FloatField(_targetW, GUILayout.Width(70));
                    EditorGUILayout.LabelField("→ H auto", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    break;

                case ScaleMode.ProportionalByHeight:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target H", GUILayout.Width(70));
                    _targetH = EditorGUILayout.FloatField(_targetH, GUILayout.Width(70));
                    EditorGUILayout.LabelField("→ W auto", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    break;

                case ScaleMode.FitInside:
                case ScaleMode.FillOutside:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Bounds", GUILayout.Width(60));
                    EditorGUILayout.LabelField("W", GUILayout.Width(14));
                    _targetW = EditorGUILayout.FloatField(_targetW, GUILayout.Width(70));
                    EditorGUILayout.LabelField("H", GUILayout.Width(14));
                    _targetH = EditorGUILayout.FloatField(_targetH, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                    break;
            }

            // Uniform scale slider
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Uniform Scale", GUILayout.Width(90));
            float newUniform = EditorGUILayout.Slider(_uniformScale, 0.01f, 10f);
            if (!Mathf.Approximately(newUniform, _uniformScale))
            {
                _uniformScale = newUniform;
                _targetW = _origW * _uniformScale;
                _targetH = _origH * _uniformScale;
            }
            if (GUILayout.Button("×2", GUILayout.Width(30))) { _uniformScale = 2f;   _targetW = _origW * 2f;   _targetH = _origH * 2f;   }
            if (GUILayout.Button("×½", GUILayout.Width(30))) { _uniformScale = 0.5f; _targetW = _origW * 0.5f; _targetH = _origH * 0.5f; }
            if (GUILayout.Button("×3", GUILayout.Width(30))) { _uniformScale = 3f;   _targetW = _origW * 3f;   _targetH = _origH * 3f;   }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                _resultsDirty = true;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
            GUI.backgroundColor = _accentGreen;
            if (GUILayout.Button("▶  Calculate", GUILayout.Height(32)) || _resultsDirty)
                Recalculate();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            DrawSectionHeader("📊 Results");
            DrawScaleResults();

            EditorGUILayout.Space(4);
            _showApplySection = EditorGUILayout.Foldout(_showApplySection, "🎯 Apply to Selection", true, _sectionStyle);
            if (_showApplySection)
                DrawApplySection();
        }

        private void Recalculate()
        {
            _resultsDirty = false;

            if (_origW <= 0 || _origH <= 0)
            {
                _computedW = 0; _computedH = 0;
                _scaleX    = 1; _scaleY    = 1;
                return;
            }

            float rawW, rawH;

            switch (_scaleMode)
            {
                case ScaleMode.FreeScale:
                    rawW = _targetW;
                    rawH = _targetH;
                    break;

                case ScaleMode.ProportionalByWidth:
                    rawW = _targetW;
                    rawH = _targetW * (_origH / _origW);
                    break;

                case ScaleMode.ProportionalByHeight:
                    rawH = _targetH;
                    rawW = _targetH * (_origW / _origH);
                    break;

                case ScaleMode.FitInside:
                {
                    float s = Mathf.Min(_targetW / _origW, _targetH / _origH);
                    rawW = _origW * s;
                    rawH = _origH * s;
                    break;
                }

                default: // FillOutside
                {
                    float s = Mathf.Max(_targetW / _origW, _targetH / _origH);
                    rawW = _origW * s;
                    rawH = _origH * s;
                    break;
                }
            }

            // Apply rounding AFTER computing so scale factors stay consistent
            _computedW = R(rawW);
            _computedH = R(rawH);
            _scaleX    = _origW > 0 ? _computedW / _origW : 1f;
            _scaleY    = _origH > 0 ? _computedH / _origH : 1f;
        }

        private void DrawScaleResults()
        {
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, 2), new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginVertical(_resultBoxStyle);
            EditorGUILayout.BeginHorizontal();

            // Original column
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Original",          EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label(FmtShort(_origW),     _bigNumberStyle);
            GUILayout.Label(FmtShort(_origH),     _bigNumberStyle);
            EditorGUILayout.EndVertical();

            GUILayout.Label("→", _bigNumberStyle, GUILayout.Width(30));

            // Result column
            EditorGUILayout.BeginVertical();
            GUI.color = _accentGreen;
            GUILayout.Label("Result",             EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label(FmtShort(_computedW), _bigNumberStyle);
            GUILayout.Label(FmtShort(_computedH), _bigNumberStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            // Scale factor column
            EditorGUILayout.BeginVertical();
            GUI.color = _accentBlue;
            GUILayout.Label("Scale factor",                 EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"×{Fmt(_scaleX)}",            _bigNumberStyle);
            GUILayout.Label($"×{Fmt(_scaleY)}",            _bigNumberStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            DrawRatioBadge(_computedW, _computedH, "Result ratio:");

            // Delta
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delta:", EditorStyles.miniLabel, GUILayout.Width(40));
            GUI.color = _accentOrange;
            float dw = R(_computedW - _origW);
            float dh = R(_computedH - _origH);
            EditorGUILayout.LabelField(
                $"ΔW = {dw:+0.##;-0.##;0}   ΔH = {dh:+0.##;-0.##;0}",
                EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // Area
            float origArea  = _origW    * _origH;
            float newArea   = R(_computedW) * R(_computedH);
            float areaRatio = origArea > 0 ? newArea / origArea : 0;
            EditorGUILayout.LabelField(
                $"Area: {origArea:F0} px² → {newArea:F0} px²  (×{areaRatio:F3})",
                EditorStyles.miniLabel);

            // Rounding warning
            if (_roundToIntegers)
            {
                GUI.color = _accentYellow;
                EditorGUILayout.LabelField(
                    "⚠ Results rounded — ratio is approximate",
                    EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);

            // Copy buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Copy W",          GUILayout.Height(22))) GUIUtility.systemCopyBuffer = FmtShort(_computedW);
            if (GUILayout.Button("📋 Copy H",          GUILayout.Height(22))) GUIUtility.systemCopyBuffer = FmtShort(_computedH);
            if (GUILayout.Button("📋 Copy Both",       GUILayout.Height(22))) GUIUtility.systemCopyBuffer = $"{FmtShort(_computedW)} × {FmtShort(_computedH)}";
            if (GUILayout.Button("📋 Scale Factors",   GUILayout.Height(22))) GUIUtility.systemCopyBuffer = $"{Fmt(_scaleX)}, {Fmt(_scaleY)}";
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("🕘 Add to History", GUILayout.Height(22)))
                AddToHistory($"[Scale] {_origW:F1}×{_origH:F1} → {FmtShort(_computedW)}×{FmtShort(_computedH)}  (×{Fmt(_scaleX)}, ×{Fmt(_scaleY)})");
        }

        private void DrawApplySection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var targets = GetSelectedTransforms();

            if (targets.Count == 0)
            {
                EditorGUILayout.HelpBox("Select GameObjects to apply computed size.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Selected: {targets.Count} object(s)", EditorStyles.boldLabel);
                _batchPreserveWorldPos = EditorGUILayout.Toggle("Preserve World Position", _batchPreserveWorldPos);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = _accentBlue;
                if (GUILayout.Button($"Apply Size  ({FmtShort(_computedW)} × {FmtShort(_computedH)})", GUILayout.Height(30)))
                    ApplyComputedSizeToSelection(targets);
                GUI.backgroundColor = _accentOrange;
                if (GUILayout.Button($"Apply Scale (×{Fmt(_scaleX)}, ×{Fmt(_scaleY)})", GUILayout.Height(30)))
                    ApplyScaleFactorToSelection(targets);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Tab — Ratio Calc

        private void DrawRatioCalcTab()
        {
            DrawSectionHeader("📏 Aspect Ratio Calculator");
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("Known dimensions:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("W", GUILayout.Width(14));
            _ratioKnownW = EditorGUILayout.FloatField(_ratioKnownW, GUILayout.Width(80));
            EditorGUILayout.LabelField("H", GUILayout.Width(14));
            _ratioKnownH = EditorGUILayout.FloatField(_ratioKnownH, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            DrawRatioBadge(_ratioKnownW, _ratioKnownH, "Ratio:");
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("Solve for:", EditorStyles.boldLabel);
            _solveForHeight = EditorGUILayout.Toggle("Solve for Height (given Width)", _solveForHeight);

            if (_solveForHeight)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New W", GUILayout.Width(50));
                _ratioW = EditorGUILayout.FloatField(_ratioW, GUILayout.Width(80));

                float solvedH = _ratioKnownW > 0 ? _ratioW * (_ratioKnownH / _ratioKnownW) : 0f;
                float display = R(solvedH);

                GUI.color = _accentGreen;
                EditorGUILayout.LabelField($"→  H = {FmtShort(display)}", _bigNumberStyle);
                GUI.color = Color.white;

                if (GUILayout.Button("📋", GUILayout.Width(28)))
                    GUIUtility.systemCopyBuffer = FmtShort(display);

                EditorGUILayout.EndHorizontal();

                DrawRoundingHint(solvedH, display);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New H", GUILayout.Width(50));
                _ratioH = EditorGUILayout.FloatField(_ratioH, GUILayout.Width(80));

                float solvedW = _ratioKnownH > 0 ? _ratioH * (_ratioKnownW / _ratioKnownH) : 0f;
                float display = R(solvedW);

                GUI.color = _accentGreen;
                EditorGUILayout.LabelField($"→  W = {FmtShort(display)}", _bigNumberStyle);
                GUI.color = Color.white;

                if (GUILayout.Button("📋", GUILayout.Width(28)))
                    GUIUtility.systemCopyBuffer = FmtShort(display);

                EditorGUILayout.EndHorizontal();

                DrawRoundingHint(solvedW, display);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Common ratios check:", EditorStyles.boldLabel);
            DrawCommonRatioComparison(_ratioKnownW, _ratioKnownH);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            DrawSectionHeader("🔲 Ratio Visualizer");
            DrawRatioVisualizer(_ratioKnownW, _ratioKnownH);
        }

        private void DrawCommonRatioComparison(float w, float h)
        {
            if (w <= 0 || h <= 0) return;

            var common = new (string name, float r)[]
            {
                ("1:1",   1f),
                ("4:3",   4f / 3f),
                ("16:9",  16f / 9f),
                ("16:10", 16f / 10f),
                ("21:9",  21f / 9f),
                ("3:4",   3f / 4f),
                ("9:16",  9f / 16f),
                ("2:3",   2f / 3f),
            };

            float inputRatio = w / h;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            foreach (var (name, r) in common)
            {
                float diff  = Mathf.Abs(inputRatio - r) / r * 100f;
                bool  match = diff < 1f;

                EditorGUILayout.BeginHorizontal();
                GUI.color = match ? _accentGreen : Color.white;
                EditorGUILayout.LabelField(name, GUILayout.Width(50));
                GUI.color = Color.white;

                Rect barRect = GUILayoutUtility.GetRect(80, 14);
                float fill = Mathf.Clamp01(1f - diff / 100f);
                EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
                EditorGUI.DrawRect(
                    new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height),
                    match ? _accentGreen : new Color(0.4f, 0.4f, 0.8f));

                EditorGUILayout.LabelField(
                    match ? "✓ Match!" : $"diff {diff:F1}%",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawRatioVisualizer(float w, float h)
        {
            if (w <= 0 || h <= 0) return;

            float maxVizW = position.width - 40f;
            float maxVizH = 100f;
            float ratio   = w / h;

            float vizW, vizH;
            if (ratio >= 1f)
            {
                vizW = maxVizW;
                vizH = Mathf.Min(maxVizW / ratio, maxVizH);
            }
            else
            {
                vizH = maxVizH;
                vizW = maxVizH * ratio;
            }

            Rect containerRect = GUILayoutUtility.GetRect(position.width - 20f, maxVizH + 10f);
            float cx = containerRect.x + (containerRect.width - vizW) * 0.5f;
            float cy = containerRect.y + (containerRect.height - vizH) * 0.5f;
            Rect vizRect = new Rect(cx, cy, vizW, vizH);

            EditorGUI.DrawRect(vizRect, new Color(0.25f, 0.4f, 0.6f, 0.6f));

            Handles.BeginGUI();
            Handles.color = _accentBlue;
            Handles.DrawSolidRectangleWithOutline(vizRect, Color.clear, _accentBlue);
            Handles.EndGUI();

            string label = $"{FmtShort(w)} × {FmtShort(h)}  ({GetSimplifiedRatio(w, h)})";
            GUI.Label(vizRect, label,
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { normal = { textColor = Color.white } });
        }

        #endregion

        #region Tab — Resolution Calc

        private void DrawResolutionTab()
        {
            DrawSectionHeader("🖥 Resolution Scale Calculator");
            EditorGUILayout.BeginVertical(GUI.skin.box);

            int newPreset = EditorGUILayout.Popup("From Preset", _presetIndex, ResolutionPresetNames);
            if (newPreset != _presetIndex)
            {
                _presetIndex = newPreset;
                if (_presetIndex > 0)
                    _resFrom = ResolutionPresets[_presetIndex];
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("From", GUILayout.Width(40));
            _resFrom.x = EditorGUILayout.IntField(_resFrom.x, GUILayout.Width(70));
            EditorGUILayout.LabelField("×", GUILayout.Width(14));
            _resFrom.y = EditorGUILayout.IntField(_resFrom.y, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            DrawRatioBadge(_resFrom.x, _resFrom.y, "Ratio:");
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("To", GUILayout.Width(40));
            _resTo.x = EditorGUILayout.IntField(_resTo.x, GUILayout.Width(70));
            EditorGUILayout.LabelField("×", GUILayout.Width(14));
            _resTo.y = EditorGUILayout.IntField(_resTo.y, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            DrawRatioBadge(_resTo.x, _resTo.y, "Ratio:");
            EditorGUILayout.Space(6);

            if (_resFrom.x > 0 && _resFrom.y > 0 && _resTo.x > 0 && _resTo.y > 0)
            {
                float rawSX = (float)_resTo.x / _resFrom.x;
                float rawSY = (float)_resTo.y / _resFrom.y;
                float sX = R(rawSX);
                float sY = R(rawSY);
                float pixelRatio = (float)(_resTo.x * _resTo.y) / (_resFrom.x * _resFrom.y);

                EditorGUILayout.BeginVertical(_resultBoxStyle);

                GUI.color = _accentGreen;
                EditorGUILayout.LabelField($"Scale X: ×{Fmt(sX)}   Scale Y: ×{Fmt(sY)}", _bigNumberStyle);
                EditorGUILayout.LabelField($"Pixel count ratio: ×{pixelRatio:F4}", _bigNumberStyle);
                GUI.color = Color.white;

                EditorGUILayout.LabelField(
                    $"{_resFrom.x * _resFrom.y / 1_000_000f:F2} MP  →  {_resTo.x * _resTo.y / 1_000_000f:F2} MP",
                    EditorStyles.miniLabel);

                // Rounding hint
                DrawRoundingHint(rawSX, sX, "Scale X");

                bool sameRatio = Mathf.Approximately((float)_resFrom.x / _resFrom.y,
                                                      (float)_resTo.x   / _resTo.y);
                if (!sameRatio)
                    EditorGUILayout.HelpBox("⚠ Aspect ratios differ — scaling will stretch content.",
                                            MessageType.Warning);

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(4);
                DrawSectionHeader("📐 Common UI element scaling");
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Original", GUILayout.Width(70));
                EditorGUILayout.LabelField("Scaled X",  GUILayout.Width(80));
                EditorGUILayout.LabelField("Scaled Y",  GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                foreach (float s in new[] { 32f, 64f, 100f, 200f, 512f, 1024f })
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{s:F0}",              GUILayout.Width(70));
                    EditorGUILayout.LabelField(FmtShort(R(s * rawSX)), GUILayout.Width(80));
                    EditorGUILayout.LabelField(FmtShort(R(s * rawSY)), GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                if (GUILayout.Button("🕘 Add to History"))
                    AddToHistory($"[Res] {_resFrom.x}×{_resFrom.y} → {_resTo.x}×{_resTo.y}  (×{Fmt(sX)}, ×{Fmt(sY)})");
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Tab — Batch Scale

        private void DrawBatchScaleTab()
        {
            DrawSectionHeader("⚡ Batch Scale Selected Objects");
            EditorGUILayout.BeginVertical(GUI.skin.box);

            _batchUniform = EditorGUILayout.Toggle("Uniform Scale", _batchUniform);

            if (_batchUniform)
            {
                _batchScaleX = EditorGUILayout.FloatField("Scale", _batchScaleX);
                _batchScaleY = _batchScaleZ = _batchScaleX;
            }
            else
            {
                _batchScaleX = EditorGUILayout.FloatField("Scale X", _batchScaleX);
                _batchScaleY = EditorGUILayout.FloatField("Scale Y", _batchScaleY);
                _batchScaleZ = EditorGUILayout.FloatField("Scale Z", _batchScaleZ);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quick:", GUILayout.Width(40));
            void SetU(float v) { _batchScaleX = _batchScaleY = _batchScaleZ = v; }
            if (GUILayout.Button("×0.1")) SetU(0.1f);
            if (GUILayout.Button("×0.5")) SetU(0.5f);
            if (GUILayout.Button("×1"))   SetU(1f);
            if (GUILayout.Button("×2"))   SetU(2f);
            if (GUILayout.Button("×5"))   SetU(5f);
            if (GUILayout.Button("×10"))  SetU(10f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            _batchUseRectTransform = EditorGUILayout.Toggle("Use sizeDelta (RectTransform)", _batchUseRectTransform);
            _batchPreserveWorldPos = EditorGUILayout.Toggle("Preserve World Position",       _batchPreserveWorldPos);

            EditorGUILayout.EndVertical();

            var targets = GetSelectedTransforms();
            if (targets.Count == 0)
            {
                EditorGUILayout.HelpBox("Select GameObjects in Hierarchy.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Will affect: {targets.Count} object(s)", EditorStyles.boldLabel);

            // Preview table
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name",    GUILayout.Width(130));
            EditorGUILayout.LabelField("Current", GUILayout.Width(120));
            EditorGUILayout.LabelField("Result",  GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, 1), new Color(0.4f, 0.4f, 0.4f));

            foreach (var t in targets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(t.name, GUILayout.Width(130));

                if (_batchUseRectTransform && t.TryGetComponent<RectTransform>(out var rt))
                {
                    Vector2 cur = rt.sizeDelta;
                    Vector2 res = new Vector2(R(cur.x * _batchScaleX), R(cur.y * _batchScaleY));
                    EditorGUILayout.LabelField($"{cur.x:F1} × {cur.y:F1}", GUILayout.Width(120));
                    GUI.color = _accentGreen;
                    EditorGUILayout.LabelField($"{FmtShort(res.x)} × {FmtShort(res.y)}", GUILayout.Width(120));
                    GUI.color = Color.white;
                }
                else
                {
                    Vector3 cur = t.localScale;
                    Vector3 res = new Vector3(
                        R(cur.x * _batchScaleX),
                        R(cur.y * _batchScaleY),
                        R(cur.z * _batchScaleZ));
                    EditorGUILayout.LabelField($"{cur.x:F3},{cur.y:F3},{cur.z:F3}", GUILayout.Width(120));
                    GUI.color = _accentGreen;
                    EditorGUILayout.LabelField($"{Fmt(res.x)},{Fmt(res.y)},{Fmt(res.z)}", GUILayout.Width(120));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            GUI.backgroundColor = _accentGreen;
            if (GUILayout.Button("⚡ Apply Batch Scale", GUILayout.Height(34)))
                ApplyBatchScale(targets);
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Helpers — Drawing

        private void DrawSectionHeader(string title) =>
            EditorGUILayout.LabelField(title, _sectionStyle);

        private void DrawHeader() =>
            EditorGUILayout.LabelField("Scale Calculator", _titleStyle);

        private void DrawRatioBadge(float w, float h, string prefix)
        {
            if (w <= 0 || h <= 0) return;
            string simplified = GetSimplifiedRatio(w, h);
            float  r          = w / h;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(prefix, EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = _accentBlue;
            EditorGUILayout.LabelField($"{simplified}  ({r:F4})", _ratioStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Shows a small warning if rounding changed the value noticeably.
        /// </summary>
        private void DrawRoundingHint(float raw, float rounded, string label = "")
        {
            if (!_roundToIntegers) return;
            float diff = Mathf.Abs(raw - rounded);
            if (diff < 0.001f) return;

            GUI.color = _accentYellow;
            string prefix = string.IsNullOrEmpty(label) ? "" : $"{label}: ";
            EditorGUILayout.LabelField(
                $"  ⚠ {prefix}exact = {raw:F4}  →  rounded = {rounded:F0}  (Δ {diff:F4})",
                EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        private void DrawHistory()
        {
            _showHistory = EditorGUILayout.Foldout(
                _showHistory, $"🕘 History ({_history.Count})", true, _sectionStyle);

            if (!_showHistory || _history.Count == 0) return;

            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.MaxHeight(120));
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                EditorGUILayout.LabelField(_history[i], _historyStyle);
                if (GUILayout.Button("📋", GUILayout.Width(26)))
                    GUIUtility.systemCopyBuffer = _history[i];
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear History", GUILayout.Height(20)))
                _history.Clear();
        }

        #endregion

        #region Helpers — Logic

        private static string GetSimplifiedRatio(float w, float h)
        {
            if (w <= 0 || h <= 0) return "N/A";
            int iw = Mathf.RoundToInt(w * 1000);
            int ih = Mathf.RoundToInt(h * 1000);
            int g  = GCD(iw, ih);
            int sw = iw / g;
            int sh = ih / g;

            if (sw > 200 || sh > 200)
            {
                float ratio = (float)sw / sh;
                var known = new (int a, int b)[]
                    { (1,1),(4,3),(3,2),(16,9),(16,10),(21,9),(2,3),(9,16),(3,4) };
                foreach (var (a, b) in known)
                    if (Mathf.Abs(ratio - (float)a / b) < 0.01f)
                        return $"{a}:{b}";
                return $"{w / h:F4}:1";
            }

            return $"{sw}:{sh}";
        }

        private static int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);

        private void AddToHistory(string entry)
        {
            if (_history.Count > 50) _history.RemoveAt(0);
            _history.Add(entry);
        }

        private static List<Transform> GetSelectedTransforms()
        {
            var result = new List<Transform>();
            foreach (var go in Selection.gameObjects)
                if (go != null)
                    result.Add(go.transform);
            return result;
        }

        private void ApplyComputedSizeToSelection(List<Transform> targets)
        {
            foreach (var t in targets)
            {
                if (t == null) continue;
                if (!t.TryGetComponent<RectTransform>(out var rt)) continue;
                Undo.RecordObject(rt, "Apply Computed Size");
                Vector3 worldPos = rt.position;
                rt.sizeDelta = new Vector2(_computedW, _computedH);
                if (_batchPreserveWorldPos) rt.position = worldPos;
                EditorUtility.SetDirty(rt);
            }
            AddToHistory($"[Apply Size] {FmtShort(_computedW)}×{FmtShort(_computedH)} to {targets.Count} objects");
        }

        private void ApplyScaleFactorToSelection(List<Transform> targets)
        {
            foreach (var t in targets)
            {
                if (t == null) continue;
                Undo.RecordObject(t, "Apply Scale Factor");

                if (t.TryGetComponent<RectTransform>(out var rt))
                {
                    rt.sizeDelta = new Vector2(
                        R(rt.sizeDelta.x * _scaleX),
                        R(rt.sizeDelta.y * _scaleY));
                    EditorUtility.SetDirty(rt);
                }
                else
                {
                    t.localScale = new Vector3(
                        R(t.localScale.x * _scaleX),
                        R(t.localScale.y * _scaleY),
                        t.localScale.z);
                    EditorUtility.SetDirty(t);
                }
            }
            AddToHistory($"[Apply Factor] ×{Fmt(_scaleX)}, ×{Fmt(_scaleY)} to {targets.Count} objects");
        }

        private void ApplyBatchScale(List<Transform> targets)
        {
            foreach (var t in targets)
            {
                if (t == null) continue;

                if (_batchUseRectTransform && t.TryGetComponent<RectTransform>(out var rt))
                {
                    Undo.RecordObject(rt, "Batch Scale sizeDelta");
                    Vector3 worldPos = rt.position;
                    rt.sizeDelta = new Vector2(
                        R(rt.sizeDelta.x * _batchScaleX),
                        R(rt.sizeDelta.y * _batchScaleY));
                    if (_batchPreserveWorldPos) rt.position = worldPos;
                    EditorUtility.SetDirty(rt);
                }
                else
                {
                    Undo.RecordObject(t, "Batch Scale");
                    Vector3 worldPos = t.position;
                    t.localScale = new Vector3(
                        R(t.localScale.x * _batchScaleX),
                        R(t.localScale.y * _batchScaleY),
                        R(t.localScale.z * _batchScaleZ));
                    if (_batchPreserveWorldPos) t.position = worldPos;
                    EditorUtility.SetDirty(t);
                }
            }
            AddToHistory(
                $"[Batch] ×{_batchScaleX:F3},{_batchScaleY:F3},{_batchScaleZ:F3} to {targets.Count} objects");
            ShowNotification(new GUIContent($"Scaled {targets.Count} objects!"));
        }

        #endregion

        #region Styles

        private void InitStyles()
        {
            if (_stylesInited) return;
            _stylesInited = true;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.95f, 0.95f, 0.95f) },
            };

            _sectionStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
            };

            _resultBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
            };

            _bigNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            _ratioStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
            };

            _historyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
            };

            _roundBadgeStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.9f, 0.2f) },
            };
        }

        #endregion
    }

    // ─────────────────────────────────────────────────────────────────────────
    [InitializeOnLoad]
    public static class ScaleCalcToolbar
    {
        private const string ElementId = "Megxlord/ScaleCalculator";
        static ScaleCalcToolbar() { }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateButton()
        {
            var content = new MainToolbarContent("Scale", tooltip: "Open Scale Calculator");
            return new MainToolbarButton(content, () => ScaleCalculatorWindow.ShowWindow());
        }
    }
}