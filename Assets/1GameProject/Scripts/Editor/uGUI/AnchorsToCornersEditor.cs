using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public class AnchorsToCorners : EditorWindow
    {
        private const float Epsilon = 0.001f;

        [MenuItem("Tools/Megxlord uGUI/Anchors to Corners")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnchorsToCorners>("Anchors to Corners");
            window.minSize = new Vector2(320, 220);
        }

        private void OnGUI()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool canExecute = CanExecute();
            EditorGUI.BeginDisabledGroup(!canExecute);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button("Anchors to Corners", buttonStyle, GUILayout.Width(240), GUILayout.Height(64)))
            {
                Execute();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (!canExecute)
                EditorGUILayout.LabelField("Выберите один или несколько объектов с RectTransform", EditorStyles.wordWrappedMiniLabel);
            else
                EditorGUILayout.LabelField($"Готово к обработке: {Selection.gameObjects.Length} объект(ов)", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        private static bool CanExecute()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;

            foreach (var go in Selection.gameObjects)
                if (go.GetComponent<RectTransform>() != null)
                    return true;

            return false;
        }

        private static void Execute()
        {
            int processedCount = 0;

            foreach (var go in Selection.gameObjects)
            {
                var rectTransform = go.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogWarning($"[AnchorsToCorners] Объект '{go.name}' не содержит RectTransform. Пропущен.");
                    continue;
                }

                Undo.RecordObject(rectTransform, "Anchors to Corners");
                SnapAnchorsToCorners(rectTransform);
                processedCount++;
            }

            Debug.Log($"[AnchorsToCorners] Обработано объектов: {processedCount}");
        }

        private static void SnapAnchorsToCorners(RectTransform rectTransform)
        {
            var parent = rectTransform.parent as RectTransform;
            if (parent == null)
            {
                Debug.LogWarning($"[AnchorsToCorners] Объект '{rectTransform.name}' не имеет родителя с RectTransform.");
                return;
            }

            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            Vector2 min = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 max = parent.InverseTransformPoint(worldCorners[2]);
            Vector2 parentSize = parent.rect.size;

            if (parentSize.x <= Epsilon || parentSize.y <= Epsilon)
            {
                Debug.LogWarning($"[AnchorsToCorners] Родитель '{parent.name}' имеет нулевой размер.");
                return;
            }

            Vector2 anchorMin = new Vector2(
                min.x / parentSize.x + parent.pivot.x,
                min.y / parentSize.y + parent.pivot.y
            );
            Vector2 anchorMax = new Vector2(
                max.x / parentSize.x + parent.pivot.x,
                max.y / parentSize.y + parent.pivot.y
            );

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rectTransform);
        }
    }
}