using UnityEngine;
using UnityEngine.UI.ProceduralImage;

namespace UnityEngine.UI.ProceduralImage
{
    // REMOVED: [ModifierID("Free")]
    public class FreeModifier : ProceduralImageModifier
    {
        [SerializeField] private Vector4 radius;
        [SerializeField] private bool uniformRadius;
        [SerializeField] private float uniformValue;

        public Vector4 Radius
        {
            get { return radius; }
            set { radius = value; _Graphic.SetVerticesDirty(); }
        }

        public bool UniformRadius
        {
            get { return uniformRadius; }
            set 
            { 
                if (uniformRadius != value)
                {
                    uniformRadius = value; 
                    if (uniformRadius)
                    {
                        uniformValue = radius.x;
                        radius = new Vector4(uniformValue, uniformValue, uniformValue, uniformValue);
                    }
                    _Graphic.SetVerticesDirty();
                }
            }
        }

        public float UniformValue
        {
            get { return uniformValue; }
            set 
            { 
                uniformValue = Mathf.Max(0, value);
                if (uniformRadius)
                {
                    radius = new Vector4(uniformValue, uniformValue, uniformValue, uniformValue);
                    _Graphic.SetVerticesDirty();
                }
            }
        }

        public override Vector4 CalculateRadius(Rect imageRect) => radius;

        protected void OnValidate()
        {
            radius.x = Mathf.Max(0, radius.x);
            radius.y = Mathf.Max(0, radius.y);
            radius.z = Mathf.Max(0, radius.z);
            radius.w = Mathf.Max(0, radius.w);
            uniformValue = Mathf.Max(0, uniformValue);
            
            if (uniformRadius)
            {
                radius = new Vector4(uniformValue, uniformValue, uniformValue, uniformValue);
            }
        }
    }
}