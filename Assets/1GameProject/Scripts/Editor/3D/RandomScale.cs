using UnityEditor;
using UnityEngine;

public class RandomScale : EditorWindow
{
    private float _minScale = 0.5f;
    private float _maxScale = 2.0f;

    [MenuItem("Tools/Megxlord Toolbox/3D/RandomScale")]
    public static void ShowWindow()
    {
        GetWindow<RandomScale>("Random Scale");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Min Scale Value");
        _minScale = EditorGUILayout.FloatField(_minScale);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Max Scale Value");
        _maxScale = EditorGUILayout.FloatField(_maxScale);
        EditorGUILayout.Space();

        if (_minScale > _maxScale)
        {
            EditorGUILayout.HelpBox("Min Scale should be less than Max Scale", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Current Range: {_minScale:F2} - {_maxScale:F2}");
        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Random Scale"))
        {
            ApplyRandomScaleToSelectedObjects();
        }
    }

    private void ApplyRandomScaleToSelectedObjects()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null)
            {
                return;
            }

            Undo.RecordObject(obj.transform, "Apply Random Scale");

            float randomScale = Random.Range(_minScale, _maxScale);
            obj.transform.localScale = Vector3.one * randomScale;

            EditorUtility.SetDirty(obj.transform);
            Debug.Log($"Scaled {obj.name} to {randomScale:F2}");
        }
    }
}