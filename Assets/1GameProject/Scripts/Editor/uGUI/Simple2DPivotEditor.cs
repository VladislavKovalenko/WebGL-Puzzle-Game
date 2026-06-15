using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace _1GameProject.Scripts.Editor.UI
{
    /// <summary>
    /// Расширенный редактор Pivot для RectTransform.
    /// Визуальный интерактивный редактор с пресетами, сеткой и мультивыбором.
    /// </summary>
    public class Simple2DPivotEditor : EditorWindow
    {
        #region Fields
        
        private Vector2 _pivot;
        private Vector2 _prevPivot;
        private bool _showVisualEditor = true;
        private bool _showPresets = true;
        private bool _showAdvanced = false;
        private bool _snapToGrid = true;
        private float _gridSize = 0.5f;
        private bool _previewMode = true;
        private bool _affectChildren = false;
        private Color _gridColor = new Color(1f, 1f, 1f, 0.15f);
        private Color _pivotColor = new Color(1f, 0.5f, 0f, 1f);
        private Color _hoverColor = new Color(1f, 0.8f, 0.3f, 1f);
        private Color _activeColor = new Color(0.2f, 0.8f, 1f, 1f);
        
        private Vector2 _visualEditorSize = new Vector2(200, 200);
        private Rect _visualRect;
        private bool _isDragging;
        private int _selectedPreset = -1;
        private float _handleSize = 12f;
        
        private GUIStyle _presetButtonStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _sectionStyle;
        
        private static readonly Vector2[] PresetPivots = new[]
        {
            new Vector2(0.0f, 1.0f), new Vector2(0.5f, 1.0f), new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f),
            new Vector2(0.0f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.0f),
        };
        
        private static readonly string[] PresetLabels = new[]
        {
            "TL", "TC", "TR",
            "ML", "MC", "MR",
            "BL", "BC", "BR",
        };
        
        private static readonly string[] PresetTooltips = new[]
        {
            "Top-Left", "Top-Center", "Top-Right",
            "Middle-Left", "Middle-Center", "Middle-Right",
            "Bottom-Left", "Bottom-Center", "Bottom-Right",
        };
        
        #endregion
        
        #region Editor Window
        
        [MenuItem("Tools/Megxlord uGUI/Simple2DPivotEditor", priority = 100)]
        public static void ShowWindow() => GetWindow<Simple2DPivotEditor>("Pivot Editor");
        
        private void OnEnable()
        {
            wantsMouseMove = true;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
            SyncPivotFromSelection();
        }
        
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnSelectionChanged()
        {
            SyncPivotFromSelection();
            Repaint();
        }

        private void SyncPivotFromSelection()
        {
            var targets = GetValidTargets();
            if (targets.Count == 1)
            {
                _pivot = targets[0].pivot;
                _prevPivot = _pivot;
            }
            else if (targets.Count > 1)
            {
                // Для мультивыбора берём pivot первого объекта как референс
                _pivot = targets[0].pivot;
                _prevPivot = _pivot;
            }
            _selectedPreset = GetPresetIndex(_pivot);
        }

        private int GetPresetIndex(Vector2 pivot)
        {
            for (int i = 0; i < PresetPivots.Length; i++)
            {
                if (Mathf.Approximately(pivot.x, PresetPivots[i].x) && 
                    Mathf.Approximately(pivot.y, PresetPivots[i].y))
                    return i;
            }
            return -1;
        }

        private void OnEditorUpdate() { if (_previewMode && _isDragging) Repaint(); }
        
        #endregion
        
        #region GUI
        
        private void OnGUI()
        {
            InitStyles();
            
            EditorGUILayout.Space(5);
            
            // Header
            EditorGUILayout.LabelField("Simple2D Pivot Editor", _titleStyle);
            EditorGUILayout.Space(10);
            
            // Target info
            DrawTargetInfo();
            EditorGUILayout.Space(10);
            
            // Visual Editor
            if (_showVisualEditor = EditorGUILayout.Foldout(_showVisualEditor, "▣ Visual Editor", true, _sectionStyle))
                DrawVisualEditor();
            
            EditorGUILayout.Space(5);
            
            // Presets
            if (_showPresets = EditorGUILayout.Foldout(_showPresets, "⊞ Quick Presets", true, _sectionStyle))
                DrawPresets();
            
            EditorGUILayout.Space(5);
            
            // Numeric input
            DrawNumericInput();
            
            EditorGUILayout.Space(5);
            
            // Advanced
            if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "⚙ Advanced", true, _sectionStyle))
                DrawAdvanced();
            
            EditorGUILayout.Space(10);
            
            // Apply buttons
            DrawActionButtons();
            
            // Status bar
            EditorGUILayout.Space(5);
            DrawStatusBar();
            
            HandleMouseEvents();
        }
        
        #endregion
        
        #region Drawing
        
        private void InitStyles()
        {
            if (_presetButtonStyle != null) return;
            
            _presetButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 32,
                fixedWidth = 50,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(0, 0, 0, 0),
            };
            
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
            };
            
            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            
            _sectionStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
            };
        }
        
        private void DrawTargetInfo()
        {
            var targets = GetValidTargets();
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (targets.Count == 0)
            {
                EditorGUILayout.HelpBox("Select one or more GameObjects with RectTransform", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Targets: {targets.Count}", EditorStyles.boldLabel);
                
                if (targets.Count == 1)
                {
                    var rt = targets[0];
                    EditorGUILayout.LabelField($"Object: {rt.name}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Current Pivot: ({rt.pivot.x:F3}, {rt.pivot.y:F3})", _valueStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("Multi-selection mode active", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"New Pivot: ({_pivot.x:F3}, {_pivot.y:F3})", _valueStyle);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawVisualEditor()
        {
            var rect = GUILayoutUtility.GetRect(_visualEditorSize.x, _visualEditorSize.y, 
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            
            rect.x += (position.width - _visualEditorSize.x) * 0.5f;
            _visualRect = rect;
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // Border
            Handles.BeginGUI();
            Handles.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.4f, 0.4f, 0.4f, 1f));
            
            // Grid
            if (_snapToGrid && _gridSize > 0)
            {
                Handles.color = _gridColor;
                int steps = Mathf.RoundToInt(1f / _gridSize);
                
                for (int i = 1; i < steps; i++)
                {
                    float t = i / (float)steps;
                    // Vertical
                    Handles.DrawLine(
                        new Vector3(rect.x + rect.width * t, rect.y, 0),
                        new Vector3(rect.x + rect.width * t, rect.y + rect.height, 0));
                    // Horizontal
                    Handles.DrawLine(
                        new Vector3(rect.x, rect.y + rect.height * t, 0),
                        new Vector3(rect.x + rect.width, rect.y + rect.height * t, 0));
                }
            }
            
            // Crosshair center
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            Handles.DrawLine(new Vector3(center.x, rect.y, 0), new Vector3(center.x, rect.y + rect.height, 0));
            Handles.DrawLine(new Vector3(rect.x, center.y, 0), new Vector3(rect.x + rect.width, center.y, 0));
            
            // Pivot position (Unity coords: bottom-left is 0,0, GUI: top-left is 0,0)
            float px = rect.x + rect.width * _pivot.x;
            float py = rect.y + rect.height * (1f - _pivot.y);
            
            // Hover detection
            Vector2 mousePos = Event.current.mousePosition;
            float dist = Vector2.Distance(mousePos, new Vector2(px, py));
            bool isHover = dist < _handleSize * 1.5f && rect.Contains(mousePos);
            
            // Draw pivot cross
            float crossSize = _handleSize * 2f;
            Color crossColor = _isDragging ? _activeColor : (isHover ? _hoverColor : _pivotColor);
            
            Handles.color = crossColor;
            Handles.DrawLine(new Vector3(px - crossSize, py, 0), new Vector3(px + crossSize, py, 0));
            Handles.DrawLine(new Vector3(px, py - crossSize, 0), new Vector3(px, py + crossSize, 0));
            
            // Draw pivot circle
            Handles.color = new Color(crossColor.r, crossColor.g, crossColor.b, 0.3f);
            Handles.DrawSolidDisc(new Vector3(px, py, 0), Vector3.forward, _handleSize);
            Handles.color = crossColor;
            Handles.DrawWireDisc(new Vector3(px, py, 0), Vector3.forward, _handleSize);
            
            // Draw inner dot
            float dotSize = _isDragging ? 4f : 3f;
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(px, py, 0), Vector3.forward, dotSize);
            
            // Labels
            GUI.color = crossColor;
            string label = $"({_pivot.x:F2}, {_pivot.y:F2})";
            Vector2 labelSize = GUI.skin.label.CalcSize(new GUIContent(label));
            GUI.Label(new Rect(px + 15, py - labelSize.y * 0.5f, labelSize.x, labelSize.y), label);
            GUI.color = Color.white;
            
            Handles.EndGUI();
            
            // Instructions
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Drag the crosshair or click anywhere in the box", EditorStyles.centeredGreyMiniLabel);
        }
        
        private void DrawPresets()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical();
            
            for (int row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int idx = row * 3 + col;
                    bool isSelected = Mathf.Approximately(_pivot.x, PresetPivots[idx].x) && 
                                      Mathf.Approximately(_pivot.y, PresetPivots[idx].y);
                    
                    Color prevBg = GUI.backgroundColor;
                    if (isSelected) GUI.backgroundColor = _activeColor;
                    
                    if (GUILayout.Button(new GUIContent(PresetLabels[idx], PresetTooltips[idx]), _presetButtonStyle))
                    {
                        _prevPivot = _pivot;
                        _pivot = PresetPivots[idx];
                        _selectedPreset = idx;
                        ApplyPivot();
                    }
                    
                    GUI.backgroundColor = prevBg;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNumericInput()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(20));
            _pivot.x = EditorGUILayout.Slider(_pivot.x, 0f, 1f);
            if (GUILayout.Button("0", GUILayout.Width(25))) _pivot.x = 0f;
            if (GUILayout.Button("½", GUILayout.Width(25))) _pivot.x = 0.5f;
            if (GUILayout.Button("1", GUILayout.Width(25))) _pivot.x = 1f;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Y", GUILayout.Width(20));
            _pivot.y = EditorGUILayout.Slider(_pivot.y, 0f, 1f);
            if (GUILayout.Button("0", GUILayout.Width(25))) _pivot.y = 0f;
            if (GUILayout.Button("½", GUILayout.Width(25))) _pivot.y = 0.5f;
            if (GUILayout.Button("1", GUILayout.Width(25))) _pivot.y = 1f;
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                _pivot.x = Mathf.Clamp01(_pivot.x);
                _pivot.y = Mathf.Clamp01(_pivot.y);
                ApplyPivot();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdvanced()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            _snapToGrid = EditorGUILayout.Toggle("Snap to Grid", _snapToGrid);
            if (_snapToGrid)
            {
                EditorGUI.indentLevel++;
                _gridSize = EditorGUILayout.Slider("Grid Size", _gridSize, 0.05f, 0.5f);
                EditorGUI.indentLevel--;
            }
            
            _previewMode = EditorGUILayout.Toggle("Live Preview", _previewMode);
            _affectChildren = EditorGUILayout.Toggle("Affect Children Anchors", _affectChildren);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            _gridColor = EditorGUILayout.ColorField("Grid", _gridColor);
            _pivotColor = EditorGUILayout.ColorField("Pivot", _pivotColor);
            _hoverColor = EditorGUILayout.ColorField("Hover", _hoverColor);
            _activeColor = EditorGUILayout.ColorField("Active", _activeColor);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("↺ Reset to Center", GUILayout.Height(30)))
            {
                _prevPivot = _pivot;
                _pivot = new Vector2(0.5f, 0.5f);
                ApplyPivot();
            }
            
            if (GUILayout.Button("⎌ Undo Last", GUILayout.Height(30)))
            {
                (_pivot, _prevPivot) = (_prevPivot, _pivot);
                ApplyPivot();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            if (GUILayout.Button("✓ Apply to Selection", GUILayout.Height(35)))
            {
                ApplyPivot(true);
                ShowNotification(new GUIContent("Pivot applied!"));
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            var rect = GUILayoutUtility.GetRect(position.width, 20);
            GUI.Label(rect, "Shortcuts: Alt+P — Open | Drag visual editor — Set pivot", EditorStyles.miniLabel);
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleMouseEvents()
        {
            Event e = Event.current;
            if (e == null) return;
            
            if (e.type == EventType.MouseDown && e.button == 0 && _visualRect.Contains(e.mousePosition))
            {
                _isDragging = true;
                _prevPivot = _pivot;
                UpdatePivotFromMouse(e.mousePosition);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                UpdatePivotFromMouse(e.mousePosition);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                ApplyPivot(true);
                e.Use();
            }
            else if (e.type == EventType.MouseMove)
            {
                Repaint();
            }
        }
        
        private void UpdatePivotFromMouse(Vector2 mousePos)
        {
            float x = Mathf.InverseLerp(_visualRect.x, _visualRect.x + _visualRect.width, mousePos.x);
            float y = 1f - Mathf.InverseLerp(_visualRect.y, _visualRect.y + _visualRect.height, mousePos.y);
            
            if (_snapToGrid && _gridSize > 0)
            {
                x = Mathf.Round(x / _gridSize) * _gridSize;
                y = Mathf.Round(y / _gridSize) * _gridSize;
            }
            
            _pivot = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
            
            if (_previewMode) ApplyPivot();
        }
        
        #endregion
        
        #region Logic
        
        private List<UnityEngine.RectTransform> GetValidTargets()
        {
            var result = new List<UnityEngine.RectTransform>();
            foreach (var go in Selection.gameObjects)
                if (go.TryGetComponent<UnityEngine.RectTransform>(out var rt))
                    result.Add(rt);
            return result;
        }
        
        private void ApplyPivot(bool recordUndo = false)
        {
            var targets = GetValidTargets();
            if (targets.Count == 0) return;
            
            foreach (var rt in targets)
            {
                if (rt == null) continue;
                
                if (recordUndo)
                    Undo.RecordObject(rt, "Change Pivot");
                
                // Сохраняем мировую позицию при изменении pivot
                Vector3 worldPos = rt.position;
                
                rt.pivot = _pivot;
                
                // Восстанавливаем позицию, чтобы объект не сдвинулся визуально
                if (_previewMode)
                    rt.position = worldPos;
                
                if (_affectChildren)
                    AdjustChildrenAnchors(rt);
                
                EditorUtility.SetDirty(rt);
            }
        }
        
        private void AdjustChildrenAnchors(UnityEngine.RectTransform parent)
        {
            foreach (UnityEngine.RectTransform child in parent)
            {
                Undo.RecordObject(child, "Adjust Child Anchors");
                
                // Нормализуем anchor относительно нового pivot родителя
                Vector2 anchorMin = child.anchorMin;
                Vector2 anchorMax = child.anchorMax;
                
                // Сохраняем относительное положение
                Vector2 offsetMin = child.offsetMin;
                Vector2 offsetMax = child.offsetMax;
                
                child.offsetMin = offsetMin;
                child.offsetMax = offsetMax;
                
                EditorUtility.SetDirty(child);
            }
        }
        
        #endregion
    }
    
    // /// <summary>
    // /// Интеграция в Inspector — кнопка быстрого открытия рядом с полем Pivot
    // /// </summary>
    // [CustomEditor(typeof(UnityEngine.RectTransform), true)]
    // public class RectTransformPivotInspector : UnityEditor.Editor
    // {
    //     public override void OnInspectorGUI()
    //     {
    //         DrawDefaultInspector();
    //         
    //         EditorGUILayout.Space(5);
    //         
    //         EditorGUILayout.BeginHorizontal();
    //         
    //         EditorGUILayout.LabelField("Quick Pivot", GUILayout.Width(70));
    //         
    //         if (GUILayout.Button("⊞", GUILayout.Width(30)))
    //             Simple2DPivotEditor.ShowWindow();
    //         
    //         // Быстрые пресеты прямо в инспекторе
    //         var rt = target as UnityEngine.RectTransform;
    //         
    //         if (GUILayout.Button("Center", GUILayout.Width(50)))
    //             SetPivot(rt, new Vector2(0.5f, 0.5f));
    //         
    //         if (GUILayout.Button("TL", GUILayout.Width(30)))
    //             SetPivot(rt, new Vector2(0f, 1f));
    //         
    //         if (GUILayout.Button("BR", GUILayout.Width(30)))
    //             SetPivot(rt, new Vector2(1f, 0f));
    //         
    //         EditorGUILayout.EndHorizontal();
    //     }
    //     
    //     private void SetPivot(UnityEngine.RectTransform rt, Vector2 pivot)
    //     {
    //         if (rt == null) return;
    //         Undo.RecordObject(rt, "Set Pivot");
    //         Vector3 worldPos = rt.position;
    //         rt.pivot = pivot;
    //         rt.position = worldPos;
    //         EditorUtility.SetDirty(rt);
    //     }
    // }
    
    //ОБЯЗАТЕЛЬНО ВКЛЮЧИТЬ ОТДЕЛЬНО кнопку В TOOLBAR
    
    [InitializeOnLoad]
    public static class PivotEditorToolbar
    {
        private const string ElementId = "Megxlord/PivotEditor";

        static PivotEditorToolbar()
        {
            //Debug.Log("PivotEditorToolbar initialized"); удобно для дебага
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateButton()
        {
            var content = new MainToolbarContent(
                "Pivot",
                tooltip: "Open Pivot Editor"
            );

            return new MainToolbarButton(content, OpenWindow);
        }

        private static void OpenWindow()
        {
            Simple2DPivotEditor.ShowWindow();
        }
    }
    
    
}