using UnityEngine;

namespace UnityEngine.UI.ProceduralImage
{
    public struct ProceduralImageInfo
    {
        public float width;
        public float height;
        public float fallOffDistance;
        public Vector4 radius;
        public float borderWidth;
        public float pixelSize;

        public ProceduralImageInfo(float width, float height, float fallOffDistance, float pixelSize, Vector4 radius, float borderWidth)
        {
            this.width = Mathf.Abs(width);
            this.height = Mathf.Abs(height);
            this.fallOffDistance = Mathf.Max(0, fallOffDistance);
            this.radius = radius;
            this.borderWidth = Mathf.Max(borderWidth, 0);
            this.pixelSize = Mathf.Max(0, pixelSize);
        }
    }
}