using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _1GameProject.Scripts.Editor.UI
{
    public class ScaleCalculator2DWindow : EditorWindow
    {
        #region Enums & Structs

        private enum SolveMode
        {
            ByLargest,
            BySmallest,
            ByWidth,
            ByHeight,
        }

        private struct InitialState
        {
            public Vector2 size;
            public Vector3 localScale;
        }

        #endregion

        #region Fields

        // ── Selection State Cache ─────────────────────────────────────────────
        private readonly Dictionary<SpriteRenderer, InitialState> _initialStates = new();

        // ── State ─────────────────────────────────────────────────────────────
        private UnityEngine.SpriteDrawMode _detectedDrawMode = UnityEngine.SpriteDrawMode.Simple;
        private string _detectedFromName = "";
        private bool   _hasValidSelection;

        // ── Original Size (Read Only) ─────────────────────────────────────────
        private float _origW = 128f;
        private float _origH = 128f;

        // ── Current Size (Editable & Applied) ─────────────────────────────────
        private float _currentW = 128f;
        private float _currentH = 128f;
        
        private bool _keepRatio = true;
        
        // Внутренний буфер обмена плагина
        private static float s_copiedW = -1f;
        private static float s_copiedH = -1f;

        // ── Ratio Presets ─────────────────────────────────────────────────────
        private static readonly (string label, float w, float h)[] RatioPresets =
        {
            ("1 : 1",  1f,  1f),   ("1 : 2",  1f,  2f),   ("2 : 1",  2f,  1f),
            ("4 : 3",  4f,  3f),   ("3 : 4",  3f,  4f),   ("16 : 9", 16f, 9f),
            ("9 : 16", 9f,  16f),  ("3 : 2",  3f,  2f),   ("2 : 3",  2f,  3f),
            ("Custom", 0f,  0f),
        };

        private int   _selectedPreset;
        private float _customRatioW = 1f;
        private float _customRatioH = 1f;

        // ── Solve mode ────────────────────────────────────────────────────────
        private SolveMode _solveMode = SolveMode.ByLargest;
        private float     _baseValue = 256f;

        // ── Logic Flags ───────────────────────────────────────────────────────
        private bool _needRecalculate;
        private bool _needApply;

        // ── Options ───────────────────────────────────────────────────────────
        private bool _roundToIntegers;
        private bool _autoApply = true;
        private bool _preserveScale;

        // ── Drag state ────────────────────────────────────────────────────────
        private static readonly int DragFloatHash = "DragFloat".GetHashCode();

        // ── Styles / Colors ───────────────────────────────────────────────────
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _resultBoxStyle;
        private GUIStyle _bigNumberStyle;
        private bool     _stylesInited;

        private readonly Color _accentGreen  = new(0.2f, 0.8f,  0.3f, 1f);
        private readonly Color _accentBlue   = new(0.2f, 0.6f,  1.0f, 1f);
        private readonly Color _accentYellow = new(1.0f, 0.9f,  0.2f, 1f);
        private readonly Color _accentOrange = new(1.0f, 0.55f, 0.1f, 1f);
        private readonly Color _accentGray   = new(0.5f, 0.5f,  0.5f, 1f);

        #endregion

        #region Rounding

        private float  R(float v)    => _roundToIntegers ? Mathf.Round(v) : v;
        private string Fmt(float v)  => _roundToIntegers ? $"{Mathf.Round(v):F0}" : $"{v:F4}";

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Megxlord Toolbox/2D/Scale Calculator 2D", priority = 102)]
        public static void ShowWindow() =>
            GetWindow<ScaleCalculator2DWindow>("Scale Calc 2D");

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            CacheInitialStates();
            TryReadFromSelection();
            
            _currentW = _origW;
            _currentH = _origH;
            
            _needRecalculate = false;
            _needApply = false;
            Repaint();
        }

        private void CacheInitialStates()
        {
            _initialStates.Clear();
            foreach (var go in Selection.gameObjects)
            {
                if (go != null && go.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    _initialStates[sr] = new InitialState
                    {
                        size = sr.size,
                        localScale = go.transform.localScale
                    };
                }
            }
        }

        #endregion

        #region Read from Selection (auto)

        private void TryReadFromSelection()
        {
            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.TryGetComponent<SpriteRenderer>(out var sr))
            {
                ReadFromSpriteRenderer(sr);
                return;
            }

            if (Selection.activeObject is Sprite sp)
            {
                _origW = sp.rect.width; _origH = sp.rect.height;
                _detectedDrawMode = UnityEngine.SpriteDrawMode.Simple;
                _detectedFromName = sp.name;
                _hasValidSelection = true;
                return;
            }

            if (Selection.activeObject is Texture2D tex)
            {
                _origW = tex.width; _origH = tex.height;
                _detectedDrawMode = UnityEngine.SpriteDrawMode.Simple;
                _detectedFromName = tex.name;
                _hasValidSelection = true;
                return;
            }

            _hasValidSelection = false;
            _detectedFromName = "";
        }

        private void ReadFromSpriteRenderer(SpriteRenderer sr)
        {
            _detectedDrawMode  = sr.drawMode;
            _detectedFromName  = sr.name;
            _hasValidSelection = true;

            switch (sr.drawMode)
            {
                case UnityEngine.SpriteDrawMode.Sliced:
                case UnityEngine.SpriteDrawMode.Tiled:
                    _origW = sr.size.x;
                    _origH = sr.size.y;
                    break;
                default:
                    if (sr.sprite != null)
                    {
                        Vector3 s = sr.transform.localScale;
                        Vector2 b = sr.sprite.bounds.size;
                        _origW = Mathf.Abs(s.x * b.x);
                        _origH = Mathf.Abs(s.y * b.y);
                    }
                    else
                    {
                        _origW = Mathf.Abs(sr.transform.localScale.x);
                        _origH = Mathf.Abs(sr.transform.localScale.y);
                    }
                    break;
            }
        }

        #endregion

        #region OnGUI Main Loop

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Scale Calculator 2D", _titleStyle);
            EditorGUILayout.Space(4);

            DrawTopActionPanel();
            EditorGUILayout.Space(6);

            DrawGlobalOptions();
            EditorGUILayout.Space(6);

            DrawOriginalSize();
            EditorGUILayout.Space(6);

            DrawCurrentSize();
            EditorGUILayout.Space(6);

            DrawRatioPresets();
            EditorGUILayout.Space(6);

            DrawSolveMode();
            EditorGUILayout.Space(6);

            if (_needRecalculate)
            {
                Recalculate();
                _needRecalculate = false;
                _needApply = true;
            }

            if (_needApply)
            {
                if (_autoApply && _hasValidSelection)
                {
                    ApplyToSelection(GetSelectedSpriteRenderers(), manualGroup: false);
                }
                _needApply = false;
            }

            DrawTransformationInfo();
            EditorGUILayout.Space(6);
            
            DrawManualApplySection();
        }

        private void DrawTopActionPanel()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = _accentOrange;
            if (GUILayout.Button("↺ Reset to Initial", GUILayout.Height(28)))
            {
                RestoreInitialStates();
            }
            
            GUI.backgroundColor = _accentBlue;
            if (GUILayout.Button("⌗ Round Selection in Scene", GUILayout.Height(28)))
            {
                RoundCurrentSelectionInScene();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGlobalOptions()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            
            _roundToIntegers = EditorGUILayout.ToggleLeft(" Round all settings to integers", _roundToIntegers, GUILayout.Width(190));
            _autoApply = EditorGUILayout.ToggleLeft(" Live Auto-Apply", _autoApply);

            if (EditorGUI.EndChangeCheck())
            {
                if (_roundToIntegers)
                {
                    _currentW = R(_currentW);
                    _currentH = R(_currentH);
                    _baseValue = R(_baseValue);
                    _customRatioW = R(_customRatioW);
                    _customRatioH = R(_customRatioH);
                    _needApply = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Original & Current Size

        private void DrawOriginalSize()
        {
            EditorGUILayout.LabelField("📐 Original Size (Read Only)", _sectionStyle);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawSelectionStatus();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Width:  {Fmt(_origW)}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Height: {Fmt(_origH)}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            DrawRatioBadge(_origW, _origH, "Original Ratio:");
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentSize()
        {
            EditorGUILayout.LabelField("📏 Current Size (Draggable & Active)", _sectionStyle);
            EditorGUILayout.BeginVertical(_resultBoxStyle);

            // Кнопки Keep Ratio, Copy, Paste
            EditorGUILayout.BeginHorizontal();
            _keepRatio = EditorGUILayout.ToggleLeft(new GUIContent("🔗 Keep Ratio", "Автоматически подгонять второе значение для сохранения пропорций"), _keepRatio, GUILayout.Width(110));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Copy", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            {
                s_copiedW = _currentW;
                s_copiedH = _currentH;
            }

            GUI.enabled = s_copiedW > 0 && s_copiedH > 0;
            if (GUILayout.Button("Paste", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                _currentW = s_copiedW;
                _currentH = s_copiedH;
                _needApply = true;
                
                _selectedPreset = RatioPresets.Length - 1; // Custom
                _customRatioW = _currentW;
                _customRatioH = _currentH;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            // Поля ввода
            Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            const float fieldW = 160f;
            const float gap    =   8f;
            const float swapW  =  60f;

            Rect rectW    = new(rowRect.x,                rowRect.y, fieldW,         rowRect.height);
            Rect rectH    = new(rowRect.x + fieldW + gap, rowRect.y, fieldW,         rowRect.height);
            Rect rectSwap = new(rowRect.xMax - swapW,     rowRect.y, swapW,          rowRect.height);

            float newW = DragFloatField(rectW, "W", _currentW);
            float newH = DragFloatField(rectH, "H", _currentH);

            bool wChanged = !Mathf.Approximately(newW, _currentW);
            bool hChanged = !Mathf.Approximately(newH, _currentH);

            if (wChanged || hChanged)
            {
                // Логика Keep Ratio
                if (_keepRatio)
                {
                    if (wChanged && _currentW > 0)
                    {
                        float multiplier = newW / _currentW;
                        newH = _currentH * multiplier;
                    }
                    else if (hChanged && _currentH > 0)
                    {
                        float multiplier = newH / _currentH;
                        newW = _currentW * multiplier;
                    }
                }

                _currentW = R(Mathf.Max(0.01f, newW));
                _currentH = R(Mathf.Max(0.01f, newH));
                
                _needApply = true;
                _selectedPreset = RatioPresets.Length - 1; // Custom
                _customRatioW = _currentW;
                _customRatioH = _currentH;
            }

            if (GUI.Button(rectSwap, "↕ Swap"))
            {
                (_currentW, _currentH) = (_currentH, _currentW);
                _needApply = true;
                
                _selectedPreset = RatioPresets.Length - 1;
                _customRatioW = _currentW;
                _customRatioH = _currentH;
            }

            EditorGUILayout.Space(8);
            DrawRatioBadge(_currentW, _currentH, "Current Ratio:");

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectionStatus()
        {
            EditorGUILayout.BeginHorizontal();

            if (_hasValidSelection)
            {
                string modeTag = _detectedDrawMode switch
                {
                    UnityEngine.SpriteDrawMode.Sliced => "[Sliced · size]",
                    UnityEngine.SpriteDrawMode.Tiled  => "[Tiled  · size]",
                    _                                 => "[Simple · localScale]",
                };

                GUI.color = _accentGreen;
                EditorGUILayout.LabelField($"● Bound to: {_detectedFromName}  {modeTag}", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = _accentGray;
                EditorGUILayout.LabelField("○ No Scene selection — select a SpriteRenderer", EditorStyles.miniLabel);
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Ratio Presets

        private void DrawRatioPresets()
        {
            EditorGUILayout.LabelField("🔲 Target Ratio Presets", _sectionStyle);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            const int cols  = 5;
            int       total = RatioPresets.Length;
            int       rows  = Mathf.CeilToInt((float)total / cols);

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    if (idx >= total) { GUILayout.FlexibleSpace(); break; }

                    bool isSelected = _selectedPreset == idx;
                    GUI.backgroundColor = isSelected ? _accentBlue : Color.white;
                    if (GUILayout.Button(RatioPresets[idx].label, GUILayout.Height(24)))
                    {
                        _selectedPreset = idx;
                        _needRecalculate = true; 
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (_selectedPreset == RatioPresets.Length - 1)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Custom W", GUILayout.Width(70));
                EditorGUI.BeginChangeCheck();
                _customRatioW = EditorGUILayout.FloatField(_customRatioW, GUILayout.Width(60));
                EditorGUILayout.LabelField("H", GUILayout.Width(14));
                _customRatioH = EditorGUILayout.FloatField(_customRatioH, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    _customRatioW = R(Mathf.Max(0.01f, _customRatioW));
                    _customRatioH = R(Mathf.Max(0.01f, _customRatioH));
                    _needRecalculate = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private (float rw, float rh) GetCurrentRatio()
        {
            if (_selectedPreset < 0 || _selectedPreset >= RatioPresets.Length) return (1f, 1f);
            (string _, float w, float h) = RatioPresets[_selectedPreset];

            if (w == 0f && h == 0f)
                return (_customRatioW > 0 ? _customRatioW : 1f, _customRatioH > 0 ? _customRatioH : 1f);

            return (w, h);
        }

        #endregion

        #region Solve Mode & Recalculate

        private void DrawSolveMode()
        {
            EditorGUILayout.LabelField("⚙ Auto-Calculation Mode", _sectionStyle);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUI.BeginChangeCheck();
            _solveMode = (SolveMode)EditorGUILayout.EnumPopup("Constraint", _solveMode);

            if (_solveMode is SolveMode.ByWidth or SolveMode.ByHeight)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Target Base Value", GUILayout.Width(110));
                _baseValue = EditorGUILayout.FloatField(_baseValue, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                _baseValue = R(Mathf.Max(0.001f, _baseValue));
                _needRecalculate = true;
            }

            string hint = _solveMode switch
            {
                SolveMode.ByLargest  => $"Основа для расчета = max(OrigW, OrigH) = {Mathf.Max(_origW, _origH)}",
                SolveMode.BySmallest => $"Основа для расчета = min(OrigW, OrigH) = {Mathf.Min(_origW, _origH)}",
                SolveMode.ByWidth    => $"Форсировать ширину (Current W) = {_baseValue}",
                SolveMode.ByHeight   => $"Форсировать высоту (Current H) = {_baseValue}",
                _                    => ""
            };
            
            GUI.color = _accentGray;
            EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _preserveScale = EditorGUILayout.Toggle(
                new GUIContent("Preserve localScale", "Изменяет только 'size' (для Sliced/Tiled). Масштаб Simple спрайтов блокируется."),
                _preserveScale);
            if (EditorGUI.EndChangeCheck()) _needApply = true;

            EditorGUILayout.EndVertical();
        }

        private void Recalculate()
        {
            if (_origW <= 0 || _origH <= 0) return;

            (float rw, float rh) = GetCurrentRatio();
            if (rw <= 0 || rh <= 0) return;

            float targetRatio = rw / rh;
            
            float baseSize = _solveMode switch
            {
                SolveMode.ByLargest  => Mathf.Max(_origW, _origH),
                SolveMode.BySmallest => Mathf.Min(_origW, _origH),
                _                    => _baseValue,
            };

            float rawW, rawH;

            if (_solveMode == SolveMode.ByHeight)
            {
                rawH = baseSize;
                rawW = baseSize * targetRatio;
            }
            else if (_solveMode == SolveMode.ByWidth)
            {
                rawW = baseSize;
                rawH = baseSize / targetRatio;
            }
            else
            {
                if (targetRatio >= 1f)
                {
                    rawW = baseSize;
                    rawH = baseSize / targetRatio;
                }
                else
                {
                    rawH = baseSize;
                    rawW = baseSize * targetRatio;
                }
            }

            _currentW = R(rawW);
            _currentH = R(rawH);
        }

        #endregion

        #region Transformation Info

        private void DrawTransformationInfo()
        {
            EditorGUILayout.LabelField("📊 Transformation Factors", _sectionStyle);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            float sx = _origW > 0 ? _currentW / _origW : 1f;
            float sy = _origH > 0 ? _currentH / _origH : 1f;
            float dw = R(_currentW - _origW);
            float dh = R(_currentH - _origH);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUI.color = _accentBlue;
            GUILayout.Label("Scale multiplier", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"×{Fmt(sx)}",  _bigNumberStyle);
            GUILayout.Label($"×{Fmt(sy)}",  _bigNumberStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUI.color = _accentOrange;
            GUILayout.Label("Delta offset", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"ΔW {dw:+0.##;-0.##;0}", _bigNumberStyle);
            GUILayout.Label($"ΔH {dh:+0.##;-0.##;0}", _bigNumberStyle);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Apply & Actions

        private void DrawManualApplySection()
        {
            if (_autoApply) return; 

            var targets = GetSelectedSpriteRenderers();
            if (targets.Count == 0) return;

            GUI.backgroundColor = _accentGreen;
            if (GUILayout.Button($"Force Apply ({targets.Count} objects)", GUILayout.Height(30)))
            {
                ApplyToSelection(targets, manualGroup: true);
            }
            GUI.backgroundColor = Color.white;
        }

        private void ApplyToSelection(List<SpriteRenderer> targets, bool manualGroup)
        {
            int groupIndex = 0;
            if (manualGroup)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Manual Apply 2D Scale");
                groupIndex = Undo.GetCurrentGroup();
            }

            foreach (SpriteRenderer sr in targets)
            {
                if (sr == null) continue;

                bool isSized = sr.drawMode is UnityEngine.SpriteDrawMode.Sliced or UnityEngine.SpriteDrawMode.Tiled;

                if (isSized)
                {
                    Undo.RecordObject(sr, "Apply 2D Scale (size)");
                    sr.size = new Vector2(_currentW, _currentH);
                    EditorUtility.SetDirty(sr);
                }
                else
                {
                    if (_preserveScale) continue;

                    Transform t = sr.transform;
                    Undo.RecordObject(t, "Apply 2D Scale (localScale)");

                    float absoluteScaleX = _currentW;
                    float absoluteScaleY = _currentH;

                    if (sr.sprite != null)
                    {
                        Vector2 bounds = sr.sprite.bounds.size;
                        absoluteScaleX = bounds.x > 0 ? _currentW / bounds.x : _currentW;
                        absoluteScaleY = bounds.y > 0 ? _currentH / bounds.y : _currentH;
                    }

                    t.localScale = new Vector3(absoluteScaleX, absoluteScaleY, t.localScale.z);
                    EditorUtility.SetDirty(t);
                }
            }

            if (manualGroup)
            {
                Undo.CollapseUndoOperations(groupIndex);
            }
        }

        private void RestoreInitialStates()
        {
            if (_initialStates.Count == 0) return;

            Undo.RecordObjects(Selection.gameObjects, "Reset Original Values");
            foreach (var kvp in _initialStates)
            {
                if (kvp.Key == null) continue;
                Undo.RecordObject(kvp.Key, "Reset Size");
                Undo.RecordObject(kvp.Key.transform, "Reset Scale");

                kvp.Key.size = kvp.Value.size;
                kvp.Key.transform.localScale = kvp.Value.localScale;
                EditorUtility.SetDirty(kvp.Key);
            }
            
            TryReadFromSelection();
            _currentW = _origW;
            _currentH = _origH;
            _needApply = false;
        }

        private void RoundCurrentSelectionInScene()
        {
            var targets = GetSelectedSpriteRenderers();
            if (targets.Count == 0) return;

            Undo.RecordObjects(Selection.gameObjects, "Round Selection");
            foreach (var sr in targets)
            {
                Undo.RecordObject(sr, "Round Size");
                Undo.RecordObject(sr.transform, "Round Scale");

                sr.size = new Vector2(Mathf.Round(sr.size.x), Mathf.Round(sr.size.y));
                sr.transform.localScale = new Vector3(
                    Mathf.Round(sr.transform.localScale.x), 
                    Mathf.Round(sr.transform.localScale.y), 
                    sr.transform.localScale.z);
                
                EditorUtility.SetDirty(sr);
            }
            CacheInitialStates();
            TryReadFromSelection();
            
            _currentW = _origW;
            _currentH = _origH;
            _needApply = false;
        }

        #endregion

        #region Helpers

        private float DragFloatField(Rect totalRect, string label, float value)
        {
            const float labelWidth = 22f;
            Rect labelRect = new(totalRect.x, totalRect.y, labelWidth, totalRect.height);
            Rect fieldRect = new(totalRect.x + labelWidth, totalRect.y, totalRect.width - labelWidth, totalRect.height);

            int controlId = GUIUtility.GetControlID(DragFloatHash, FocusType.Passive, labelRect);
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.ResizeHorizontal);

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when labelRect.Contains(e.mousePosition) && e.button == 0:
                    GUIUtility.hotControl = controlId;
                    DragState.Start(value, e.mousePosition.x);
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    value = DragState.StartValue + (e.mousePosition.x - DragState.StartMouseX) * 0.5f;
                    GUI.changed = true;
                    Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }

            GUIStyle labelStyle = new(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.55f, 0.77f, 1.0f) },
            };
            GUI.Label(labelRect, label, labelStyle);

            return EditorGUI.FloatField(fieldRect, value);
        }

        private static class DragState
        {
            public static float StartValue;
            public static float StartMouseX;
            public static void Start(float value, float mouseX)
            {
                StartValue = value; StartMouseX = mouseX;
            }
        }

        private void DrawRatioBadge(float w, float h, string prefix = "Ratio:")
        {
            if (w <= 0 || h <= 0) return;
            GUI.color = _accentBlue;
            EditorGUILayout.LabelField($"{prefix}  {GetSimplifiedRatio(w, h)}  ({w / h:F4})", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        private static List<SpriteRenderer> GetSelectedSpriteRenderers()
        {
            var result = new List<SpriteRenderer>();
            foreach (GameObject go in Selection.gameObjects)
                if (go != null && go.TryGetComponent<SpriteRenderer>(out var sr))
                    result.Add(sr);
            return result;
        }

        private static string GetSimplifiedRatio(float w, float h)
        {
            if (w <= 0 || h <= 0) return "N/A";
            int iw = Mathf.RoundToInt(w * 1000);
            int ih = Mathf.RoundToInt(h * 1000);
            if (iw == 0 || ih == 0) return $"{w / h:F2}:1";

            int g  = Gcd(iw, ih);
            int sw = iw / g;
            int sh = ih / g;

            if (sw > 200 || sh > 200)
            {
                float ratio = (float)sw / sh;
                (int a, int b)[] known = { (1,1),(4,3),(3,2),(16,9),(16,10),(21,9),(2,3),(9,16),(3,4),(1,2),(2,1) };
                foreach ((int a, int b) in known)
                    if (Mathf.Abs(ratio - (float)a / b) < 0.01f) return $"{a}:{b}";
                return $"{w / h:F4}:1";
            }
            return $"{sw}:{sh}";
        }

        private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

        #endregion

        #region Styles

        private void InitStyles()
        {
            if (_stylesInited) return;
            _stylesInited = true;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.95f, 0.95f, 0.95f) },
            };
            _sectionStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontSize  = 12, fontStyle = FontStyle.Bold,
            };
            _resultBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
            };
            _bigNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            };
        }

        #endregion
    }
}