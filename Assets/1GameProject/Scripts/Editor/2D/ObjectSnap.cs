using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegxlordTools.Editor
{
    public static class ObjectSnap
    {
        [MenuItem("Tools/Megxlord Toolbox/2D/Object Snap 2D")]
        private static void Execute()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length < 2)
            {
                Debug.LogWarning("[ObjectSnap] Выделите минимум 2 объекта.");
                return;
            }

            // Selection.gameObjects возвращает массив в порядке выбора:
            // последний выбранный объект — последний в массиве (primary selection).
            GameObject target = selected.Last();
            Vector3 targetPos = target.transform.position;

            Undo.SetCurrentGroupName("Object Snap 2D");

            foreach (GameObject go in selected)
            {
                if (go == target) continue;

                Undo.RecordObject(go.transform, "Snap Transform");
                Vector3 p = go.transform.position;
                go.transform.position = new Vector3(targetPos.x, targetPos.y, p.z);
            }
        }

        [MenuItem("Tools/Megxlord Toolbox/Tools/Object Snap 2D", true)]
        private static bool Validate()
        {
            return Selection.gameObjects.Length >= 2;
        }
    }
}