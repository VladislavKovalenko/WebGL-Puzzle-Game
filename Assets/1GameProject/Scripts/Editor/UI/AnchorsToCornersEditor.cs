using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public static class AnchorsToCornersEditor
    {
        [MenuItem("Tools/Megxlord UI/Anchors to Corners %#a")]
        public static void AnchorsToCorners()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("[AnchorsToCorners] Не выбрано ни одного объекта.");
                return;
            }

            int processedCount = 0;

            foreach (var go in selectedObjects)
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

            // Получаем мировые углы объекта
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            // Преобразуем мировые координаты в локальные координаты родителя
            Vector2 min = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 max = parent.InverseTransformPoint(worldCorners[2]);

            // Нормализуем относительно размеров родителя
            Vector2 parentSize = parent.rect.size;
            Vector2 anchorMin = new Vector2(
                min.x / parentSize.x + parent.pivot.x,
                min.y / parentSize.y + parent.pivot.y
            );
            Vector2 anchorMax = new Vector2(
                max.x / parentSize.x + parent.pivot.x,
                max.y / parentSize.y + parent.pivot.y
            );

            // Применяем anchors
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            // Сбрасываем offset'ы, чтобы объект остался на том же месте
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rectTransform);
        }

        [MenuItem("Tools/UI/Anchors to Corners %#a", validate = true)]
        private static bool ValidateAnchorsToCorners()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;

            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<RectTransform>() != null)
                    return true;
            }
            return false;
        }
    }
}


