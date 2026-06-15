using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Окно поиска Missing Scripts на сцене.
/// Ищет все GameObject'ы, на которых есть компоненты со ссылкой на потерянный скрипт.
/// </summary>
public class MissingScriptValidatorWindow : EditorWindow
{
    // --- State ---
    private Vector2 _scrollPosition;
    private bool _isSearching;
    private bool _searchCompleted;

    // --- Results ---
    private List<MissingScriptIssue> _results = new List<MissingScriptIssue>();

    private class MissingScriptIssue
    {
        public GameObject Target;
        public string Path;
        public int MissingCount;
    }

    [MenuItem("Tools/Megxlord Toolbox/Hierarchy/Missing Script Validator", priority = 200)]
    public static void ShowWindow()
    {
        var window = GetWindow<MissingScriptValidatorWindow>("Missing Script Validator");
        window.minSize = new Vector2(450, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Поиск потерянных скриптов (Missing Scripts)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "Ищет все GameObject'ы на сцене, у которых есть компоненты " +
            "со ссылкой на несуществующий скрипт (Missing Script).",
            MessageType.Info);

        EditorGUILayout.Space(10);

        GUI.enabled = !_isSearching;
        if (GUILayout.Button(_isSearching ? "Поиск..." : "Начать проверку", GUILayout.Height(32)))
            Search();
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        if (_searchCompleted)
        {
            if (_results.Count > 0)
            {
                EditorGUILayout.LabelField($"Найдено проблем: {_results.Count}", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

                foreach (var issue in _results)
                {
                    // === ЗАЩИТА: объект мог быть удалён ===
                    if (issue.Target == null)
                        continue;

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.LabelField(issue.Target.name, EditorStyles.boldLabel);
                                EditorGUILayout.LabelField(issue.Path, EditorStyles.miniLabel);

                                string countText = issue.MissingCount == 1
                                    ? "• <color=#FF6B6B>1 потерянный скрипт</color>"
                                    : $"• <color=#FF6B6B>{issue.MissingCount} потерянных скрипта</color>";

                                EditorGUILayout.LabelField(countText, new GUIStyle(EditorStyles.label)
                                {
                                    wordWrap = true,
                                    richText = true
                                });
                            }
                            EditorGUILayout.EndVertical();

                            if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(40)))
                            {
                                // === ЗАЩИТА: объект мог быть удалён между кадрами ===
                                if (issue.Target != null)
                                {
                                    Selection.activeGameObject = issue.Target;
                                    EditorGUIUtility.PingObject(issue.Target);
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Проблем не найдено. На сцене нет потерянных скриптов.", MessageType.Info);
            }
        }
        else if (!_isSearching)
        {
            EditorGUILayout.HelpBox("Нажмите «Начать проверку» для анализа сцены.", MessageType.Info);
        }
    }

    /// <summary>
    /// Запускает анализ всех GameObject'ов на сцене.
    /// </summary>
    private void Search()
    {
        _isSearching = true;
        _results.Clear();
        _searchCompleted = false;

        // Unity 6: FindObjectsOfType устарел, используем FindObjectsByType
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var go in allGameObjects)
        {
            int missingCount = GetMissingScriptCount(go);
            if (missingCount > 0)
            {
                _results.Add(new MissingScriptIssue
                {
                    Target = go,
                    Path = GetHierarchyPath(go.transform),
                    MissingCount = missingCount
                });
            }
        }

        _isSearching = false;
        _searchCompleted = true;
        Repaint();
    }

    /// <summary>
    /// Подсчитывает количество компонентов со ссылкой на потерянный скрипт на GameObject.
    /// </summary>
    private int GetMissingScriptCount(GameObject go)
    {
        int count = 0;
        var components = go.GetComponents<Component>();

        foreach (var component in components)
        {
            if (component == null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Формирует путь в иерархии от корня до указанного Transform.
    /// </summary>
    private string GetHierarchyPath(Transform tr)
    {
        var sb = new StringBuilder(tr.name);
        Transform current = tr.parent;

        while (current != null)
        {
            sb.Insert(0, current.name + "/");
            current = current.parent;
        }

        return sb.ToString();
    }
}