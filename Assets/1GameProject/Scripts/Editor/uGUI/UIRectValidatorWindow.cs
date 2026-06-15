using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Окно валидации RectTransform: поиск «грязных» значений
/// (дробные, нечётные, нестандартный scale/rotation) на сцене.
/// </summary>
public class UIRectValidatorWindow : EditorWindow
{
    // --- Settings ---
    private bool _checkMultipleOfTwo;
    private bool _checkAnchoredPosition = true;
    private bool _checkOffset = true;
    private bool _checkSizeDelta = true;
    private bool _checkWidthHeight;
    private bool _checkScale = true;
    private bool _checkRotation = true;

    // --- State ---
    private Vector2 _scrollPosition;
    private bool _isSearching;
    private bool _searchCompleted;

    // --- Results ---
    private List<UIIssue> _results = new List<UIIssue>();

    private const float Epsilon = 0.001f;

    private class UIIssue
    {
        public Object Target;
        public string Path;
        public List<string> Fields = new List<string>();
    }

    [MenuItem("Tools/Megxlord uGUI/UI Rect Validator")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIRectValidatorWindow>("UI Rect Validator");
        window.minSize = new Vector2(450, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Проверка RectTransform на «чистоту» значений", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Позиция и размер", EditorStyles.miniBoldLabel);
        _checkAnchoredPosition = EditorGUILayout.ToggleLeft(
            new GUIContent("Anchored Position", "Проверяет anchoredPosition.x/y на дробные части и нечётные целые значения."),
            _checkAnchoredPosition);

        _checkOffset = EditorGUILayout.ToggleLeft(
            new GUIContent("Offset", "Проверяет offsetMin и offsetMax на дробные части и нечётные целые значения."),
            _checkOffset);

        _checkSizeDelta = EditorGUILayout.ToggleLeft(
            new GUIContent("SizeDelta", "Разница между размером элемента и расстоянием между якорями. " +
                "При якорях, прижатых к одной стороне (например, Left-Left), SizeDelta.x равен ширине элемента. " +
                "При растянутых якорях (Stretch) SizeDelta.x — это дополнительное смещение относительно якорей, " +
                "а не фактическая ширина. Проверяет sizeDelta.x/y на дробные части и нечётные целые значения."),
            _checkSizeDelta);

        _checkWidthHeight = EditorGUILayout.ToggleLeft(
            new GUIContent("Width / Height", "Проверяет фактические размеры элемента (rect.width / rect.height) на дробные части и нечётные целые значения."),
            _checkWidthHeight);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Трансформация", EditorStyles.miniBoldLabel);
        _checkScale = EditorGUILayout.ToggleLeft(
            new GUIContent("Scale", "Проверяет, что localScale строго равен (1, 1, 1)."),
            _checkScale);

        _checkRotation = EditorGUILayout.ToggleLeft(
            new GUIContent("Rotation", "Проверяет, что localEulerAngles строго равен (0, 0, 0)."),
            _checkRotation);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Дополнительно", EditorStyles.miniBoldLabel);
        _checkMultipleOfTwo = EditorGUILayout.ToggleLeft(
            new GUIContent("Проверять объекты кратные 2 (ловить нечётные)", "Дополнительно подсвечивает целые нечётные числа (например, 1, 3, 5) во всех включённых проверках размеров и позиций."),
            _checkMultipleOfTwo);

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
                    EditorGUILayout.BeginVertical(GUI.skin.box);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(issue.Target.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(issue.Path, EditorStyles.miniLabel);

                    var sb = new StringBuilder();
                    foreach (var field in issue.Fields)
                        sb.AppendLine("• " + field);

                    EditorGUILayout.LabelField(sb.ToString(), new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true });
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        Selection.activeObject = issue.Target;
                        EditorGUIUtility.PingObject(issue.Target);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Проблем не найдено. Все RectTransform «чистые».", MessageType.Info);
            }
        }
        else if (!_isSearching)
        {
            EditorGUILayout.HelpBox("Нажмите «Начать проверку» для анализа сцены.", MessageType.Info);
        }
    }

    /// <summary>
    /// Запускает анализ всех RectTransform на сцене.
    /// </summary>
    private void Search()
    {
        _isSearching = true;
        _results.Clear();
        _searchCompleted = false;

        var sceneRects = FindObjectsOfType<RectTransform>(true);
        foreach (var rt in sceneRects)
        {
            if (TryCollectIssues(rt, out var fields))
            {
                _results.Add(new UIIssue
                {
                    Target = rt.gameObject,
                    Path = GetHierarchyPath(rt),
                    Fields = fields
                });
            }
        }

        _isSearching = false;
        _searchCompleted = true;
        Repaint();
    }

    /// <summary>
    /// Собирает список проблем для конкретного RectTransform согласно включённым галочкам.
    /// </summary>
    private bool TryCollectIssues(RectTransform rt, out List<string> fields)
    {
        fields = new List<string>();

        if (_checkSizeDelta)
            CheckVector2(rt.sizeDelta, "SizeDelta", fields);

        if (_checkAnchoredPosition)
            CheckVector2(rt.anchoredPosition, "AnchoredPosition", fields);

        if (_checkOffset)
        {
            CheckVector2(rt.offsetMin, "OffsetMin", fields);
            CheckVector2(rt.offsetMax, "OffsetMax", fields);
        }

        if (_checkWidthHeight)
        {
            var rect = rt.rect;
            CheckFloat(rect.width, "Width", fields);
            CheckFloat(rect.height, "Height", fields);
        }

        if (_checkScale)
        {
            var s = rt.localScale;
            if (Mathf.Abs(s.x - 1f) > Epsilon || Mathf.Abs(s.y - 1f) > Epsilon || Mathf.Abs(s.z - 1f) > Epsilon)
            {
                fields.Add($"<color=#4A90FF>Scale.x = {s.x}</color>  (не 1)");
                fields.Add($"<color=#4A90FF>Scale.y = {s.y}</color>  (не 1)");
                fields.Add($"<color=#4A90FF>Scale.z = {s.z}</color>  (не 1)");
            }
        }

        if (_checkRotation)
        {
            var r = rt.localEulerAngles;
            if (Mathf.Abs(r.x) > Epsilon || Mathf.Abs(r.y) > Epsilon || Mathf.Abs(r.z) > Epsilon)
                fields.Add($"<color=#4A90FF>Rotation ({r.x:F2}, {r.y:F2}, {r.z:F2}) — ожидается (0, 0, 0)</color>");
        }

        return fields.Count > 0;
    }

    private void CheckVector2(Vector2 v, string name, List<string> fields)
    {
        CheckFloat(v.x, $"{name}.x", fields);
        CheckFloat(v.y, $"{name}.y", fields);
    }

    /// <summary>
    /// Проверяет float на дробную часть и, при включённой опции, на нечётность.
    /// </summary>
    private void CheckFloat(float value, string label, List<string> fields)
    {
        float abs = Mathf.Abs(value);
        float frac = abs % 1f;

        bool isFractional = (frac > Epsilon && frac < 1f - Epsilon);
        bool isBadMultiple = false;

        if (_checkMultipleOfTwo && !isFractional)
        {
            int rounded = Mathf.RoundToInt(value);
            if (Mathf.Abs(value - rounded) < Epsilon && (rounded % 2) != 0)
                isBadMultiple = true;
        }

        if (isFractional)
            fields.Add($"<color=#FF6B6B>{label} = {value}</color>  (дробное)");
        else if (isBadMultiple)
            fields.Add($"<color=#FFD93D>{label} = {value}</color>  (нечётное)");
    }

    /// <summary>
    /// Формирует путь в иерархии от корня до указанного RectTransform.
    /// </summary>
    private string GetHierarchyPath(RectTransform rt, Transform stopAt = null)
    {
        var sb = new StringBuilder(rt.name);
        Transform current = rt.parent;

        while (current != null && current != stopAt)
        {
            sb.Insert(0, current.name + "/");
            current = current.parent;
        }

        return sb.ToString();
    }
}