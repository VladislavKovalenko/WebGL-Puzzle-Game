using UnityEngine;
using UnityEngine.UI;
using System;

namespace UnityEngine.UI.ProceduralImage
{
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Procedural Image")]
    [RequireComponent(typeof(FreeModifier))]
    public class ProceduralImage : Image
    {
        [Header("Procedural Settings")]
        [SerializeField] private float borderWidth;
        [SerializeField] private float falloffDistance = 1;

        [Header("Gradient Settings")]
        [SerializeField] private bool useGradient;
        [SerializeField] private bool threeColors;
        [SerializeField] private Color color1 = Color.white;
        [SerializeField] private Color color2 = Color.white;
        [SerializeField] private Color color3 = Color.white;
        [Range(0, 360)] [SerializeField] private float angle;
        [Range(0.01f, 0.99f)] [SerializeField] private float middlePoint = 0.5f;

        private Material _runtimeMaterial;
        private static Material _sharedDefaultMaterial;

        // IDs свойств для оптимизации
        private static readonly int PropUseGradient = Shader.PropertyToID("_UseGradient");
        private static readonly int PropThreeColors = Shader.PropertyToID("_ThreeColors");
        private static readonly int PropColor1 = Shader.PropertyToID("_GradientColor1");
        private static readonly int PropColor2 = Shader.PropertyToID("_GradientColor2");
        private static readonly int PropColor3 = Shader.PropertyToID("_GradientColor3");
        private static readonly int PropAngle = Shader.PropertyToID("_GradientAngle");
        private static readonly int PropMiddle = Shader.PropertyToID("_MiddlePoint");

        private static Material DefaultProceduralMaterial
        {
            get
            {
                if (_sharedDefaultMaterial == null)
                {
                    // Шейдер должен называться именно так в самом .shader файле
                    var shader = Shader.Find("UI/Procedural UI Image Gradient");
                    if (shader != null)
                    {
                        _sharedDefaultMaterial = new Material(shader);
                    }
                }
                return _sharedDefaultMaterial;
            }
        }

        #region UI.Image Overrides

        /// <summary>
        /// Мы переопределяем этот геттер, чтобы Unity UI всегда брал наш инстанс материала.
        /// Это исправляет проблему исчезновения в Play Mode и позволяет иметь разные градиенты.
        /// </summary>
        public override Material materialForRendering
        {
            get
            {
                if (_runtimeMaterial == null)
                {
                    if (DefaultProceduralMaterial == null) return base.materialForRendering;
                    
                    _runtimeMaterial = new Material(DefaultProceduralMaterial);
                    _runtimeMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable;
                }

                // Шейдер рисует форму (скругление) ВСЕГДА, 
                // но градиент накладывает только если этот флаг = 1
                _runtimeMaterial.SetFloat(PropUseGradient, useGradient ? 1f : 0f);
                
                if (useGradient)
                {
                    _runtimeMaterial.SetFloat(PropThreeColors, threeColors ? 1f : 0f);
                    _runtimeMaterial.SetColor(PropColor1, color1);
                    _runtimeMaterial.SetColor(PropColor2, color2);
                    _runtimeMaterial.SetColor(PropColor3, color3);
                    _runtimeMaterial.SetFloat(PropAngle, angle);
                    _runtimeMaterial.SetFloat(PropMiddle, middlePoint);
                }

                return _runtimeMaterial;
            }
        }

        /// <summary>
        /// Здесь упаковываются данные о радиусах углов и границах в вершины (UV1-UV3).
        /// Это срабатывает при вызове SetVerticesDirty() из модификаторов.
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            var r = GetPixelAdjustedRect();
            
            float pixelSize = 1f / Mathf.Max(0.01f, falloffDistance);
            Vector4 radius = FixRadius(GetRadius(r));
            float minside = Mathf.Min(r.width, r.height);

            UIVertex vert = new UIVertex();
            // UV1: Размеры области
            Vector2 uv1 = new Vector2(r.width + falloffDistance, r.height + falloffDistance);
            // UV2: Упакованные радиусы углов
            Vector2 uv2 = new Vector2(EncodeFloats(radius.x / minside, radius.y / minside), 
                                     EncodeFloats(radius.z / minside, radius.w / minside));
            // UV3: Толщина рамки и мягкость края
            Vector2 uv3 = new Vector2(borderWidth == 0 ? 1 : Mathf.Clamp01(borderWidth / minside * 2), pixelSize);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                // Смещение позиции для поддержки сглаживания (Antialiasing)
                vert.position += ((Vector3)vert.uv0 - new Vector3(0.5f, 0.5f)) * falloffDistance;
                vert.uv1 = uv1;
                vert.uv2 = uv2;
                vert.uv3 = uv3;
                vh.SetUIVertex(vert, i);
            }
        }

        #endregion

        #region Internal Logic

        public void UpdateMaterial()
        {
            // Уведомляем систему рендеринга, что материал нужно обновить
            SetMaterialDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            FixTexCoordsInCanvas();
            if (sprite == null) sprite = EmptySprite.Get();
            UpdateMaterial();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Очистка памяти при выключении объекта
            if (_runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(_runtimeMaterial);
                else DestroyImmediate(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        private Vector4 GetRadius(Rect imageRect)
        {
            var freeMod = GetComponent<FreeModifier>();
            return freeMod != null ? freeMod.CalculateRadius(imageRect) : Vector4.zero;
        }

        private Vector4 FixRadius(Vector4 vec)
        {
            Rect r = rectTransform.rect;
            vec = new Vector4(Mathf.Max(vec.x, 0), Mathf.Max(vec.y, 0), Mathf.Max(vec.z, 0), Mathf.Max(vec.w, 0));
            float scaleFactor = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(r.width / (vec.x + vec.y), r.width / (vec.z + vec.w)), r.height / (vec.x + vec.w)), r.height / (vec.z + vec.y)), 1f);
            return vec * scaleFactor;
        }

        private float EncodeFloats(float a, float b)
        {
            Vector2 kDecodeDot = new Vector2(1.0f, 1f / 65535.0f);
            return Vector2.Dot(new Vector2(Mathf.Floor(a * 65534) / 65535f, Mathf.Floor(b * 65534) / 65535f), kDecodeDot);
        }

        private void FixTexCoordsInCanvas()
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null)
                c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | 
                                             AdditionalCanvasShaderChannels.TexCoord2 | 
                                             AdditionalCanvasShaderChannels.TexCoord3;
        }

        #endregion

        #region Public API
        public float BorderWidth 
        { 
            get => borderWidth; 
            set { borderWidth = value; SetVerticesDirty(); } 
        }
        
        public bool UseGradient 
        { 
            get => useGradient; 
            set { useGradient = value; UpdateMaterial(); } 
        }
        #endregion

#if UNITY_EDITOR
        // Обновление в редакторе без нажатия Play
        public void Update() 
        { 
            if (!Application.isPlaying) UpdateGeometry(); 
        }

        protected override void OnValidate() 
        { 
            base.OnValidate(); 
            UpdateMaterial(); 
            SetVerticesDirty(); 
        }
#endif
    }
}