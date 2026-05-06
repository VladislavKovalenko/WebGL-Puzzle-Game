using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Локальный визуализатор Grid Layout Group — работает ТОЛЬКО на объекте, на котором висит скрипт.
    /// Показывает сетку, padding, spacing и параметры ячеек.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VisualizeGridLocal : MonoBehaviour
    {
        [Header("Компоненты для визуализации")]
        [SerializeField] private bool _showGridLayout = true;
        [SerializeField] private bool _showContentSizeFitter = true;
        [SerializeField] private bool _showLayoutElement = true;
        
        [Header("Цвета")]
        [SerializeField] private Color _gridColor = new Color(0.5f, 0f, 1f, 0.4f);
        [SerializeField] private Color _contentFitterColor = new Color(1f, 0.6f, 0f, 0.4f);
        [SerializeField] private Color _layoutElementColor = new Color(0.4f, 1f, 0.4f, 0.4f);
        [SerializeField] private Color _labelColor = Color.white;
        
        [Header("Настройки отображения")]
        [SerializeField] private float _borderThickness = 2f;
        [SerializeField] private bool _showLabels = true;
        [SerializeField] private bool _showPadding = true;
        [SerializeField] private bool _showSpacing = true;
        [SerializeField] private bool _showCellOutlines = true;
        [SerializeField] private bool _showGridLines = true;
        
        [Header("Padding")]
        [Tooltip("Толщина линий Padding")]
        [SerializeField] private float _paddingLineThickness = 1.5f;
        
        [Header("Spacing")]
        [Tooltip("Цвет заливки Spacing между ячейками")]
        [SerializeField] private Color _spacingFillColor = new Color(1f, 0.4f, 0.7f, 0.2f);
        [Tooltip("Цвет линий Spacing")]
        [SerializeField] private Color _spacingLineColor = new Color(1f, 0.4f, 0.7f, 0.6f);
        
        [Header("Сетка")]
        [Tooltip("Цвет линий сетки")]
        [SerializeField] private Color _gridLinesColor = new Color(0.7f, 0.3f, 1f, 0.3f);
        [Tooltip("Толщина линий сетки")]
        [SerializeField] private float _gridLineThickness = 1f;
        [Tooltip("Цвет контуров ячеек")]
        [SerializeField] private Color _cellOutlineColor = new Color(0.5f, 0f, 1f, 0.5f);
        [Tooltip("Толщина контуров ячеек")]
        [SerializeField] private float _cellOutlineThickness = 1.5f;
        
        [Header("Размер шрифта Labels")]
        [Tooltip("Размер шрифта основных меток")]
        [SerializeField] private int _mainLabelFontSize = 11;
        [Tooltip("Размер шрифта вспомогательных меток")]
        [SerializeField] private int _subLabelFontSize = 10;
        
        private GUIStyle _labelStyle;
        private GUIStyle _subLabelStyle;
        private bool _stylesDirty = true;
        
        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (!enabled || !gameObject.activeInHierarchy) return;
            
            InitializeStyles();
            DrawThisObjectOnly();
            #endif
        }
        
        private void OnDisable()
        {
            #if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
            #endif
        }
        
        private void OnValidate()
        {
            _stylesDirty = true;
            #if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
            #endif
        }
        
        private void InitializeStyles()
        {
            if (_labelStyle == null || _stylesDirty)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _mainLabelFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = _labelColor }
                };
            }
            
            if (_subLabelStyle == null || _stylesDirty)
            {
                _subLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _subLabelFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
                };
            }
            
            _stylesDirty = false;
        }
        
        private void DrawThisObjectOnly()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            bool drewAnything = false;
            
            if (_showGridLayout)
            {
                var gridLayout = GetComponent<GridLayoutGroup>();
                if (gridLayout != null)
                {
                    DrawGridLayout(corners, gridLayout);
                    drewAnything = true;
                }
            }
            
            if (_showContentSizeFitter)
            {
                var fitter = GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    DrawContentSizeFitter(corners, fitter);
                    drewAnything = true;
                }
            }
            
            if (_showLayoutElement)
            {
                var layoutElement = GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    DrawLayoutElement(corners, layoutElement);
                    drewAnything = true;
                }
            }
            
            if (!drewAnything && _showLabels)
            {
                DrawEmptyHint(corners);
            }
        }
        
        private void DrawGridLayout(Vector3[] corners, GridLayoutGroup grid)
        {
            DrawQuadBorder(corners, _gridColor, _borderThickness);
            
            if (_showPadding)
            {
                DrawPadding(corners, grid);
            }
            
            // Рисуем сетку и ячейки
            DrawGrid(corners, grid);
            
            if (_showLabels)
            {
                string label = $"⊞ Grid Layout\n" +
                              $"Cell: {grid.cellSize.x:F0}x{grid.cellSize.y:F0}\n" +
                              $"Spacing: {grid.spacing.x:F0}x{grid.spacing.y:F0}\n" +
                              $"Constraint: {grid.constraint}";
                
                if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    label += $" ({grid.constraintCount} cols)";
                }
                else if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    label += $" ({grid.constraintCount} rows)";
                }
                
                DrawLabel(corners, label, _gridColor);
            }
        }
        
        private void DrawGrid(Vector3[] corners, GridLayoutGroup grid)
        {
            Vector3 bottomLeft = corners[0];
            Vector3 topLeft = corners[1];
            Vector3 topRight = corners[2];
            Vector3 bottomRight = corners[3];
            
            float width = Vector3.Distance(bottomLeft, bottomRight);
            float height = Vector3.Distance(bottomLeft, topLeft);
            
            Vector3 right = (bottomRight - bottomLeft).normalized;
            Vector3 up = (topLeft - bottomLeft).normalized;
            
            // Учитываем padding
            float paddingLeft = grid.padding.left;
            float paddingRight = grid.padding.right;
            float paddingTop = grid.padding.top;
            float paddingBottom = grid.padding.bottom;
            
            // Стартовая позиция сетки (после padding)
            Vector3 gridStart = bottomLeft + right * paddingLeft + up * paddingBottom;
            float availableWidth = width - paddingLeft - paddingRight;
            float availableHeight = height - paddingTop - paddingBottom;
            
            // Размеры ячейки и spacing
            Vector2 cellSize = grid.cellSize;
            Vector2 spacing = grid.spacing;
            
            // Вычисляем количество столбцов и строк
            int columns = 0;
            int rows = 0;
            int activeChildren = 0;
            
            // Считаем активных детей
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeInHierarchy)
                    activeChildren++;
            }
            
            // Определяем количество столбцов и строк
            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                columns = grid.constraintCount;
                rows = Mathf.CeilToInt((float)activeChildren / columns);
            }
            else if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                rows = grid.constraintCount;
                columns = Mathf.CeilToInt((float)activeChildren / rows);
            }
            else // Flexible
            {
                columns = Mathf.FloorToInt((availableWidth + spacing.x) / (cellSize.x + spacing.x));
                columns = Mathf.Max(1, columns);
                rows = Mathf.CeilToInt((float)activeChildren / columns);
            }
            
            // Рисуем ячейки и spacing
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int cellIndex = row * columns + col;
                    if (cellIndex >= activeChildren) break;
                    
                    // Позиция ячейки
                    float xPos = col * (cellSize.x + spacing.x);
                    float yPos = row * (cellSize.y + spacing.y);
                    
                    Vector3 cellBottomLeft = gridStart + right * xPos + up * yPos;
                    Vector3 cellBottomRight = cellBottomLeft + right * cellSize.x;
                    Vector3 cellTopLeft = cellBottomLeft + up * cellSize.y;
                    Vector3 cellTopRight = cellTopLeft + right * cellSize.x;
                    
                    Vector3[] cellCorners = new Vector3[4]
                    {
                        cellBottomLeft,
                        cellTopLeft,
                        cellTopRight,
                        cellBottomRight
                    };
                    
                    // Контур ячейки
                    if (_showCellOutlines)
                    {
                        DrawQuadBorder(cellCorners, _cellOutlineColor, _cellOutlineThickness);
                    }
                    
                    // Spacing справа от ячейки (кроме последнего столбца)
                    if (_showSpacing && col < columns - 1 && spacing.x > 0)
                    {
                        Vector3 spacingBottomLeft = cellBottomRight;
                        Vector3 spacingTopLeft = cellTopRight;
                        Vector3 spacingBottomRight = spacingBottomLeft + right * spacing.x;
                        Vector3 spacingTopRight = spacingTopLeft + right * spacing.x;
                        
                        Vector3[] spacingCorners = new Vector3[4]
                        {
                            spacingBottomLeft,
                            spacingTopLeft,
                            spacingTopRight,
                            spacingBottomRight
                        };
                        
                        DrawFilledQuad(spacingCorners, _spacingFillColor);
                        
                        if (_showLabels && spacing.x > 15)
                        {
                            Vector3 labelPos = (spacingBottomLeft + spacingTopRight) * 0.5f;
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(labelPos, $"sx:{spacing.x:F0}", _subLabelStyle);
                            #endif
                        }
                    }
                    
                    // Spacing снизу от ячейки (кроме последней строки)
                    if (_showSpacing && row < rows - 1 && spacing.y > 0)
                    {
                        Vector3 spacingBottomLeft = cellTopLeft;
                        Vector3 spacingBottomRight = cellTopRight;
                        Vector3 spacingTopLeft = spacingBottomLeft + up * spacing.y;
                        Vector3 spacingTopRight = spacingBottomRight + up * spacing.y;
                        
                        Vector3[] spacingCorners = new Vector3[4]
                        {
                            spacingBottomLeft,
                            spacingTopLeft,
                            spacingTopRight,
                            spacingBottomRight
                        };
                        
                        DrawFilledQuad(spacingCorners, _spacingFillColor);
                        
                        if (_showLabels && spacing.y > 15)
                        {
                            Vector3 labelPos = (spacingBottomLeft + spacingTopRight) * 0.5f;
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(labelPos, $"sy:{spacing.y:F0}", _subLabelStyle);
                            #endif
                        }
                    }
                    
                    // Spacing в углу (справа-снизу от ячейки)
                    if (_showSpacing && col < columns - 1 && row < rows - 1 && spacing.x > 0 && spacing.y > 0)
                    {
                        Vector3 cornerBottomLeft = cellTopRight;
                        Vector3 cornerBottomRight = cornerBottomLeft + right * spacing.x;
                        Vector3 cornerTopLeft = cornerBottomLeft + up * spacing.y;
                        Vector3 cornerTopRight = cornerTopLeft + right * spacing.x;
                        
                        Vector3[] cornerCorners = new Vector3[4]
                        {
                            cornerBottomLeft,
                            cornerTopLeft,
                            cornerTopRight,
                            cornerBottomRight
                        };
                        
                        DrawFilledQuad(cornerCorners, _spacingFillColor);
                    }
                }
            }
            
            // Рисуем линии сетки
            if (_showGridLines && (columns > 1 || rows > 1))
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.color = _gridLinesColor;
                
                // Вертикальные линии
                for (int col = 1; col < columns; col++)
                {
                    float xPos = col * (cellSize.x + spacing.x);
                    Vector3 lineBottom = gridStart + right * xPos;
                    Vector3 lineTop = lineBottom + up * (rows * cellSize.y + (rows - 1) * spacing.y);
                    
                    UnityEditor.Handles.DrawDottedLine(lineBottom, lineTop, 4f);
                }
                
                // Горизонтальные линии
                for (int row = 1; row < rows; row++)
                {
                    float yPos = row * (cellSize.y + spacing.y);
                    Vector3 lineLeft = gridStart + up * yPos;
                    Vector3 lineRight = lineLeft + right * (columns * cellSize.x + (columns - 1) * spacing.x);
                    
                    UnityEditor.Handles.DrawDottedLine(lineLeft, lineRight, 4f);
                }
                #endif
            }
        }
        
        private void DrawContentSizeFitter(Vector3[] corners, ContentSizeFitter fitter)
        {
            DrawQuadBorder(corners, _contentFitterColor, _borderThickness);
            
            if (_showLabels)
            {
                string label = $"Content Size Fitter\nH: {fitter.horizontalFit}\nV: {fitter.verticalFit}";
                DrawLabel(corners, label, _contentFitterColor);
            }
        }
        
        private void DrawLayoutElement(Vector3[] corners, LayoutElement element)
        {
            DrawQuadBorder(corners, _layoutElementColor, _borderThickness);
            
            if (_showLabels)
            {
                string label = $"Layout Element\n" +
                              $"Min: {element.minWidth:F0}x{element.minHeight:F0}\n" +
                              $"Pref: {element.preferredWidth:F0}x{element.preferredHeight:F0}\n" +
                              $"Flex: {element.flexibleWidth:F1}x{element.flexibleHeight:F1}\n" +
                              $"Ignore: {element.ignoreLayout}";
                DrawLabel(corners, label, _layoutElementColor);
            }
        }
        
        private void DrawEmptyHint(Vector3[] corners)
        {
            Color hintColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            DrawQuadBorder(corners, hintColor, 1f);
            
            string hint = "No Layout Components\n(GridLayoutGroup / ContentSizeFitter / LayoutElement)";
            DrawLabel(corners, hint, hintColor);
        }
        
        private void DrawPadding(Vector3[] corners, LayoutGroup group)
        {
            float paddingLeft = group.padding.left;
            float paddingRight = group.padding.right;
            float paddingTop = group.padding.top;
            float paddingBottom = group.padding.bottom;
            
            Vector3 bottomLeft = corners[0];
            Vector3 topLeft = corners[1];
            Vector3 topRight = corners[2];
            Vector3 bottomRight = corners[3];
            
            float width = Vector3.Distance(bottomLeft, bottomRight);
            float height = Vector3.Distance(bottomLeft, topLeft);
            
            Vector3 right = (bottomRight - bottomLeft).normalized;
            Vector3 up = (topLeft - bottomLeft).normalized;
            
            Color paddingFillColor = new Color(_gridColor.r, _gridColor.g, _gridColor.b, 0.25f);
            Color paddingLineColor = new Color(_gridColor.r, _gridColor.g, _gridColor.b, 0.6f);
            
            // Left Padding
            if (paddingLeft > 0)
            {
                Vector3 p1 = bottomLeft + right * paddingLeft;
                Vector3 p2 = topLeft + right * paddingLeft;
                
                Vector3[] leftPaddingCorners = new Vector3[4]
                {
                    bottomLeft,
                    topLeft,
                    p2,
                    p1
                };
                
                DrawFilledQuad(leftPaddingCorners, paddingFillColor);
                DrawQuadBorder(leftPaddingCorners, paddingLineColor, _paddingLineThickness);
                
                if (_showLabels && paddingLeft > 20)
                {
                    Vector3 labelPos = bottomLeft + right * (paddingLeft * 0.5f) + up * (height * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"L:{paddingLeft}", _subLabelStyle);
                    #endif
                }
            }
            
            // Right Padding
            if (paddingRight > 0)
            {
                Vector3 p1 = bottomRight - right * paddingRight;
                Vector3 p2 = topRight - right * paddingRight;
                
                Vector3[] rightPaddingCorners = new Vector3[4]
                {
                    p1,
                    p2,
                    topRight,
                    bottomRight
                };
                
                DrawFilledQuad(rightPaddingCorners, paddingFillColor);
                DrawQuadBorder(rightPaddingCorners, paddingLineColor, _paddingLineThickness);
                
                if (_showLabels && paddingRight > 20)
                {
                    Vector3 labelPos = bottomRight - right * (paddingRight * 0.5f) + up * (height * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"R:{paddingRight}", _subLabelStyle);
                    #endif
                }
            }
            
            // Top Padding
            if (paddingTop > 0)
            {
                Vector3 p1 = topLeft + up * -paddingTop;
                Vector3 p2 = topRight + up * -paddingTop;
                
                Vector3[] topPaddingCorners = new Vector3[4]
                {
                    p1,
                    topLeft,
                    topRight,
                    p2
                };
                
                DrawFilledQuad(topPaddingCorners, paddingFillColor);
                DrawQuadBorder(topPaddingCorners, paddingLineColor, _paddingLineThickness);
                
                if (_showLabels && paddingTop > 15)
                {
                    Vector3 labelPos = topLeft + right * (width * 0.5f) + up * (-paddingTop * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"T:{paddingTop}", _subLabelStyle);
                    #endif
                }
            }
            
            // Bottom Padding
            if (paddingBottom > 0)
            {
                Vector3 p1 = bottomLeft + up * paddingBottom;
                Vector3 p2 = bottomRight + up * paddingBottom;
                
                Vector3[] bottomPaddingCorners = new Vector3[4]
                {
                    bottomLeft,
                    p1,
                    p2,
                    bottomRight
                };
                
                DrawFilledQuad(bottomPaddingCorners, paddingFillColor);
                DrawQuadBorder(bottomPaddingCorners, paddingLineColor, _paddingLineThickness);
                
                if (_showLabels && paddingBottom > 15)
                {
                    Vector3 labelPos = bottomLeft + right * (width * 0.5f) + up * (paddingBottom * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"B:{paddingBottom}", _subLabelStyle);
                    #endif
                }
            }
        }
        
        private void DrawFilledQuad(Vector3[] corners, Color color)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAConvexPolygon(corners);
            #endif
        }
        
        private void DrawQuadBorder(Vector3[] corners, Color color, float thickness)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            
            UnityEditor.Handles.DrawLine(corners[0], corners[1], thickness);
            UnityEditor.Handles.DrawLine(corners[1], corners[2], thickness);
            UnityEditor.Handles.DrawLine(corners[2], corners[3], thickness);
            UnityEditor.Handles.DrawLine(corners[3], corners[0], thickness);
            #endif
        }
        
        private void DrawLabel(Vector3[] corners, string text, Color bgColor)
        {
            #if UNITY_EDITOR
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            UnityEditor.Handles.Label(center, text, _labelStyle);
            #endif
        }
    }
}