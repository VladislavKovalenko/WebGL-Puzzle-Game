using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Визуализирует якоря (anchors) всех дочерних RectTransform элементов.
    /// Показывает anchor min/max, pivot и позицию.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VisualizeAnchors : MonoBehaviour
    {
        [Header("Что показывать")]
        [SerializeField] private bool _showAnchors = true;
        [SerializeField] private bool _showPivot = true;
        [SerializeField] private bool _showAnchorLines = true;
        [SerializeField] private bool _showLabels = true;
        [SerializeField] private bool _includeInactive = false;
        
        [Header("Цвета")]
        [SerializeField] private Color _anchorColor = new Color(0f, 1f, 0f, 0.8f);
        [SerializeField] private Color _anchorAreaColor = new Color(0f, 1f, 0f, 0.15f);
        [SerializeField] private Color _pivotColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private Color _anchorLineColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color _labelColor = Color.white;
        
        [Header("Размеры")]
        [SerializeField] private float _anchorSize = 8f;
        [SerializeField] private float _pivotSize = 6f;
        [SerializeField] private float _lineThickness = 1.5f;
        [SerializeField] private int _labelFontSize = 10;
        
        [Header("Фильтр глубины")]
        [Tooltip("0 = все дочерние элементы, 1 = только прямые дети, 2 = дети + внуки и т.д.")]
        [SerializeField] private int _maxDepth = 0;
        
        private GUIStyle _labelStyle;
        private bool _stylesDirty = true;
        
        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (!enabled || !gameObject.activeInHierarchy) return;
            
            InitializeStyles();
            DrawChildAnchors(transform, 0);
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
                    fontSize = _labelFontSize,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = _labelColor }
                };
                
                _stylesDirty = false;
            }
        }
        
        private void DrawChildAnchors(Transform parent, int currentDepth)
        {
            if (_maxDepth > 0 && currentDepth >= _maxDepth) return;
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                
                if (!_includeInactive && !child.gameObject.activeInHierarchy)
                    continue;
                
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    DrawRectTransformAnchors(rectTransform);
                }
                
                // Рекурсивно обрабатываем детей
                DrawChildAnchors(child, currentDepth + 1);
            }
        }
        
        private void DrawRectTransformAnchors(RectTransform rectTransform)
        {
            #if UNITY_EDITOR
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null) return;
            
            // Получаем углы родительского объекта в мировых координатах
            Vector3[] parentCorners = new Vector3[4];
            parentRect.GetWorldCorners(parentCorners);
            
            Vector3 parentBottomLeft = parentCorners[0];
            Vector3 parentTopRight = parentCorners[2];
            
            // Вычисляем размеры родителя
            float parentWidth = Vector3.Distance(parentCorners[0], parentCorners[3]);
            float parentHeight = Vector3.Distance(parentCorners[0], parentCorners[1]);
            
            Vector3 parentRight = (parentCorners[3] - parentCorners[0]).normalized;
            Vector3 parentUp = (parentCorners[1] - parentCorners[0]).normalized;
            
            // Вычисляем позиции якорей в мировых координатах
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            
            Vector3 anchorMinWorld = parentBottomLeft + 
                                    parentRight * (anchorMin.x * parentWidth) + 
                                    parentUp * (anchorMin.y * parentHeight);
            
            Vector3 anchorMaxWorld = parentBottomLeft + 
                                    parentRight * (anchorMax.x * parentWidth) + 
                                    parentUp * (anchorMax.y * parentHeight);
            
            // Рисуем область между якорями (если они не совпадают)
            if (_showAnchors && Vector2.Distance(anchorMin, anchorMax) > 0.01f)
            {
                Vector3 anchorBottomLeft = parentBottomLeft + 
                                          parentRight * (anchorMin.x * parentWidth) + 
                                          parentUp * (anchorMin.y * parentHeight);
                
                Vector3 anchorBottomRight = parentBottomLeft + 
                                           parentRight * (anchorMax.x * parentWidth) + 
                                           parentUp * (anchorMin.y * parentHeight);
                
                Vector3 anchorTopLeft = parentBottomLeft + 
                                       parentRight * (anchorMin.x * parentWidth) + 
                                       parentUp * (anchorMax.y * parentHeight);
                
                Vector3 anchorTopRight = parentBottomLeft + 
                                        parentRight * (anchorMax.x * parentWidth) + 
                                        parentUp * (anchorMax.y * parentHeight);
                
                Vector3[] anchorAreaCorners = new Vector3[4]
                {
                    anchorBottomLeft,
                    anchorTopLeft,
                    anchorTopRight,
                    anchorBottomRight
                };
                
                // Заливка области якорей
                UnityEditor.Handles.color = _anchorAreaColor;
                UnityEditor.Handles.DrawAAConvexPolygon(anchorAreaCorners);
                
                // Обводка области якорей
                UnityEditor.Handles.color = _anchorColor;
                UnityEditor.Handles.DrawLine(anchorBottomLeft, anchorTopLeft, _lineThickness);
                UnityEditor.Handles.DrawLine(anchorTopLeft, anchorTopRight, _lineThickness);
                UnityEditor.Handles.DrawLine(anchorTopRight, anchorBottomRight, _lineThickness);
                UnityEditor.Handles.DrawLine(anchorBottomRight, anchorBottomLeft, _lineThickness);
            }
            
            // Рисуем точки якорей
            if (_showAnchors)
            {
                UnityEditor.Handles.color = _anchorColor;
                UnityEditor.Handles.DrawSolidDisc(anchorMinWorld, Vector3.forward, _anchorSize);
                UnityEditor.Handles.DrawSolidDisc(anchorMaxWorld, Vector3.forward, _anchorSize);
                
                // Метки якорей
                if (_showLabels)
                {
                    string anchorMinLabel = $"Min ({anchorMin.x:F2}, {anchorMin.y:F2})";
                    string anchorMaxLabel = $"Max ({anchorMax.x:F2}, {anchorMax.y:F2})";
                    
                    UnityEditor.Handles.Label(anchorMinWorld + Vector3.left * 15, anchorMinLabel, _labelStyle);
                    UnityEditor.Handles.Label(anchorMaxWorld + Vector3.right * 15, anchorMaxLabel, _labelStyle);
                }
            }
            
            // Рисуем Pivot
            if (_showPivot)
            {
                Vector3 pivotWorld = rectTransform.position;
                
                UnityEditor.Handles.color = _pivotColor;
                UnityEditor.Handles.DrawSolidDisc(pivotWorld, Vector3.forward, _pivotSize);
                
                // Крестик в центре pivot
                UnityEditor.Handles.DrawLine(
                    pivotWorld + Vector3.left * _pivotSize, 
                    pivotWorld + Vector3.right * _pivotSize, 
                    2f);
                UnityEditor.Handles.DrawLine(
                    pivotWorld + Vector3.down * _pivotSize, 
                    pivotWorld + Vector3.up * _pivotSize, 
                    2f);
                
                if (_showLabels)
                {
                    Vector2 pivot = rectTransform.pivot;
                    string pivotLabel = $"Pivot ({pivot.x:F2}, {pivot.y:F2})";
                    UnityEditor.Handles.Label(pivotWorld + Vector3.down * 15, pivotLabel, _labelStyle);
                }
            }
            
            // Рисуем линии от якорей к pivot
            if (_showAnchorLines)
            {
                Vector3 pivotWorld = rectTransform.position;
                
                UnityEditor.Handles.color = _anchorLineColor;
                UnityEditor.Handles.DrawDottedLine(anchorMinWorld, pivotWorld, 4f);
                UnityEditor.Handles.DrawDottedLine(anchorMaxWorld, pivotWorld, 4f);
            }
            #endif
        }
    }
}