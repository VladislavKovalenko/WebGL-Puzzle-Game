using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ScriptUsageFinder : EditorWindow
{
    private MonoScript _targetScript;
    private Vector2 _scrollPosition;
    private List<(GameObject go, string path)> _results = new List<(GameObject, string)>();
    private bool _isSearching;
    private bool _searchCompleted;

    [MenuItem("Tools/Megxlord Toolbox/Scripts/Find Script Usage")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptUsageFinder>("Script Finder");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Перетащите скрипт сюда или выделите в папке Assets:", EditorStyles.boldLabel);

        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, _targetScript != null ? _targetScript.name : "Drag & Drop Script Here",
            new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 14 });

        HandleDragAndDrop(dropArea);

        EditorGUILayout.Space(10);

        GUI.enabled = _targetScript != null && !_isSearching;
        if (GUILayout.Button(_isSearching ? "Поиск..." : "Поиск", GUILayout.Height(35)))
        {
            SearchScriptUsage();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // === РЕЗУЛЬТАТЫ ПОИСКА ===
        if (_searchCompleted)
        {
            if (_results.Count > 0)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

                foreach (var (go, path) in _results)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Объект: {go.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Путь: {path}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        Selection.activeGameObject = go;
                        EditorGUIUtility.PingObject(go);
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Результатов поиска нет", MessageType.Info);
            }
        }
        else if (!_isSearching)
        {
            EditorGUILayout.HelpBox("Нажмите 'Поиск' для поиска использования скрипта.", MessageType.Info);
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is MonoScript monoScript)
        {
            _targetScript = monoScript;
            _results.Clear();
            _searchCompleted = false;
            Repaint();
        }
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is MonoScript monoScript)
                        {
                            _targetScript = monoScript;
                            _results.Clear();
                            _searchCompleted = false;
                            evt.Use();
                            break;
                        }
                    }
                }
                evt.Use();
                break;
        }
    }

    private void SearchScriptUsage()
    {
        if (_targetScript == null) return;

        _isSearching = true;
        _results.Clear();
        _searchCompleted = false;

        System.Type scriptType = _targetScript.GetClass();
        if (scriptType == null)
        {
            Debug.LogError($"Не удалось получить тип класса из скрипта: {_targetScript.name}");
            _isSearching = false;
            _searchCompleted = true;
            Repaint();
            return;
        }

        // Поиск на текущей сцене
        var sceneObjects = FindObjectsOfType<GameObject>(true);
        foreach (var go in sceneObjects)
        {
            Component component = go.GetComponent(scriptType);
            if (component != null)
            {
                string path = GetGameObjectPath(go);
                _results.Add((go, path));
            }
        }

        // Поиск в префабах
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null) continue;

            var allChildren = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                Component component = child.GetComponent(scriptType);
                if (component != null)
                {
                    string path = $"[PREFAB] {assetPath} -> {GetGameObjectPath(child.gameObject, prefab)}";
                    _results.Add((child.gameObject, path));
                }
            }
        }

        _isSearching = false;
        _searchCompleted = true;
        Repaint();
    }

    private string GetGameObjectPath(GameObject obj, GameObject root = null)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null && current.gameObject != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}