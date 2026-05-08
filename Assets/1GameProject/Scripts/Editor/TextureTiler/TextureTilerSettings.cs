using UnityEngine;

namespace TextureTiler
{
    [CreateAssetMenu(fileName = "TilerSettings", menuName = "Texture Tiler/Settings Preset")]
    public class TextureTilerSettings : ScriptableObject
    {
        // Algorithm
        public TilingMode Mode = TilingMode.Basic;

        // Output
        public int OutputResolution = 256;
        public int PreviewSize = 400;

        // Color Matching
        public bool EnableColorMatch = true;
        public bool EnableMeanStd = true;
        public bool EnableColorTransfer = false;
        public bool EnableClahe = false;
        public float ClaheClipLimit = 2.0f;

        // Seamless Blending
        public bool EnablePoisson = false;
        public bool EnableMultiband = true;
        public bool EnableSeamCarve = false;
        public bool EnableEdgeAware = false;
        public int BandLevels = 4;

        // Anti-Couch (Remove large-scale gradients)
        public bool EnableFreqSeamless = false;
        public bool EnableLCN = false;
        public bool EnableHistFlat = false;
        public bool EnableJitter = false;

        // Patch
        public bool EnableGainOffset = true;
        public bool EnableFreqSep = false;
        public int SeamOverlap = 20;

        // Variations
        public int VariationCount = 4;
        public float VoronoiSpread = 5.0f;
        public int PatternCount = 8;
        public float BlendIntensity = 0.5f;

        // Preview Settings
        [Tooltip("How many tiles to show in preview grid (e.g. 2 = 2x2 grid)")]
        public int PreviewTileCount = 2;
        [Tooltip("Padding between tiles in preview (pixels)")]
        [Range(0, 20)]
        public int PreviewTilePadding = 0;

        public enum TilingMode
        {
            Basic, WangTiles, Stochastic, Quilting, NineSlice,
            Variations, Adaptive, GradientBlend, Seamless,
            Tech1HashTile, Tech2Voronoi, Tech3Virtual
        }
    }
}