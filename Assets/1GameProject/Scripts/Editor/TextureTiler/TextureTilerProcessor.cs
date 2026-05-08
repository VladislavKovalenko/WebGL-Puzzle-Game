using System.Collections.Generic;
using UnityEngine;

namespace TextureTiler
{
    public static class Processor
    {
        // Main entry: process source texture with settings
        public static Texture2D Process(Texture2D source, TextureTilerSettings settings)
        {
            int w = source.width;
            int h = source.height;

            // Step 1: Base seamless (color match edges)
            Color32[] pixels = source.GetPixels32();
            if (settings.EnableColorMatch)
                MakeSeamless(ref pixels, w, h, Mathf.FloorToInt(Mathf.Min(w, h) * 0.15f));

            // Step 2: Anti-couch processing
            if (settings.EnableFreqSeamless)
                ApplyFrequencySeparationSeamless(ref pixels, w, h);
            if (settings.EnableLCN)
                ApplyLocalContrastNormalization(ref pixels, w, h, 16);
            if (settings.EnableHistFlat)
                ApplyHistogramFlattening(ref pixels, w, h);
            if (settings.EnableJitter)
                ApplyTileJitter(ref pixels, w, h);

            // Step 3: Advanced edge processing for non-quilting modes
            if (settings.Mode != TextureTilerSettings.TilingMode.Quilting &&
                (settings.EnablePoisson || settings.EnableMultiband ||
                 settings.EnableSeamCarve || settings.EnableEdgeAware))
            {
                ApplyAdvancedSeamless(ref pixels, w, h, settings);
            }

            // Step 4: Tiling processor
            Texture2D result = new Texture2D(settings.OutputResolution, settings.OutputResolution,
                TextureFormat.RGBA32, false);
            result.SetPixels32(RunProcessor(pixels, w, h, settings));
            result.Apply();
            return result;
        }

        // =====================================================================
        // SEAMLESS BASE
        // =====================================================================

        static void MakeSeamless(ref Color32[] data, int w, int h, int blend)
        {
            // Horizontal: blend right INTO left, then left INTO right
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < blend; x++)
                {
                    int li = y * w + x;
                    int ri = y * w + (w - blend + x);
                    float f = (float)x / blend;
                    float inv = 1f - f;

                    data[li].r = (byte)(data[ri].r * inv + data[li].r * f);
                    data[li].g = (byte)(data[ri].g * inv + data[li].g * f);
                    data[li].b = (byte)(data[ri].b * inv + data[li].b * f);
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < blend; x++)
                {
                    int li = y * w + x;
                    int ri = y * w + (w - blend + x);
                    float f = (float)x / blend;

                    data[ri].r = (byte)(data[li].r * f + data[ri].r * (1f - f));
                    data[ri].g = (byte)(data[li].g * f + data[ri].g * (1f - f));
                    data[ri].b = (byte)(data[li].b * f + data[ri].b * (1f - f));
                }
            }

            // Vertical (same pattern)
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < blend; y++)
                {
                    int ti = y * w + x;
                    int bi = (h - blend + y) * w + x;
                    float f = (float)y / blend;
                    float inv = 1f - f;

                    data[ti].r = (byte)(data[bi].r * inv + data[ti].r * f);
                    data[ti].g = (byte)(data[bi].g * inv + data[ti].g * f);
                    data[ti].b = (byte)(data[bi].b * inv + data[ti].b * f);
                }
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < blend; y++)
                {
                    int ti = y * w + x;
                    int bi = (h - blend + y) * w + x;
                    float f = (float)y / blend;

                    data[bi].r = (byte)(data[ti].r * f + data[bi].r * (1f - f));
                    data[bi].g = (byte)(data[ti].g * f + data[bi].g * (1f - f));
                    data[bi].b = (byte)(data[ti].b * f + data[bi].b * (1f - f));
                }
            }
        }

        // =====================================================================
        // ANTI-COUCH ALGORITHMS
        // =====================================================================

        static void ApplyFrequencySeparationSeamless(ref Color32[] data, int w, int h)
        {
            // Blur for low freq (box blur approximation)
            Color32[] lowFreq = BoxBlur(data, w, h, Mathf.FloorToInt(Mathf.Min(w, h) * 0.08f));
            Color32[] highFreq = new Color32[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                highFreq[i] = new Color32(
                    (byte)Mathf.Clamp(data[i].r - lowFreq[i].r + 128, 0, 255),
                    (byte)Mathf.Clamp(data[i].g - lowFreq[i].g + 128, 0, 255),
                    (byte)Mathf.Clamp(data[i].b - lowFreq[i].b + 128, 0, 255),
                    255
                );
            }

            // Make low-freq seamless with wider blend
            int blend = Mathf.FloorToInt(Mathf.Min(w, h) * 0.25f);
            MakeSeamless(ref lowFreq, w, h, blend);

            // Recombine
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Color32(
                    (byte)Mathf.Clamp(lowFreq[i].r + highFreq[i].r - 128, 0, 255),
                    (byte)Mathf.Clamp(lowFreq[i].g + highFreq[i].g - 128, 0, 255),
                    (byte)Mathf.Clamp(lowFreq[i].b + highFreq[i].b - 128, 0, 255),
                    255
                );
            }
        }

        static Color32[] BoxBlur(Color32[] src, int w, int h, int radius)
        {
            Color32[] dst = new Color32[src.Length];
            int r2 = radius * 2 + 1;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int sr = 0, sg = 0, sb = 0, count = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int py = Mathf.Clamp(y + dy, 0, h - 1);
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int px = Mathf.Clamp(x + dx, 0, w - 1);
                            int idx = py * w + px;
                            sr += src[idx].r;
                            sg += src[idx].g;
                            sb += src[idx].b;
                            count++;
                        }
                    }
                    dst[y * w + x] = new Color32(
                        (byte)(sr / count),
                        (byte)(sg / count),
                        (byte)(sb / count),
                        255
                    );
                }
            }
            return dst;
        }

        static void ApplyLocalContrastNormalization(ref Color32[] data, int w, int h, int window)
        {
            int half = window / 2;
            Vector3[] localMean = new Vector3[data.Length];
            Vector3[] localStd = new Vector3[data.Length];

            // Compute local stats
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector3 sum = Vector3.zero;
                    Vector3 sumSq = Vector3.zero;
                    int count = 0;

                    for (int dy = -half; dy <= half; dy++)
                    {
                        int py = Mathf.Clamp(y + dy, 0, h - 1);
                        for (int dx = -half; dx <= half; dx++)
                        {
                            int px = Mathf.Clamp(x + dx, 0, w - 1);
                            var p = data[py * w + px];
                            sum.x += p.r; sum.y += p.g; sum.z += p.b;
                            sumSq.x += p.r * p.r;
                            sumSq.y += p.g * p.g;
                            sumSq.z += p.b * p.b;
                            count++;
                        }
                    }

                    int idx = y * w + x;
                    localMean[idx] = new Vector3(sum.x / count, sum.y / count, sum.z / count);
                    localStd[idx] = new Vector3(
                        Mathf.Sqrt(Mathf.Max(0, sumSq.x / count - localMean[idx].x * localMean[idx].x)),
                        Mathf.Sqrt(Mathf.Max(0, sumSq.y / count - localMean[idx].y * localMean[idx].y)),
                        Mathf.Sqrt(Mathf.Max(0, sumSq.z / count - localMean[idx].z * localMean[idx].z))
                    );
                }
            }

            // Global stats
            Utils.GetStats(data, out Vector3 gMean, out Vector3 gStd);

            // Normalize
            for (int i = 0; i < data.Length; i++)
            {
                var p = data[i];
                data[i] = new Color32(
                    (byte)Mathf.Clamp(((p.r - localMean[i].x) / Mathf.Max(localStd[i].x, 1)) * gStd.x + gMean.x, 0, 255),
                    (byte)Mathf.Clamp(((p.g - localMean[i].y) / Mathf.Max(localStd[i].y, 1)) * gStd.y + gMean.y, 0, 255),
                    (byte)Mathf.Clamp(((p.b - localMean[i].z) / Mathf.Max(localStd[i].z, 1)) * gStd.z + gMean.z, 0, 255),
                    255
                );
            }
        }

        static void ApplyHistogramFlattening(ref Color32[] data, int w, int h)
        {
            for (int c = 0; c < 3; c++)
            {
                int[] hist = new int[256];
                foreach (var p in data)
                {
                    int v = c == 0 ? p.r : c == 1 ? p.g : p.b;
                    hist[v]++;
                }

                int[] cdf = new int[256];
                cdf[0] = hist[0];
                for (int i = 1; i < 256; i++) cdf[i] = cdf[i - 1] + hist[i];

                int cdfMin = cdf[0];
                int cdfMax = cdf[255] - cdfMin;

                for (int i = 0; i < data.Length; i++)
                {
                    int v = c == 0 ? data[i].r : c == 1 ? data[i].g : data[i].b;
                    int nv = Mathf.RoundToInt((float)(cdf[v] - cdfMin) / Mathf.Max(cdfMax, 1) * 255);
                    nv = Mathf.Clamp(nv, 0, 255);

                    if (c == 0) data[i].r = (byte)nv;
                    else if (c == 1) data[i].g = (byte)nv;
                    else data[i].b = (byte)nv;
                }
            }
        }

        static void ApplyTileJitter(ref Color32[] data, int w, int h)
        {
            // Create 3x3 grid with random offsets
            float jitter = 0.3f;
            System.Random rng = new System.Random();

            Color32[] grid = new Color32[w * 3 * h * 3];
            Vector2[] offsets = new Vector2[9];

            for (int i = 0; i < 9; i++)
            {
                offsets[i] = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * w * jitter,
                    (float)(rng.NextDouble() - 0.5) * h * jitter
                );
            }

            // Fill grid
            for (int ty = 0; ty < 3; ty++)
            {
                for (int tx = 0; tx < 3; tx++)
                {
                    int idx = ty * 3 + tx;
                    Vector2 off = offsets[idx];

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int sx = Mathf.FloorToInt(Mathf.Clamp(x + off.x, 0, w - 1));
                            int sy = Mathf.FloorToInt(Mathf.Clamp(y + off.y, 0, h - 1));
                            grid[(ty * h + y) * w * 3 + (tx * w + x)] = data[sy * w + sx];
                        }
                    }
                }
            }

            // Blend seams
            int overlap = Mathf.FloorToInt(Mathf.Min(w, h) * 0.15f);
            BlendGridSeams(ref grid, w * 3, h * 3, w, h, overlap);

            // Extract center
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    data[y * w + x] = grid[(h + y) * w * 3 + (w + x)];
                }
            }
        }

        static void BlendGridSeams(ref Color32[] grid, int gw, int gh, int tw, int th, int overlap)
        {
            // Horizontal seams
            for (int y = 0; y < gh; y++)
            {
                for (int t = 1; t < 3; t++)
                {
                    int seamX = t * tw;
                    for (int x = 0; x < overlap; x++)
                    {
                        int li = y * gw + (seamX - overlap + x);
                        int ri = y * gw + (seamX + x);
                        float f = Utils.Smoothstep(0, overlap, x);

                        grid[li].r = (byte)(grid[li].r * (1 - f) + grid[ri].r * f);
                        grid[li].g = (byte)(grid[li].g * (1 - f) + grid[ri].g * f);
                        grid[li].b = (byte)(grid[li].b * (1 - f) + grid[ri].b * f);
                        grid[ri] = grid[li];
                    }
                }
            }

            // Vertical seams
            for (int x = 0; x < gw; x++)
            {
                for (int t = 1; t < 3; t++)
                {
                    int seamY = t * th;
                    for (int y = 0; y < overlap; y++)
                    {
                        int ti = (seamY - overlap + y) * gw + x;
                        int bi = (seamY + y) * gw + x;
                        float f = Utils.Smoothstep(0, overlap, y);

                        grid[ti].r = (byte)(grid[ti].r * (1 - f) + grid[bi].r * f);
                        grid[ti].g = (byte)(grid[ti].g * (1 - f) + grid[bi].g * f);
                        grid[ti].b = (byte)(grid[ti].b * (1 - f) + grid[bi].b * f);
                        grid[bi] = grid[ti];
                    }
                }
            }
        }

        // =====================================================================
        // ADVANCED SEAMLESS (2x2 grid + blend)
        // =====================================================================

        static void ApplyAdvancedSeamless(ref Color32[] data, int w, int h, TextureTilerSettings s)
        {
            int overlap = Mathf.FloorToInt(Mathf.Min(w, h) * 0.15f);

            // Build 2x2 grid
            Color32[] grid = new Color32[w * 2 * h * 2];
            for (int y = 0; y < h * 2; y++)
            {
                for (int x = 0; x < w * 2; x++)
                {
                    grid[y * w * 2 + x] = data[(y % h) * w + (x % w)];
                }
            }

            // Extract and blend vertical seam
            Color32[] left = ExtractPatch(grid, w * 2, h * 2, w - overlap, 0, overlap, h * 2);
            Color32[] right = ExtractPatch(grid, w * 2, h * 2, w, 0, overlap, h * 2);
            Color32[] blendedV = BlendPatches(left, right, overlap, h * 2, s);

            // Extract and blend horizontal seam
            Color32[] top = ExtractPatch(grid, w * 2, h * 2, 0, h - overlap, w * 2, overlap);
            Color32[] bot = ExtractPatch(grid, w * 2, h * 2, 0, h, w * 2, overlap);
            Color32[] blendedH = BlendPatches(top, bot, w * 2, overlap, s);

            // Write back
            WritePatch(ref grid, w * 2, h * 2, w - overlap, 0, blendedV);
            WritePatch(ref grid, w * 2, h * 2, 0, h - overlap, blendedH);

            // Extract center tile
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    data[y * w + x] = grid[y * w * 2 + x];
                }
            }
        }

        static Color32[] ExtractPatch(Color32[] src, int sw, int sh, int x, int y, int w, int h)
        {
            Color32[] dst = new Color32[w * h];
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    dst[dy * w + dx] = src[(y + dy) * sw + (x + dx)];
                }
            }
            return dst;
        }

        static void WritePatch(ref Color32[] dst, int dw, int dh, int x, int y, Color32[] src)
        {
            int w = src.Length; // approximate, assumes rectangular
            w = Mathf.FloorToInt(Mathf.Sqrt(src.Length)); // hack, use proper dims
        }

        static Color32[] BlendPatches(Color32[] a, Color32[] b, int w, int h, TextureTilerSettings s)
        {
            if (s.EnablePoisson) return PoissonBlend(a, b, w, h);
            if (s.EnableEdgeAware) return EdgeAwareBlend(a, b, w, h);
            if (s.EnableSeamCarve) return SeamCarveBlend(a, b, w, h);
            if (s.EnableMultiband) return MultiBandBlend(a, b, w, h, s.BandLevels);
            return GradientBlend(a, b, w, h);
        }

        // =====================================================================
        // BLENDING ALGORITHMS
        // =====================================================================

        static Color32[] GradientBlend(Color32[] a, Color32[] b, int w, int h)
        {
            Color32[] r = new Color32[a.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float f = Utils.Smoothstep(0, w, x);
                    int i = y * w + x;
                    r[i] = new Color32(
                        (byte)(a[i].r * (1 - f) + b[i].r * f),
                        (byte)(a[i].g * (1 - f) + b[i].g * f),
                        (byte)(a[i].b * (1 - f) + b[i].b * f),
                        255
                    );
                }
            }
            return r;
        }

        static Color32[] PoissonBlend(Color32[] patch, Color32[] target, int w, int h)
        {
            // Simplified: gradient-guided average
            Color32[] r = new Color32[patch.Length];
            for (int i = 0; i < patch.Length; i++)
            {
                r[i] = new Color32(
                    (byte)((patch[i].r + target[i].r) / 2),
                    (byte)((patch[i].g + target[i].g) / 2),
                    (byte)((patch[i].b + target[i].b) / 2),
                    255
                );
            }
            return r;
        }

        static Color32[] EdgeAwareBlend(Color32[] patch, Color32[] target, int w, int h)
        {
            Color32[] r = new Color32[patch.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    // Compute edge magnitude
                    float edge = 0;
                    if (x > 0 && x < w - 1 && y > 0 && y < h - 1)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int v = c == 0 ? patch[i].r : c == 1 ? patch[i].g : patch[i].b;
                            int vl = c == 0 ? patch[i - 1].r : c == 1 ? patch[i - 1].g : patch[i - 1].b;
                            int vr = c == 0 ? patch[i + 1].r : c == 1 ? patch[i + 1].g : patch[i + 1].b;
                            int vt = c == 0 ? patch[i - w].r : c == 1 ? patch[i - w].g : patch[i - w].b;
                            int vb = c == 0 ? patch[i + w].r : c == 1 ? patch[i + w].g : patch[i + w].b;
                            edge += Mathf.Abs(vr - vl) + Mathf.Abs(vb - vt);
                        }
                    }
                    edge = Mathf.Min(edge / 1000f, 1f);

                    float feather = w * (0.3f + 0.7f * (1f - edge));
                    float f = Utils.Smoothstep(0, feather, x);

                    r[i] = new Color32(
                        (byte)(patch[i].r * f + target[i].r * (1 - f)),
                        (byte)(patch[i].g * f + target[i].g * (1 - f)),
                        (byte)(patch[i].b * f + target[i].b * (1 - f)),
                        255
                    );
                }
            }
            return r;
        }

        static Color32[] SeamCarveBlend(Color32[] patch, Color32[] target, int w, int h)
        {
            // DP seam finding
            float[] error = new float[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float dr = patch[i].r - target[i].r;
                    float dg = patch[i].g - target[i].g;
                    float db = patch[i].b - target[i].b;
                    error[i] = dr * dr + dg * dg + db * db;
                }
            }

            float[] cost = new float[w * h];
            int[] path = new int[w * h];

            for (int x = 0; x < w; x++) cost[x] = error[x];

            for (int y = 1; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float minC = cost[(y - 1) * w + x];
                    int minX = x;
                    if (x > 0 && cost[(y - 1) * w + (x - 1)] < minC) { minC = cost[(y - 1) * w + (x - 1)]; minX = x - 1; }
                    if (x < w - 1 && cost[(y - 1) * w + (x + 1)] < minC) { minC = cost[(y - 1) * w + (x + 1)]; minX = x + 1; }

                    cost[y * w + x] = minC + error[y * w + x];
                    path[y * w + x] = minX;
                }
            }

            // Find min at bottom
            int minIdx = 0;
            for (int x = 1; x < w; x++)
                if (cost[(h - 1) * w + x] < cost[(h - 1) * w + minIdx]) minIdx = x;

            // Backtrack
            int[] seam = new int[h];
            int cx = minIdx;
            for (int y = h - 1; y >= 0; y--)
            {
                seam[y] = cx;
                cx = path[y * w + cx];
            }

            // Blend around seam
            Color32[] r = new Color32[patch.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    int split = seam[y];
                    float f;
                    if (x < split - 5) f = 1;
                    else if (x > split + 5) f = 0;
                    else f = 0.5f + (split - x) / 10f;
                    f = Mathf.Clamp01(f);

                    r[i] = new Color32(
                        (byte)(patch[i].r * f + target[i].r * (1 - f)),
                        (byte)(patch[i].g * f + target[i].g * (1 - f)),
                        (byte)(patch[i].b * f + target[i].b * (1 - f)),
                        255
                    );
                }
            }
            return r;
        }

        static Color32[] MultiBandBlend(Color32[] patch, Color32[] target, int w, int h, int levels)
        {
            // Simplified: just gradient blend for now
            // Full Laplacian pyramid requires more code
            return GradientBlend(patch, target, w, h);
        }

        // =====================================================================
        // TILING PROCESSORS
        // =====================================================================

        static Color32[] RunProcessor(Color32[] src, int sw, int sh, TextureTilerSettings s)
        {
            int res = s.OutputResolution;
            Color32[] dst = new Color32[res * res];

            switch (s.Mode)
            {
                case TextureTilerSettings.TilingMode.Basic:
                    BasicTile(src, sw, sh, dst, res);
                    break;
                case TextureTilerSettings.TilingMode.WangTiles:
                    WangTiles(src, sw, sh, dst, res);
                    break;
                case TextureTilerSettings.TilingMode.Stochastic:
                    Stochastic(src, sw, sh, dst, res, s);
                    break;
                case TextureTilerSettings.TilingMode.Quilting:
                    Quilting(src, sw, sh, dst, res, s);
                    break;
                case TextureTilerSettings.TilingMode.Seamless:
                    Seamless(src, sw, sh, dst, res);
                    break;
                case TextureTilerSettings.TilingMode.Tech1HashTile:
                    Tech1HashTile(src, sw, sh, dst, res);
                    break;
                case TextureTilerSettings.TilingMode.Tech2Voronoi:
                    Tech2Voronoi(src, sw, sh, dst, res, s);
                    break;
                case TextureTilerSettings.TilingMode.Tech3Virtual:
                    Tech3Virtual(src, sw, sh, dst, res, s);
                    break;
                default:
                    BasicTile(src, sw, sh, dst, res);
                    break;
            }

            return dst;
        }

        static void BasicTile(Color32[] src, int sw, int sh, Color32[] dst, int res)
        {
            // Реальный тайлинг: повторяем исходную текстуру по сетке в выходном разрешении
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    dst[y * res + x] = src[(y % sh) * sw + (x % sw)];
                }
            }
        }

        static void Seamless(Color32[] src, int sw, int sh, Color32[] dst, int res)
        {
            BasicTile(src, sw, sh, dst, res);
        }

        static void WangTiles(Color32[] src, int sw, int sh, Color32[] dst, int res)
        {
            int tileSize = Mathf.Min(sw, sh);
            // Pre-generate 4 rotated variants
            Color32[][] variants = new Color32[4][];
            variants[0] = src; // 0°
            variants[1] = Rotate90(src, sw, sh);
            variants[2] = Rotate180(src, sw, sh);
            variants[3] = Rotate270(src, sw, sh);

            System.Random rng = new System.Random();

            for (int y = 0; y < res; y += tileSize)
            {
                for (int x = 0; x < res; x += tileSize)
                {
                    int v = rng.Next(4);
                    Color32[] var = variants[v];
                    int vw = v % 2 == 0 ? sw : sh;
                    int vh = v % 2 == 0 ? sh : sw;

                    for (int dy = 0; dy < tileSize && y + dy < res; dy++)
                    {
                        for (int dx = 0; dx < tileSize && x + dx < res; dx++)
                        {
                            dst[(y + dy) * res + (x + dx)] = var[(dy % vh) * vw + (dx % vw)];
                        }
                    }
                }
            }
        }

        static Color32[] Rotate90(Color32[] src, int w, int h)
        {
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dst[x * h + (h - 1 - y)] = src[y * w + x];
            return dst;
        }

        static Color32[] Rotate180(Color32[] src, int w, int h)
        {
            Color32[] dst = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++)
                dst[src.Length - 1 - i] = src[i];
            return dst;
        }

        static Color32[] Rotate270(Color32[] src, int w, int h)
        {
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dst[(w - 1 - x) * h + y] = src[y * w + x];
            return dst;
        }

        static void Stochastic(Color32[] src, int sw, int sh, Color32[] dst, int res, TextureTilerSettings s)
        {
            System.Random rng = new System.Random();
            int overlap = Mathf.FloorToInt(sw * 0.3f);
            int stepX = sw - overlap;
            int stepY = sh - overlap;

            // Generate variations
            List<Color32[]> variations = new List<Color32[]>();
            for (int i = 0; i < s.VariationCount; i++)
            {
                Color32[] v = new Color32[src.Length];
                float hue = (float)(rng.NextDouble() * 20 - 10);
                float bright = 1f + (float)(rng.NextDouble() * 0.1f - 0.05f);

                for (int p = 0; p < src.Length; p++)
                {
                    Color c = src[p];
                    // Simple brightness + slight hue shift approximation
                    float h, S, vval;
                    Color.RGBToHSV(c, out h, out S, out vval);
                    h = (h + hue / 360f) % 1f;
                    vval = Mathf.Clamp01(vval * bright);
                    Color nc = Color.HSVToRGB(h, S, vval);
                    v[p] = new Color32(
                        (byte)(nc.r * 255),
                        (byte)(nc.g * 255),
                        (byte)(nc.b * 255),
                        255
                    );
                }
                variations.Add(v);
            }

            for (int y = -overlap; y < res; y += stepY)
            {
                for (int x = -overlap; x < res; x += stepX)
                {
                    int vi = rng.Next(variations.Count);
                    Color32[] var = variations[vi];

                    for (int dy = 0; dy < sh && y + dy < res; dy++)
                    {
                        int py = y + dy;
                        if (py < 0) continue;
                        for (int dx = 0; dx < sw && x + dx < res; dx++)
                        {
                            int px = x + dx;
                            if (px < 0) continue;
                            dst[py * res + px] = var[dy * sw + dx];
                        }
                    }
                }
            }
        }

        static void Quilting(Color32[] src, int sw, int sh, Color32[] dst, int res, TextureTilerSettings s)
        {
            int patchSize = Mathf.Min(sw, sh);
            int overlap = s.SeamOverlap;
            int step = patchSize - overlap;

            // First patch
            CopyPatch(src, sw, sh, dst, res, 0, 0, patchSize, patchSize, 0, 0);

            for (int y = 0; y < res; y += step)
            {
                for (int x = 0; x < res; x += step)
                {
                    if (x == 0 && y == 0) continue;

                    // Simple copy for now — full quilting with seam finding is complex
                    CopyPatch(src, sw, sh, dst, res, 0, 0, patchSize, patchSize, x, y);
                }
            }
        }

        static void CopyPatch(Color32[] src, int sw, int sh, Color32[] dst, int dw,
            int sx, int sy, int w, int h, int dx, int dy)
        {
            for (int y = 0; y < h && dy + y < dw; y++)
            {
                for (int x = 0; x < w && dx + x < dw; x++)
                {
                    dst[(dy + y) * dw + (dx + x)] = src[((sy + y) % sh) * sw + ((sx + x) % sw)];
                }
            }
        }

        static void Tech1HashTile(Color32[] src, int sw, int sh, Color32[] dst, int res)
        {
            int tileW = sw, tileH = sh;
            int tilesX = Mathf.CeilToInt((float)res / tileW) + 1;
            int tilesY = Mathf.CeilToInt((float)res / tileH) + 1;

            for (int ty = -1; ty < tilesY; ty++)
            {
                for (int tx = -1; tx < tilesX; tx++)
                {
                    float[] h = Utils.Hash4(tx, ty);
                    int px = tx * tileW;
                    int py = ty * tileH;

                    // Random offset + mirror
                    int ox = Mathf.FloorToInt(h[0] * 0.5f * tileW);
                    int oy = Mathf.FloorToInt(h[1] * 0.5f * tileH);
                    bool mx = h[2] > 0.5f;
                    bool my = h[3] > 0.5f;

                    for (int y = 0; y < tileH && py + y < res; y++)
                    {
                        for (int x = 0; x < tileW && px + x < res; x++)
                        {
                            int sx = mx ? tileW - 1 - x : x;
                            int sy = my ? tileH - 1 - y : y;
                            sx = (sx + ox) % sw;
                            sy = (sy + oy) % sh;

                            if (py + y >= 0 && px + x >= 0)
                                dst[(py + y) * res + (px + x)] = src[sy * sw + sx];
                        }
                    }
                }
            }
        }

        static void Tech2Voronoi(Color32[] src, int sw, int sh, Color32[] dst, int res, TextureTilerSettings s)
        {
            float spread = s.VoronoiSpread;
            float cellSize = Mathf.Max(sw, sh) * 1.5f;
            int cellsX = Mathf.CeilToInt(res / cellSize) + 2;
            int cellsY = Mathf.CeilToInt(res / cellSize) + 2;

            // Generate feature points
            List<Vector4> features = new List<Vector4>(); // x, y, ou, ov
            for (int cy = -1; cy < cellsY; cy++)
            {
                for (int cx = -1; cx < cellsX; cx++)
                {
                    float[] h = Utils.Hash4(cx, cy);
                    features.Add(new Vector4(
                        cx * cellSize + h[0] * cellSize,
                        cy * cellSize + h[1] * cellSize,
                        h[2] * 0.3f - 0.15f,
                        h[3] * 0.3f - 0.15f
                    ));
                }
            }

            // For each pixel, accumulate weighted colors
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float sumR = 0, sumG = 0, sumB = 0, sumW = 0;

                    foreach (var f in features)
                    {
                        float dx = x - f.x;
                        float dy = y - f.y;
                        float d2 = dx * dx + dy * dy;
                        float w = Mathf.Exp(-spread * d2 / (cellSize * cellSize));

                        if (w < 0.001f) continue;

                        float u = (float)x / sw + f.z;
                        float v = (float)y / sh + f.w;
                        int sx = Mathf.FloorToInt(((u % 1) + 1) % 1 * sw);
                        int sy = Mathf.FloorToInt(((v % 1) + 1) % 1 * sh);
                        Color32 c = src[sy * sw + sx];

                        sumR += w * c.r;
                        sumG += w * c.g;
                        sumB += w * c.b;
                        sumW += w;
                    }

                    if (sumW < 0.001f) sumW = 1;
                    dst[y * res + x] = new Color32(
                        (byte)Mathf.Clamp(sumR / sumW, 0, 255),
                        (byte)Mathf.Clamp(sumG / sumW, 0, 255),
                        (byte)Mathf.Clamp(sumB / sumW, 0, 255),
                        255
                    );
                }
            }
        }

        static void Tech3Virtual(Color32[] src, int sw, int sh, Color32[] dst, int res, TextureTilerSettings s)
        {
            int patternCount = s.PatternCount;

            // Generate offsets
            Vector2[] offsets = new Vector2[patternCount];
            for (int i = 0; i < patternCount; i++)
            {
                offsets[i] = new Vector2(
                    Mathf.Sin(3f * i) * 0.5f,
                    Mathf.Sin(7f * i) * 0.5f
                );
            }

            // Precompute low-freq noise
            int noiseScale = Mathf.Max(1, Mathf.FloorToInt(res * 0.05f));
            float[] noise = new float[noiseScale * noiseScale];
            for (int y = 0; y < noiseScale; y++)
            {
                for (int x = 0; x < noiseScale; x++)
                {
                    noise[y * noiseScale + x] = Utils.Hash2(x, y);
                }
            }

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / sw;
                    float v = (float)y / sh;

                    // Sample noise (bilinear)
                    float nx = (float)x / res * noiseScale;
                    float ny = (float)y / res * noiseScale;
                    int nx0 = Mathf.FloorToInt(nx) % noiseScale;
                    int ny0 = Mathf.FloorToInt(ny) % noiseScale;
                    int nx1 = (nx0 + 1) % noiseScale;
                    int ny1 = (ny0 + 1) % noiseScale;
                    float fx = nx - nx0;
                    float fy = ny - ny0;

                    float n00 = noise[ny0 * noiseScale + nx0];
                    float n10 = noise[ny0 * noiseScale + nx1];
                    float n01 = noise[ny1 * noiseScale + nx0];
                    float n11 = noise[ny1 * noiseScale + nx1];
                    float k = Mathf.Lerp(Mathf.Lerp(n00, n10, fx), Mathf.Lerp(n01, n11, fx), fy);

                    float index = k * patternCount;
                    int iA = Mathf.FloorToInt(index) % patternCount;
                    int iB = (iA + 1) % patternCount;
                    float fIdx = index - Mathf.Floor(index);

                    Vector2 offA = offsets[iA];
                    Vector2 offB = offsets[iB];

                    Color32 cA = SampleSource(src, sw, sh, u + offA.x, v + offA.y);
                    Color32 cB = SampleSource(src, sw, sh, u + offB.x, v + offB.y);

                    float diff = Mathf.Abs(cA.r - cB.r) + Mathf.Abs(cA.g - cB.g) + Mathf.Abs(cA.b - cB.b);
                    float boost = diff / 765f;

                    float t = fIdx - 0.1f * boost;
                    t = Mathf.Clamp01(t);
                    t = t * t * (3f - 2f * t);

                    dst[y * res + x] = new Color32(
                        (byte)Mathf.Lerp(cA.r, cB.r, t),
                        (byte)Mathf.Lerp(cA.g, cB.g, t),
                        (byte)Mathf.Lerp(cA.b, cB.b, t),
                        255
                    );
                }
            }
        }

        static Color32 SampleSource(Color32[] src, int w, int h, float u, float v)
        {
            u = ((u % 1) + 1) % 1;
            v = ((v % 1) + 1) % 1;
            int x = Mathf.FloorToInt(u * w);
            int y = Mathf.FloorToInt(v * h);
            return src[y * w + x];
        }
    }
}