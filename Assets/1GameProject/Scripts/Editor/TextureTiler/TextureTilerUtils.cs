using UnityEngine;

namespace TextureTiler
{
    public static class Utils
    {
        // Hash for Quilez techniques
        public static float Hash2(float x, float y)
        {
            float n = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        public static float[] Hash4(float x, float y)
        {
            return new[]
            {
                Hash2(x + 0.0f, y + 0.0f),
                Hash2(x + 1.0f, y + 0.0f),
                Hash2(x + 0.0f, y + 1.0f),
                Hash2(x + 1.0f, y + 1.0f)
            };
        }

        // RGB <-> Lab (simplified Reinhard)
        public static Vector3 RgbToLab(Color c)
        {
            float r = c.r > 0.04045f ? Mathf.Pow((c.r + 0.055f) / 1.055f, 2.4f) : c.r / 12.92f;
            float g = c.g > 0.04045f ? Mathf.Pow((c.g + 0.055f) / 1.055f, 2.4f) : c.g / 12.92f;
            float b = c.b > 0.04045f ? Mathf.Pow((c.b + 0.055f) / 1.055f, 2.4f) : c.b / 12.92f;

            float x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            float y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            float z = r * 0.0193f + g * 0.1192f + b * 0.9505f;

            x = x > 0.008856f ? Mathf.Pow(x, 1f / 3f) : 7.787f * x + 16f / 116f;
            y = y > 0.008856f ? Mathf.Pow(y, 1f / 3f) : 7.787f * y + 16f / 116f;
            z = z > 0.008856f ? Mathf.Pow(z, 1f / 3f) : 7.787f * z + 16f / 116f;

            return new Vector3(
                116f * y - 16f,
                500f * (x - y),
                200f * (y - z)
            );
        }

        public static Color LabToRgb(Vector3 lab)
        {
            float y = (lab.x + 16f) / 116f;
            float x = lab.y / 500f + y;
            float z = y - lab.z / 200f;

            float x3 = x * x * x;
            float y3 = y * y * y;
            float z3 = z * z * z;

            x = x3 > 0.008856f ? x3 : (x - 16f / 116f) / 7.787f;
            y = y3 > 0.008856f ? y3 : (y - 16f / 116f) / 7.787f;
            z = z3 > 0.008856f ? z3 : (z - 16f / 116f) / 7.787f;

            float r = x * 3.2406f + y * -1.5372f + z * -0.4986f;
            float g = x * -0.9689f + y * 1.8758f + z * 0.0415f;
            float b = x * 0.0557f + y * -0.2040f + z * 1.0570f;

            r = r > 0.0031308f ? 1.055f * Mathf.Pow(r, 1f / 2.4f) - 0.055f : 12.92f * r;
            g = g > 0.0031308f ? 1.055f * Mathf.Pow(g, 1f / 2.4f) - 0.055f : 12.92f * g;
            b = b > 0.0031308f ? 1.055f * Mathf.Pow(b, 1f / 2.4f) - 0.055f : 12.92f * b;

            return new Color(
                Mathf.Clamp01(r),
                Mathf.Clamp01(g),
                Mathf.Clamp01(b),
                1f
            );
        }

        // Image stats
        public static void GetStats(Color32[] pixels, out Vector3 mean, out Vector3 std)
        {
            Vector3 sum = Vector3.zero;
            Vector3 sumSq = Vector3.zero;
            int n = pixels.Length;

            foreach (var p in pixels)
            {
                sum.x += p.r;
                sum.y += p.g;
                sum.z += p.b;
                sumSq.x += p.r * p.r;
                sumSq.y += p.g * p.g;
                sumSq.z += p.b * p.b;
            }

            mean = new Vector3(sum.x / n, sum.y / n, sum.z / n);
            std = new Vector3(
                Mathf.Sqrt(sumSq.x / n - mean.x * mean.x),
                Mathf.Sqrt(sumSq.y / n - mean.y * mean.y),
                Mathf.Sqrt(sumSq.z / n - mean.z * mean.z)
            );
        }

        // Smoothstep
        public static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}