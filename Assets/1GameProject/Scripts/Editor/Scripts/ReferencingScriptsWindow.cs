using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ReferencingScriptsWindow : EditorWindow
{
    private List<string> referencingScripts = new List<string>();
    private Vector2 scrollPosition = Vector2.zero;
    private string selectedScriptPath = "";
    private string className = "";

    [MenuItem("Tools/Megxlord Toolbox/Scripts/Найти ссылающиеся скрипты на этот")]
    public static void ShowWindow()
    {
        GetWindow<ReferencingScriptsWindow>("Referencing Scripts Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Поиск скриптов, ссылающихся на класс", EditorStyles.boldLabel);
        GUILayout.Space(10);

        className = EditorGUILayout.TextField("Имя класса:", className);

        if (GUILayout.Button("Найти ссылающиеся скрипты", GUILayout.Height(30)))
        {
            FindReferencingScripts();
        }

        GUILayout.Space(10);

        if (referencingScripts.Count > 0)
        {
            GUILayout.Label($"Найдено {referencingScripts.Count} скриптов, ссылающихся на '{className}':", EditorStyles.boldLabel);
            GUILayout.Space(5);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (string path in referencingScripts)
            {
                GUILayout.BeginHorizontal();
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                GUILayout.Label(script?.name ?? "Unknown", GUILayout.Width(200));

                if (GUILayout.Button("Открыть", GUILayout.Width(80)))
                {
                    AssetDatabase.OpenAsset(script);
                }

                if (GUILayout.Button("Project", GUILayout.Width(100)))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    EditorGUIUtility.PingObject(obj);  // EditorUtility.RevealInFinder(path)
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        else if (!string.IsNullOrEmpty(className))
        {
            GUILayout.Label("Ссылающиеся скрипты не найдены.", EditorStyles.helpBox);
        }
        else
        {
            GUILayout.Label("Введите имя класса или выберите скрипт в Project окне.", EditorStyles.helpBox);
        }

        GUILayout.Space(10);
        GUILayout.Label("Примечание: Поиск основан на текстовом анализе. Возможны ложные срабатывания.", EditorStyles.miniLabel);
    }

    private void FindReferencingScripts()
    {
        referencingScripts.Clear();

        UnityEngine.Object selected = Selection.activeObject;
        if (selected is MonoScript selectedScript)
        {
            Type selectedType = selectedScript.GetClass();
            if (selectedType != null)
            {
                string newClassName = selectedType.Name;
                string newScriptPath = AssetDatabase.GetAssetPath(selectedScript);

                // Обновляем только если объект изменился
                if (newScriptPath != selectedScriptPath || newClassName != className)
                {
                    className = newClassName;
                    selectedScriptPath = newScriptPath;
                }
            }
        }

        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("Ошибка", "Введите имя класса или выберите скрипт в Project окне.", "OK");
            return;
        }

        referencingScripts = ScriptReferenceFinder.FindReferences(className, selectedScriptPath);
        Repaint();
    }

    // 🔍 Вспомогательный класс для поиска ссылок на класс
    private static class ScriptReferenceFinder
    {
        public static List<string> FindReferences(string className, string excludePath)
        {
            List<string> results = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;
                if (path == excludePath) continue;

                MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (scriptAsset == null) continue;

                string code = StripCommentsAndStrings(scriptAsset.text);
                if (HasReferenceToClass(code, className))
                {
                    results.Add(path);
                }
            }

            return results;
        }

        private static string StripCommentsAndStrings(string code)
        {
            string noStrings = Regex.Replace(code, "\".*?\"", "");
            string noComments = Regex.Replace(noStrings, @"//.*?$|/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
            return noComments;
        }

        private static bool HasReferenceToClass(string code, string className)
        {
            if (!code.Contains(className)) return false;

            string pattern = @"(:\s*)" + Regex.Escape(className) + @"(\s*[,{])|" +
                             @"(new\s+" + Regex.Escape(className) + @")|" +
                             @"(\b" + Regex.Escape(className) + @"\s+[a-zA-Z_][a-zA-Z0-9_]*\s*(=|\;))|" +
                             @"(\b" + Regex.Escape(className) + @"\s*\([^)]*\))";

            string usingPattern = @"using\s+[A-Za-z0-9_.]*" + Regex.Escape(className) + @"[;]";
            if (Regex.IsMatch(code, usingPattern)) return false;

            return Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
    }
}
