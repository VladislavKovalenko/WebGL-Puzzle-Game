using UnityEngine;
using UnityEditor;

public class SnapToTerrain : EditorWindow
{
    private Terrain selectedTerrain;
    private GameObject selectedObject;

    [MenuItem("Tools/MegxlordScene/Snap To Terrain")]
    public static void ShowWindow()
    {
        GetWindow<SnapToTerrain>("Snap To Terrain");
    }

    private enum PivotMode { Edge, Center }
    private PivotMode pivotMode = PivotMode.Edge;

    private enum Language { English, Russian }
    private Language currentLanguage = Language.Russian;

    // Тексты для двух языков
    private static readonly System.Collections.Generic.Dictionary<string, string[]> translations = new System.Collections.Generic.Dictionary<string, string[]>
    {
        {"SelectTerrain", new[]{"Select Terrain:", "Выберите Terrain:"}},
        {"SelectMode", new[]{"Select Snap Mode:", "Выберите режим привязки:"}},
        {"PivotMode", new[]{"Pivot Mode", "Режим Pivot"}},
        {"Edge", new[]{"Pivot on Edge", "Pivot по краю"}},
        {"Center", new[]{"Pivot on Center", "Pivot по центру"}},
        {"SelectObjects", new[]{"Select object(s) in scene:", "Выделите объект(ы) на сцене:"}},
        {"SelectedObject", new[]{"Selected object:", "Выбран объект:"}},
        {"NoObjects", new[]{"No objects selected", "Объекты не выбраны"}},
        {"SnapButton", new[]{"Snap To Terrain", "Snap To Terrain"}},
        {"WarningNoTerrainOrObjects", new[]{"Terrain or objects not selected.", "Terrain или объекты не выбраны."}},
        {"WarningNoMesh", new[]{"Object {0} and its children have no MeshFilter with mesh. Offset not calculated.", "У объекта {0} и его детей нет MeshFilter с mesh. Offset не вычислен."}},
        {"MenuTitle", new[]{"Snap To Terrain", "Snap To Terrain"}},
        {"Language", new[]{"Language:", "Язык:"}},
        // Удаляем переводы для English и Russian
        //{"English", new[]{"English", "Английский"}},
        //{"Russian", new[]{"Russian", "Русский"}},
    };

    private string T(string key)
    {
        int idx = currentLanguage == Language.English ? 0 : 1;
        if (translations.TryGetValue(key, out var arr))
            return arr[idx];
        return key;
    }

    private void OnGUI()
    {
        // Language switch
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(T("Language"), GUILayout.Width(80));
        if (GUILayout.Toggle(currentLanguage == Language.English, "English", EditorStyles.miniButtonLeft))
            currentLanguage = Language.English;
        if (GUILayout.Toggle(currentLanguage == Language.Russian, "Русский", EditorStyles.miniButtonRight))
            currentLanguage = Language.Russian;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(T("SelectTerrain"), EditorStyles.boldLabel);
        selectedTerrain = (Terrain)EditorGUILayout.ObjectField("Terrain", selectedTerrain, typeof(Terrain), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(T("SelectMode"), EditorStyles.boldLabel);
        // Локализуем EnumPopup
        PivotMode[] modes = { PivotMode.Edge, PivotMode.Center };
        string[] modeLabels = { T("Edge"), T("Center") };
        int modeIdx = (int)pivotMode;
        modeIdx = GUILayout.Toolbar(modeIdx, modeLabels);
        pivotMode = modes[modeIdx];

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(T("SelectObjects"), EditorStyles.boldLabel);
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects != null && selectedObjects.Length > 0)
        {
            foreach (var obj in selectedObjects)
            {
                EditorGUILayout.LabelField(T("SelectedObject"), obj.name);
            }
        }
        else
        {
            EditorGUILayout.LabelField(T("NoObjects"));
        }

        EditorGUILayout.Space();
        GUI.enabled = selectedTerrain != null && selectedObjects != null && selectedObjects.Length > 0;
        if (GUILayout.Button(T("SnapButton")))
        {
            SnapObjectsToTerrain(selectedObjects);
        }
        GUI.enabled = true;
    }

    private void SnapObjectsToTerrain(GameObject[] objects)
    {
        if (selectedTerrain == null || objects == null || objects.Length == 0)
        {
            Debug.LogWarning(T("WarningNoTerrainOrObjects"));
            return;
        }
        foreach (var obj in objects)
        {
            SnapObjectToTerrain(obj);
        }
    }

    private void SnapObjectToTerrain(GameObject obj)
    {
        if (selectedTerrain == null || obj == null)
        {
            Debug.LogWarning(T("WarningNoTerrainOrObjects"));
            return;
        }

        Vector3 objPos = obj.transform.position;
        Vector3 terrainPos = selectedTerrain.transform.position;
        TerrainData tData = selectedTerrain.terrainData;

        float relativeX = (objPos.x - terrainPos.x) / tData.size.x;
        float relativeZ = (objPos.z - terrainPos.z) / tData.size.z;

        float height = tData.GetInterpolatedHeight(relativeX, relativeZ) + terrainPos.y;

        float yOffset = 0f;
        if (pivotMode == PivotMode.Center)
        {
            float minY = float.MaxValue;
            bool foundMesh = false;
            foreach (var meshFilter in obj.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;
                foreach (var v in mesh.vertices)
                {
                    Vector3 worldV = meshFilter.transform.TransformPoint(v);
                    if (worldV.y < minY) minY = worldV.y;
                    foundMesh = true;
                }
            }
            if (foundMesh)
            {
                yOffset = objPos.y - minY;
            }
            else
            {
                Debug.LogWarning(string.Format(T("WarningNoMesh"), obj.name));
            }
        }
        // В режиме Edge yOffset = 0

        Undo.RecordObject(obj.transform, "Snap To Terrain");
        obj.transform.position = new Vector3(objPos.x, height + yOffset, objPos.z);
    }
}
