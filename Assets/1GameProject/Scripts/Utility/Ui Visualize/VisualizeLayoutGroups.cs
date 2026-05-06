using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Визуализатор Layout Groups в Unity UI — рисует границы и метки для Horizontal/Vertical Layout Group
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VisualizeLayoutGroups : MonoBehaviour
    {
        [Header("Визуализация")]
        [SerializeField] private bool _showLayoutGroups = true;
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
        
        private static GUIStyle _labelStyle;
        private static GUIStyle _paddingStyle;
        
        // === КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: проверяем enabled ===
        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            // OnDrawGizmos вызывается ВСЕГДА, даже если скрипт disabled!
            // Поэтому явно проверяем enabled и activeInHierarchy
            if (!enabled || !gameObject.activeInHierarchy) return;
            
            InitializeStyles();
            DrawLayoutVisualizers(transform);
            #endif
        }
        
        // === ДОПОЛНИТЕЛЬНО: принудительно перерисовываем при выключении ===
        private void OnDisable()
        {
            #if UNITY_EDITOR
            // Принудительно перерисовываем сцену, чтобы убрать остатки gizmos
            UnityEditor.SceneView.RepaintAll();
            #endif
        }
        
        private void InitializeStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = _labelColor }
                };
            }
            
            if (_paddingStyle == null)
            {
                _paddingStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 9,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
                };
            }
        }
        
        private void DrawLayoutVisualizers(Transform target)
        {
            if (target == null) return;
            
            foreach (Transform child in target)
            {
                DrawComponentVisualizers(child);
                DrawLayoutVisualizers(child);
            }
        }
        
        private void DrawComponentVisualizers(Transform target)
        {
            if (target == null) return;
            
            var rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            if (_showLayoutGroups)
            {
                var layoutGroup = target.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    DrawLayoutGroup(corners, layoutGroup, target.name);
                }
            }
            
            if (_showContentSizeFitter)
            {
                var fitter = target.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    DrawContentSizeFitter(corners, fitter, target.name);
                }
            }
            
            if (_showLayoutElement)
            {
                var layoutElement = target.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    DrawLayoutElement(corners, layoutElement, target.name);
                }
            }
        }
        
        private void DrawLayoutGroup(Vector3[] corners, HorizontalOrVerticalLayoutGroup group, string objectName)
        {
            DrawFilledQuad(corners, _layoutGroupColor);
            DrawQuadBorder(corners, _borderColor, _borderThickness);
            
            if (_showPadding)
            {
                DrawPadding(corners, group);
            }
            
            if (_showSpacing && group.transform.childCount > 1)
            {
                DrawSpacing(group);
            }
            
            if (_showLabels)
            {
                string label = group is HorizontalLayoutGroup ? "↔ Horizontal" : "↕ Vertical";
                DrawLabel(corners, label, _layoutGroupColor);
            }
        }
        
        private void DrawContentSizeFitter(Vector3[] corners, ContentSizeFitter fitter, string objectName)
        {
            DrawFilledQuad(corners, _contentFitterColor);
            DrawQuadBorder(corners, _borderColor, _borderThickness);
            
            if (_showLabels)
            {
                string label = $"Content Fitter\nH: {fitter.horizontalFit}\nV: {fitter.verticalFit}";
                DrawLabel(corners, label, _contentFitterColor);
            }
        }
        
        private void DrawLayoutElement(Vector3[] corners, LayoutElement element, string objectName)
        {
            DrawFilledQuad(corners, _layoutElementColor);
            DrawQuadBorder(corners, _borderColor, _borderThickness);
            
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
            
            Color paddingColor = new Color(_layoutGroupColor.r, _layoutGroupColor.g, _layoutGroupColor.b, 0.2f);
            
            if (paddingLeft > 0)
            {
                Vector3 p1 = bottomLeft + right * paddingLeft;
                Vector3 p2 = topLeft + right * paddingLeft;
                DrawDashedLine(bottomLeft, p1, paddingColor);
                DrawDashedLine(topLeft, p2, paddingColor);
                DrawDashedLine(p1, p2, paddingColor);
                
                if (_showLabels && paddingLeft > 20)
                {
                    Vector3 labelPos = bottomLeft + right * (paddingLeft * 0.5f) + up * (height * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"L:{paddingLeft}", _paddingStyle);
                    #endif
                }
            }
            
            if (paddingRight > 0)
            {
                Vector3 p1 = bottomRight - right * paddingRight;
                Vector3 p2 = topRight - right * paddingRight;
                DrawDashedLine(bottomRight, p1, paddingColor);
                DrawDashedLine(topRight, p2, paddingColor);
                DrawDashedLine(p1, p2, paddingColor);
                
                if (_showLabels && paddingRight > 20)
                {
                    Vector3 labelPos = bottomRight - right * (paddingRight * 0.5f) + up * (height * 0.5f);
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"R:{paddingRight}", _paddingStyle);
                    #endif
                }
            }
            
            if (paddingTop > 0)
            {
                Vector3 p1 = topLeft + up * -paddingTop;
                Vector3 p2 = topRight + up * -paddingTop;
                DrawDashedLine(topLeft, p1, paddingColor);
                DrawDashedLine(topRight, p2, paddingColor);
                DrawDashedLine(p1, p2, paddingColor);
            }
            
            if (paddingBottom > 0)
            {
                Vector3 p1 = bottomLeft + up * paddingBottom;
                Vector3 p2 = bottomRight + up * paddingBottom;
                DrawDashedLine(bottomLeft, p1, paddingColor);
                DrawDashedLine(bottomRight, p2, paddingColor);
                DrawDashedLine(p1, p2, paddingColor);
            }
        }
        
        private void DrawSpacing(HorizontalOrVerticalLayoutGroup group)
        {
            bool isHorizontal = group is HorizontalLayoutGroup;
            float spacing = group.spacing;
            
            if (spacing <= 0) return;
            
            var children = new System.Collections.Generic.List<RectTransform>();
            for (int i = 0; i < group.transform.childCount; i++)
            {
                var child = group.transform.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy)
                    children.Add(child);
            }
            
            for (int i = 0; i < children.Count - 1; i++)
            {
                var current = children[i];
                var next = children[i + 1];
                
                Vector3[] currCorners = new Vector3[4];
                Vector3[] nextCorners = new Vector3[4];
                current.GetWorldCorners(currCorners);
                next.GetWorldCorners(nextCorners);
                
                Color spacingColor = new Color(1f, 1f, 0f, 0.6f);
                
                if (isHorizontal)
                {
                    Vector3 start = currCorners[3];
                    Vector3 end = nextCorners[0];
                    DrawDashedLine(start, end, spacingColor);
                    
                    Vector3 mid = (start + end) * 0.5f;
                    if (_showLabels)
                    {
                        #if UNITY_EDITOR
                        UnityEditor.Handles.Label(mid + Vector3.up * 10, $"s:{spacing:F0}", _paddingStyle);
                        #endif
                    }
                }
                else
                {
                    Vector3 start = currCorners[0];
                    Vector3 end = nextCorners[1];
                    DrawDashedLine(start, end, spacingColor);
                    
                    Vector3 mid = (start + end) * 0.5f;
                    if (_showLabels)
                    {
                        #if UNITY_EDITOR
                        UnityEditor.Handles.Label(mid + Vector3.right * 10, $"s:{spacing:F0}", _paddingStyle);
                        #endif
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
        
        private void DrawDashedLine(Vector3 from, Vector3 to, Color color)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawDottedLine(from, to, 4f);
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