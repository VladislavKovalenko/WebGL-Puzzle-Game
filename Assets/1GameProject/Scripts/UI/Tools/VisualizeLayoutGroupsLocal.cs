using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Локальный визуализатор Layout Groups — работает ТОЛЬКО на объекте, на котором висит скрипт.
    /// Заливка только для Padding областей, для всей группы — только обводка рамкой.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VisualizeLayoutGroupsLocal : MonoBehaviour
    {
        [Header("Компоненты для визуализации")]
        [SerializeField] private bool _showLayoutGroup = true;
        [SerializeField] private bool _showContentSizeFitter = true;
        [SerializeField] private bool _showLayoutElement = true;
        
        [Header("Цвета")]
        [SerializeField] private Color _layoutGroupColor = new Color(0f, 0.8f, 1f, 0.4f);
        [SerializeField] private Color _contentFitterColor = new Color(1f, 0.6f, 0f, 0.4f);
        [SerializeField] private Color _layoutElementColor = new Color(0.4f, 1f, 0.4f, 0.4f);
        [SerializeField] private Color _borderColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color _labelColor = Color.white;
        
        [Header("Настройки отображения")]
        [SerializeField] private float _borderThickness = 2f;
        [SerializeField] private bool _showLabels = true;
        [SerializeField] private bool _showPadding = true;
        [SerializeField] private bool _showSpacing = true;
        
        [Header("Padding")]
        [Tooltip("Количество точек на пунктирной линии Padding")]
        [SerializeField] private float _paddingLineDots = 4f;
        [Tooltip("Толщина линий Padding")]
        [SerializeField] private float _paddingLineThickness = 1.5f;
        
        [Header("Spacing")]
        [Tooltip("Цвет заливки Spacing между элементами")]
        [SerializeField] private Color _spacingFillColor = new Color(1f, 0.4f, 0.7f, 0.3f);
        [Tooltip("Цвет пунктирной линии Spacing = 0 (разделитель)")]
        [SerializeField] private Color _spacingZeroLineColor = new Color(1f, 0.4f, 0.7f, 0.8f);
        [Tooltip("Количество точек на пунктирной линии Spacing = 0 (-2 = автоматический подбор)")]
        [SerializeField] private float _spacingZeroLineDots = -2f;
        [Tooltip("Толщина пунктирной линии Spacing = 0")]
        [SerializeField] private float _spacingZeroLineThickness = 2f;
        
        [Header("Размер шрифта Labels")]
        [Tooltip("Размер шрифта основных меток (Layout Group, Content Fitter, Layout Element)")]
        [SerializeField] private int _mainLabelFontSize = 11;
        [Tooltip("Размер шрифта вспомогательных меток (Padding, Spacing)")]
        [SerializeField] private int _subLabelFontSize = 12;
        
        private GUIStyle _labelStyle;
        private GUIStyle _paddingStyle;
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
            
            if (_paddingStyle == null || _stylesDirty)
            {
                _paddingStyle = new GUIStyle(GUI.skin.label)
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
            
            if (_showLayoutGroup)
            {
                var layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    DrawLayoutGroup(corners, layoutGroup);
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
        
        private void DrawLayoutGroup(Vector3[] corners, HorizontalOrVerticalLayoutGroup group)
        {
            DrawQuadBorder(corners, _layoutGroupColor, _borderThickness);
            
            if (_showPadding)
            {
                DrawPadding(corners, group);
            }
            
            if (_showSpacing && transform.childCount > 1)
            {
                DrawSpacing(group);
            }
            
            if (_showLabels)
            {
                string label = group is HorizontalLayoutGroup ? "↔ Horizontal Layout" : "↕ Vertical Layout";
                DrawLabel(corners, label, _layoutGroupColor);
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
            
            string hint = "No Layout Components\n(LayoutGroup / ContentSizeFitter / LayoutElement)";
            DrawLabel(corners, hint, hintColor);
        }
        
        private void DrawPadding(Vector3[] corners, HorizontalOrVerticalLayoutGroup group)
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
            
            Color paddingFillColor = new Color(_layoutGroupColor.r, _layoutGroupColor.g, _layoutGroupColor.b, 0.25f);
            Color paddingLineColor = new Color(_layoutGroupColor.r, _layoutGroupColor.g, _layoutGroupColor.b, 0.6f);
            
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
                    UnityEditor.Handles.Label(labelPos, $"L:{paddingLeft}", _paddingStyle);
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
                    UnityEditor.Handles.Label(labelPos, $"R:{paddingRight}", _paddingStyle);
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
                    UnityEditor.Handles.Label(labelPos, $"T:{paddingTop}", _paddingStyle);
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
                    bottomRight,
                    p2,
                    p1
                };
                
                DrawFilledQuad(bottomPaddingCorners, paddingFillColor);
                DrawQuadBorder(bottomPaddingCorners, paddingLineColor, _paddingLineThickness);
                
                if (_showLabels && paddingBottom > 15)
                {
                    Vector3 labelPos = bottomLeft + right * (width * 0.5f) + up * (paddingBottom * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"B:{paddingBottom}", _paddingStyle);
                    #endif
                }
            }
        }
        
        private void DrawSpacing(HorizontalOrVerticalLayoutGroup group)
        {
            bool isHorizontal = group is HorizontalLayoutGroup;
            float spacing = group.spacing;
            
            var children = new System.Collections.Generic.List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy)
                    children.Add(child);
            }
            
            if (children.Count < 2) return;
            
            // Рассчитываем dotSize: если -2, подбираем автоматически по длине линии
            float dotSize = _spacingZeroLineDots;
            
            for (int i = 0; i < children.Count - 1; i++)
            {
                var current = children[i];
                var next = children[i + 1];
                
                Vector3[] currCorners = new Vector3[4];
                Vector3[] nextCorners = new Vector3[4];
                current.GetWorldCorners(currCorners);
                next.GetWorldCorners(nextCorners);
                
                if (isHorizontal)
                {
                    Vector3 startBottom = currCorners[3];
                    Vector3 endBottom = nextCorners[0];
                    Vector3 startTop = currCorners[2];
                    Vector3 endTop = nextCorners[1];
                    
                    if (spacing > 0)
                    {
                        Vector3[] spacingCorners = new Vector3[4]
                        {
                            startBottom,
                            startTop,
                            endTop,
                            endBottom
                        };
                        
                        DrawFilledQuad(spacingCorners, _spacingFillColor);
                        
                        DrawDottedLine(startBottom, startTop, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(startTop, endTop, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(endTop, endBottom, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(endBottom, startBottom, _spacingFillColor, _spacingZeroLineDots);
                        
                        Vector3 mid = (startBottom + endBottom) * 0.5f;
                        if (_showLabels)
                        {
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(mid + Vector3.up * 10, $"s:{spacing:F0}", _paddingStyle);
                            #endif
                        }
                    }
                    else
                    {
                        Vector3 lineBottom = (startBottom + endBottom) * 0.5f;
                        Vector3 lineTop = (startTop + endTop) * 0.5f;
                        
                        // Автоподбор dotSize если -2
                        if (dotSize < 0)
                        {
                            float lineLength = Vector3.Distance(lineBottom, lineTop);
                            dotSize = Mathf.Max(2f, lineLength / 20f);
                        }
                        
                        #if UNITY_EDITOR
                        UnityEditor.Handles.color = _spacingZeroLineColor;
                        UnityEditor.Handles.DrawDottedLine(lineBottom, lineTop, dotSize);
                        #endif
                        
                        if (_showLabels)
                        {
                            Vector3 labelPos = (lineBottom + lineTop) * 0.5f;
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(labelPos + Vector3.up * 10, "s:0", _paddingStyle);
                            #endif
                        }
                    }
                }
                else
                {
                    Vector3 startLeft = currCorners[0];
                    Vector3 endLeft = nextCorners[1];
                    Vector3 startRight = currCorners[3];
                    Vector3 endRight = nextCorners[2];
                    
                    if (spacing > 0)
                    {
                        Vector3[] spacingCorners = new Vector3[4]
                        {
                            startLeft,
                            startRight,
                            endRight,
                            endLeft
                        };
                        
                        DrawFilledQuad(spacingCorners, _spacingFillColor);
                        
                        DrawDottedLine(startLeft, startRight, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(startRight, endRight, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(endRight, endLeft, _spacingFillColor, _spacingZeroLineDots);
                        DrawDottedLine(endLeft, startLeft, _spacingFillColor, _spacingZeroLineDots);
                        
                        Vector3 mid = (startLeft + endLeft) * 0.5f;
                        if (_showLabels)
                        {
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(mid + Vector3.right * 10, $"s:{spacing:F0}", _paddingStyle);
                            #endif
                        }
                    }
                    else
                    {
                        Vector3 lineLeft = (startLeft + endLeft) * 0.5f;
                        Vector3 lineRight = (startRight + endRight) * 0.5f;
                        
                        // Автоподбор dotSize если -2
                        if (dotSize < 0)
                        {
                            float lineLength = Vector3.Distance(lineLeft, lineRight);
                            dotSize = Mathf.Max(2f, lineLength / 20f);
                        }
                        
                        #if UNITY_EDITOR
                        UnityEditor.Handles.color = _spacingZeroLineColor;
                        UnityEditor.Handles.DrawDottedLine(lineLeft, lineRight, dotSize);
                        #endif
                        
                        if (_showLabels)
                        {
                            Vector3 labelPos = (lineLeft + lineRight) * 0.5f;
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(labelPos + Vector3.right * 10, "s:0", _paddingStyle);
                            #endif
                        }
                    }
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
        
        private void DrawDottedLine(Vector3 from, Vector3 to, Color color, float dotSize)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawDottedLine(from, to, dotSize);
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