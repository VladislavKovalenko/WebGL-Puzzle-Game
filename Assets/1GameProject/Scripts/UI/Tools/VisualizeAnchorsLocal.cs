using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Локальный визуализатор якорей — показывает только сам объект и его прямых детей (1 уровень).
    /// Понятная визуализация для новичков: рамка объекта, якоря, pivot и связи.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class VisualizeAnchorsLocal : MonoBehaviour
    {
        [Header("Что показывать")]
        [SerializeField] private bool _showSelf = true;
        [SerializeField] private bool _showChildren = true;
        [SerializeField] private bool _showObjectBounds = true;
        [SerializeField] private bool _showAnchors = true;
        [SerializeField] private bool _showPivot = true;
        [SerializeField] private bool _showAnchorLines = true;
        [SerializeField] private bool _showLabels = true;
        [SerializeField] private bool _includeInactive = false;
        
        [Header("Цвета объектов")]
        [SerializeField] private Color _selfBoundsColor = new Color(0f, 0.5f, 1f, 0.6f);
        [SerializeField] private Color _childBoundsColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        
        [Header("Цвета якорей родителя (THIS)")]
        [SerializeField] private Color _selfAnchorPointColor = new Color(1f, 0.9f, 0f, 1f); // Жёлтый
        [SerializeField] private Color _selfAnchorAreaFillColor = new Color(1f, 0.9f, 0f, 0.12f); // Жёлтая заливка
        [SerializeField] private Color _selfAnchorAreaBorderColor = new Color(1f, 0.9f, 0f, 0.5f); // Жёлтая обводка
        
        [Header("Цвета якорей детей")]
        [SerializeField] private Color _childAnchorPointColor = new Color(0f, 1f, 0.2f, 1f); // Зелёный
        [SerializeField] private Color _childAnchorAreaFillColor = new Color(0f, 1f, 0.2f, 0.12f); // Зелёная заливка
        [SerializeField] private Color _childAnchorAreaBorderColor = new Color(0f, 1f, 0.2f, 0.5f); // Зелёная обводка
        
        [Header("Цвета Pivot")]
        [SerializeField] private Color _pivotColor = new Color(1f, 0.3f, 0f, 1f);
        [SerializeField] private Color _pivotCrossColor = new Color(1f, 1f, 1f, 0.9f);
        
        [Header("Цвета связей")]
        [SerializeField] private Color _anchorToPivotLineColor = new Color(1f, 0.8f, 0f, 0.6f);
        
        [Header("Цвета меток")]
        [SerializeField] private Color _labelBgColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Color _labelTextColor = Color.white;
        [SerializeField] private Color _selfAnchorLabelColor = new Color(1f, 0.9f, 0.3f, 1f); // Жёлтый для родителя
        [SerializeField] private Color _childAnchorLabelColor = new Color(0.3f, 1f, 0.3f, 1f); // Зелёный для детей
        [SerializeField] private Color _pivotLabelColor = new Color(1f, 0.6f, 0.2f, 1f);
        
        [Header("Размеры")]
        [SerializeField] private float _boundsThickness = 2.5f;
        [SerializeField] private float _anchorPointSize = 10f;
        [SerializeField] private float _anchorBorderThickness = 2f;
        [SerializeField] private float _pivotSize = 8f;
        [SerializeField] private float _pivotCrossSize = 12f;
        [SerializeField] private float _lineThickness = 2f;
        
        [Header("Шрифты")]
        [SerializeField] private int _objectNameFontSize = 12;
        [SerializeField] private int _anchorLabelFontSize = 10;
        [SerializeField] private int _infoLabelFontSize = 9;
        
        private GUIStyle _objectNameStyle;
        private GUIStyle _selfAnchorLabelStyle;
        private GUIStyle _childAnchorLabelStyle;
        private GUIStyle _infoLabelStyle;
        private bool _stylesDirty = true;
        
        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (!enabled || !gameObject.activeInHierarchy) return;
            
            InitializeStyles();
            DrawLocalAnchors();
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
            if (_objectNameStyle == null || _stylesDirty)
            {
                _objectNameStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = _objectNameFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = 
                    { 
                        textColor = _labelTextColor,
                        background = MakeTex(2, 2, _labelBgColor)
                    },
                    padding = new RectOffset(6, 6, 3, 3)
                };
            }
            
            if (_selfAnchorLabelStyle == null || _stylesDirty)
            {
                _selfAnchorLabelStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = _anchorLabelFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = 
                    { 
                        textColor = _selfAnchorLabelColor,
                        background = MakeTex(2, 2, _labelBgColor)
                    },
                    padding = new RectOffset(5, 5, 2, 2)
                };
            }
            
            if (_childAnchorLabelStyle == null || _stylesDirty)
            {
                _childAnchorLabelStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = _anchorLabelFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = 
                    { 
                        textColor = _childAnchorLabelColor,
                        background = MakeTex(2, 2, _labelBgColor)
                    },
                    padding = new RectOffset(5, 5, 2, 2)
                };
            }
            
            if (_infoLabelStyle == null || _stylesDirty)
            {
                _infoLabelStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = _infoLabelFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    normal = 
                    { 
                        textColor = _labelTextColor,
                        background = MakeTex(2, 2, _labelBgColor)
                    },
                    padding = new RectOffset(4, 4, 2, 2)
                };
            }
            
            _stylesDirty = false;
        }
        
        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private void DrawLocalAnchors()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            // Рисуем сам объект
            if (_showSelf)
            {
                DrawRectTransformAnchors(rectTransform, true);
            }
            
            // Рисуем только прямых детей
            if (_showChildren)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    
                    if (!_includeInactive && !child.gameObject.activeInHierarchy)
                        continue;
                    
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        DrawRectTransformAnchors(childRect, false);
                    }
                }
            }
        }
        
        private void DrawRectTransformAnchors(RectTransform rectTransform, bool isSelf)
        {
            #if UNITY_EDITOR
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null) return;
            
            // Выбираем цвета в зависимости от того, это родитель или ребёнок
            Color anchorPointColor = isSelf ? _selfAnchorPointColor : _childAnchorPointColor;
            Color anchorAreaFillColor = isSelf ? _selfAnchorAreaFillColor : _childAnchorAreaFillColor;
            Color anchorAreaBorderColor = isSelf ? _selfAnchorAreaBorderColor : _childAnchorAreaBorderColor;
            GUIStyle anchorLabelStyle = isSelf ? _selfAnchorLabelStyle : _childAnchorLabelStyle;
            
            // === 1. РИСУЕМ ГРАНИЦЫ САМОГО ОБЪЕКТА ===
            if (_showObjectBounds)
            {
                Vector3[] objectCorners = new Vector3[4];
                rectTransform.GetWorldCorners(objectCorners);
                
                Color boundsColor = isSelf ? _selfBoundsColor : _childBoundsColor;
                
                // Рамка объекта
                UnityEditor.Handles.color = boundsColor;
                UnityEditor.Handles.DrawLine(objectCorners[0], objectCorners[1], _boundsThickness);
                UnityEditor.Handles.DrawLine(objectCorners[1], objectCorners[2], _boundsThickness);
                UnityEditor.Handles.DrawLine(objectCorners[2], objectCorners[3], _boundsThickness);
                UnityEditor.Handles.DrawLine(objectCorners[3], objectCorners[0], _boundsThickness);
                
                // Имя объекта в углу
                if (_showLabels)
                {
                    Vector3 labelPos = objectCorners[1]; // Верхний левый угол
                    string objectLabel = isSelf ? $"[THIS] {rectTransform.name}" : rectTransform.name;
                    UnityEditor.Handles.Label(labelPos + Vector3.up * 5, objectLabel, _objectNameStyle);
                }
            }
            
            // Получаем углы родительского объекта
            Vector3[] parentCorners = new Vector3[4];
            parentRect.GetWorldCorners(parentCorners);
            
            Vector3 parentBottomLeft = parentCorners[0];
            float parentWidth = Vector3.Distance(parentCorners[0], parentCorners[3]);
            float parentHeight = Vector3.Distance(parentCorners[0], parentCorners[1]);
            
            Vector3 parentRight = (parentCorners[3] - parentCorners[0]).normalized;
            Vector3 parentUp = (parentCorners[1] - parentCorners[0]).normalized;
            
            // Вычисляем позиции якорей
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            
            Vector3 anchorMinWorld = parentBottomLeft + 
                                    parentRight * (anchorMin.x * parentWidth) + 
                                    parentUp * (anchorMin.y * parentHeight);
            
            Vector3 anchorMaxWorld = parentBottomLeft + 
                                    parentRight * (anchorMax.x * parentWidth) + 
                                    parentUp * (anchorMax.y * parentHeight);
            
            bool singleAnchor = Vector2.Distance(anchorMin, anchorMax) < 0.01f;
            
            // === 2. РИСУЕМ ОБЛАСТЬ ЯКОРЕЙ (если растянуты) ===
            if (_showAnchors && !singleAnchor)
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
                
                // Заливка области (жёлтая для родителя, зелёная для детей)
                UnityEditor.Handles.color = anchorAreaFillColor;
                UnityEditor.Handles.DrawAAConvexPolygon(anchorAreaCorners);
                
                // Пунктирная обводка области
                UnityEditor.Handles.color = anchorAreaBorderColor;
                DrawDashedLine(anchorBottomLeft, anchorTopLeft, _anchorBorderThickness);
                DrawDashedLine(anchorTopLeft, anchorTopRight, _anchorBorderThickness);
                DrawDashedLine(anchorTopRight, anchorBottomRight, _anchorBorderThickness);
                DrawDashedLine(anchorBottomRight, anchorBottomLeft, _anchorBorderThickness);
                
                // Метка в центре области якорей
                if (_showLabels)
                {
                    Vector3 areaCenter = (anchorBottomLeft + anchorTopRight) * 0.5f;
                    UnityEditor.Handles.Label(areaCenter, "⚓ ANCHOR AREA", _infoLabelStyle);
                }
            }
            
            // === 3. РИСУЕМ ТОЧКИ ЯКОРЕЙ ===
            if (_showAnchors)
            {
                if (singleAnchor)
                {
                    // Один якорь - рисуем ромб
                    DrawDiamond(anchorMinWorld, _anchorPointSize, anchorPointColor);
                    
                    if (_showLabels)
                    {
                        string anchorLabel = $"⚓ ({anchorMin.x:F2}, {anchorMin.y:F2})";
                        UnityEditor.Handles.Label(anchorMinWorld + Vector3.up * 18, anchorLabel, anchorLabelStyle);
                    }
                }
                else
                {
                    // Два якоря - рисуем круги
                    DrawCircleWithBorder(anchorMinWorld, _anchorPointSize, anchorPointColor, Color.white);
                    DrawCircleWithBorder(anchorMaxWorld, _anchorPointSize, anchorPointColor, Color.white);
                    
                    if (_showLabels)
                    {
                        string minLabel = $"⚓ MIN\n({anchorMin.x:F2}, {anchorMin.y:F2})";
                        string maxLabel = $"⚓ MAX\n({anchorMax.x:F2}, {anchorMax.y:F2})";
                        
                        UnityEditor.Handles.Label(anchorMinWorld + Vector3.down * 25, minLabel, anchorLabelStyle);
                        UnityEditor.Handles.Label(anchorMaxWorld + Vector3.up * 18, maxLabel, anchorLabelStyle);
                    }
                }
            }
            
            // === 4. РИСУЕМ ЛИНИИ ОТ ЯКОРЕЙ К PIVOT ===
            Vector3 pivotWorld = rectTransform.position;
            
            if (_showAnchorLines && _showAnchors && _showPivot)
            {
                UnityEditor.Handles.color = _anchorToPivotLineColor;
                
                if (singleAnchor)
                {
                    DrawArrowLine(anchorMinWorld, pivotWorld, _lineThickness);
                }
                else
                {
                    DrawArrowLine(anchorMinWorld, pivotWorld, _lineThickness);
                    DrawArrowLine(anchorMaxWorld, pivotWorld, _lineThickness);
                }
            }
            
            // === 5. РИСУЕМ PIVOT ===
            if (_showPivot)
            {
                // Круг pivot
                DrawCircleWithBorder(pivotWorld, _pivotSize, _pivotColor, Color.white);
                
                // Крестик в центре
                UnityEditor.Handles.color = _pivotCrossColor;
                UnityEditor.Handles.DrawLine(
                    pivotWorld + Vector3.left * _pivotCrossSize, 
                    pivotWorld + Vector3.right * _pivotCrossSize, 
                    3f);
                UnityEditor.Handles.DrawLine(
                    pivotWorld + Vector3.down * _pivotCrossSize, 
                    pivotWorld + Vector3.up * _pivotCrossSize, 
                    3f);
                
                if (_showLabels)
                {
                    Vector2 pivot = rectTransform.pivot;
                    
                    // Определяем лучшую позицию для метки (чтобы не перекрывалась с якорями)
                    Vector3 labelOffset = Vector3.right * 20;
                    if (!singleAnchor && Vector3.Distance(pivotWorld, anchorMaxWorld) < 50)
                    {
                        labelOffset = Vector3.left * 80;
                    }
                    
                    GUIStyle pivotStyle = new GUIStyle(anchorLabelStyle);
                    pivotStyle.normal.textColor = _pivotLabelColor;
                    
                    string pivotLabel = $"◉ PIVOT\n({pivot.x:F2}, {pivot.y:F2})";
                    UnityEditor.Handles.Label(pivotWorld + labelOffset, pivotLabel, pivotStyle);
                }
            }
            #endif
        }
        
        private void DrawCircleWithBorder(Vector3 center, float size, Color fillColor, Color borderColor)
        {
            #if UNITY_EDITOR
            // Заливка
            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.forward, size);
            
            // Обводка
            UnityEditor.Handles.color = borderColor;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, size, 3f);
            #endif
        }
        
        private void DrawDiamond(Vector3 center, float size, Color color)
        {
            #if UNITY_EDITOR
            Vector3 top = center + Vector3.up * size;
            Vector3 bottom = center + Vector3.down * size;
            Vector3 left = center + Vector3.left * size;
            Vector3 right = center + Vector3.right * size;
            
            Vector3[] diamondCorners = new Vector3[4] { bottom, left, top, right };
            
            // Заливка
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAConvexPolygon(diamondCorners);
            
            // Обводка
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawLine(bottom, left, 3f);
            UnityEditor.Handles.DrawLine(left, top, 3f);
            UnityEditor.Handles.DrawLine(top, right, 3f);
            UnityEditor.Handles.DrawLine(right, bottom, 3f);
            #endif
        }
        
        private void DrawDashedLine(Vector3 from, Vector3 to, float thickness)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.DrawDottedLine(from, to, 5f);
            #endif
        }
        
        private void DrawArrowLine(Vector3 from, Vector3 to, float thickness)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.DrawDottedLine(from, to, 4f);
            
            // Стрелка на конце
            Vector3 direction = (to - from).normalized;
            Vector3 arrowBase = to - direction * 8f;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * 4f;
            
            UnityEditor.Handles.DrawLine(to, arrowBase + perpendicular, 2f);
            UnityEditor.Handles.DrawLine(to, arrowBase - perpendicular, 2f);
            #endif
        }
    }
}