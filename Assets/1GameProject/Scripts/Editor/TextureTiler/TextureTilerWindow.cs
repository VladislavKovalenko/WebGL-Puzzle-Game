using System.IO;
using UnityEditor;
using UnityEngine;

namespace TextureTiler
{
    public class TextureTilerWindow : EditorWindow
    {
        Texture2D _sourceTexture;
        Texture2D _previewTexture;
        TextureTilerSettings _settings;
        Vector2 _scrollPos;
        bool _processing;
        //int _previewTileCount = 2; // Сколько плиток показывать в превью (2x2, 4x4 и т.д.)

        [MenuItem("Tools/Texture Tiler")]
        public static void ShowWindow()
        {
            GetWindow<TextureTilerWindow>("Texture Tiler");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawSource();
            DrawSettings();
            DrawActions();
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎨 Texture Tiler", titleStyle);
            EditorGUILayout.Space(5);
        }

        void DrawSource()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);

            _sourceTexture = EditorGUILayout.ObjectField(
                "Input", _sourceTexture, typeof(Texture2D), false
            ) as Texture2D;

            if (_sourceTexture != null)
            {
                EditorGUILayout.LabelField($"Size: {_sourceTexture.width}x{_sourceTexture.height}");
                EditorGUILayout.LabelField($"Format: {_sourceTexture.format}");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");

            // Settings asset
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            _settings = EditorGUILayout.ObjectField(
                "Settings Asset", _settings, typeof(TextureTilerSettings), false
            ) as TextureTilerSettings;

            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or assign a Settings Preset asset", MessageType.Info
                );
                if (GUILayout.Button("Create New Preset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Settings", "TilerSettings", "asset", ""
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        var so = CreateInstance<TextureTilerSettings>();
                        AssetDatabase.CreateAsset(so, path);
                        AssetDatabase.SaveAssets();
                        _settings = so;
                    }
                }
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Algorithm
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Algorithm", EditorStyles.boldLabel);
            _settings.Mode = (TextureTilerSettings.TilingMode)EditorGUILayout.EnumPopup(
                "Mode", _settings.Mode
            );

            // Output
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _settings.OutputResolution = EditorGUILayout.IntSlider(
                "Resolution", _settings.OutputResolution, 128, 2048
            );

            // Anti-Couch
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Anti-Couch (Remove Large-Scale Gradients)", EditorStyles.boldLabel);
            _settings.EnableFreqSeamless = EditorGUILayout.Toggle(
                "Freq-Sep Seamless", _settings.EnableFreqSeamless
            );
            _settings.EnableLCN = EditorGUILayout.Toggle(
                "Local Contrast Normalize", _settings.EnableLCN
            );
            _settings.EnableHistFlat = EditorGUILayout.Toggle(
                "Histogram Flattening", _settings.EnableHistFlat
            );
            _settings.EnableJitter = EditorGUILayout.Toggle(
                "Tile Jitter", _settings.EnableJitter
            );

            // Seamless Blending
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Seamless Blending", EditorStyles.boldLabel);
            _settings.EnablePoisson = EditorGUILayout.Toggle("Poisson", _settings.EnablePoisson);
            _settings.EnableMultiband = EditorGUILayout.Toggle("Multi-Band", _settings.EnableMultiband);
            _settings.EnableSeamCarve = EditorGUILayout.Toggle("Seam Carve", _settings.EnableSeamCarve);
            _settings.EnableEdgeAware = EditorGUILayout.Toggle("Edge-Aware", _settings.EnableEdgeAware);
            _settings.BandLevels = EditorGUILayout.IntSlider("Band Levels", _settings.BandLevels, 2, 8);

            // Color Matching
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Color Matching", EditorStyles.boldLabel);
            _settings.EnableColorMatch = EditorGUILayout.Toggle("Color Match", _settings.EnableColorMatch);
            _settings.EnableMeanStd = EditorGUILayout.Toggle("Mean/Std Norm", _settings.EnableMeanStd);

            // Variations
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Variations", EditorStyles.boldLabel);
            _settings.VariationCount = EditorGUILayout.IntSlider("Count", _settings.VariationCount, 2, 16);
            _settings.VoronoiSpread = EditorGUILayout.Slider("Voronoi Spread", _settings.VoronoiSpread, 1f, 20f);
            _settings.PatternCount = EditorGUILayout.IntSlider("Pattern Count", _settings.PatternCount, 2, 16);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _sourceTexture != null && _settings != null && !_processing;

            if (GUILayout.Button("▶ Process & Preview", GUILayout.Height(40)))
            {
                ProcessTexture();
            }

            if (GUILayout.Button("💾 Save as PNG", GUILayout.Height(40)))
            {
                SaveTexture();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        void DrawPreview()
            {
                if (_previewTexture == null) return;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                // Настройки сетки превью
                _settings.PreviewTileCount = EditorGUILayout.IntSlider("Tiles in Preview", _settings.PreviewTileCount, 1, 10);
                _settings.PreviewTilePadding = EditorGUILayout.IntSlider("Tile Padding", _settings.PreviewTilePadding, 0, 20);

                float maxWidth = position.width - 40;
                float aspect = (float)_previewTexture.width / _previewTexture.height;
                float displayHeight = maxWidth / aspect;

                Rect rect = GUILayoutUtility.GetRect(maxWidth, displayHeight);

                // Вычисляем размер одного тайла с учетом padding
                float totalPaddingX = _settings.PreviewTilePadding * (_settings.PreviewTileCount - 1);
                float totalPaddingY = _settings.PreviewTilePadding * (_settings.PreviewTileCount - 1);
                float tileW = (rect.width - totalPaddingX) / _settings.PreviewTileCount;
                float tileH = (rect.height - totalPaddingY) / _settings.PreviewTileCount;

                for (int ty = 0; ty < _settings.PreviewTileCount; ty++)
                {
                    for (int tx = 0; tx < _settings.PreviewTileCount; tx++)
                    {
                        Rect tileRect = new Rect(
                            rect.x + tx * (tileW + _settings.PreviewTilePadding),
                            rect.y + ty * (tileH + _settings.PreviewTilePadding),
                            tileW,
                            tileH
                        );
                        GUI.DrawTexture(tileRect, _previewTexture, ScaleMode.ScaleToFit);
                    }
                }

                EditorGUILayout.EndVertical();
            }

        void ProcessTexture()
        {
            _processing = true;
            try
            {
                // Ensure texture is readable
                string path = AssetDatabase.GetAssetPath(_sourceTexture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                _previewTexture = Processor.Process(_sourceTexture, _settings);
            }
            finally
            {
                _processing = false;
            }
        }

        void SaveTexture()
        {
            if (_previewTexture == null) return;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Tiled Texture",
                _sourceTexture.name + "_tiled",
                "png",
                "Choose save location"
            );

            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = _previewTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            // Configure import settings
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            EditorUtility.DisplayDialog("Saved", $"Texture saved to:\n{path}", "OK");
        }
    }
}