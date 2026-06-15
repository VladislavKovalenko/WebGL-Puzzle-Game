using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace EditorTools
{
    /// <summary>
    /// Окно проверки соответствия якорей визуальным границам RectTransform (Anchors to Corners).
    /// </summary>
    public class AnchorsToCornersChecker : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _isSearching;
        private bool _searchCompleted;
        private List<UIIssue> _results = new List<UIIssue>();

        private bool _skipTextAndTMP = true;
        private bool _checkPointAnchors = false;
        private bool _showDisabledAndHidden = false;

        private const float Epsilon = 0.001f;

        private class UIIssue
        {
            public GameObject Target;
            public string Path;
            public string Description;
        }

        [MenuItem("Tools/Megxlord uGUI/Anchors to Corners Checker")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnchorsToCornersChecker>("Anchors to Corners Checker");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Проверка: якоря соответствуют визуальным границам RectTransform", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Проверяет объекты со Stretch-якорями: anchorMin/anchorMax должны точно совпадать с углами объекта относительно родителя, а offset'ы быть равны нулю.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Фильтры", EditorStyles.miniBoldLabel);

            _skipTextAndTMP = EditorGUILayout.ToggleLeft(
                new GUIContent("Пропускать Text и TMP", "Исключить из проверки объекты, содержащие компоненты Text, TMP_Text или TextMeshProUGUI. Текстовые элементы часто используют авто-размер и им не критичны точные якоря."),
                _skipTextAndTMP);

            _checkPointAnchors = EditorGUILayout.ToggleLeft(
                new GUIContent("Проверять точечные якоря", "Включить в проверку объекты, у которых anchorMin и anchorMax совпадают (точечная привязка). Такие объекты можно исправить, превратив в Stretch."),
                _checkPointAnchors);

            _showDisabledAndHidden = EditorGUILayout.ToggleLeft(
                new GUIContent("Показывать выключенные и скрытые в сцене объекты", "Если выключено — объекты с activeInHierarchy == false или hideFlags != None пропускаются."),
                _showDisabledAndHidden);

            EditorGUILayout.Space(10);

            GUI.enabled = !_isSearching;
            if (GUILayout.Button(_isSearching ? "Поиск..." : "Проверить всю сцену", GUILayout.Height(36)))
                SearchScene();
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
                        EditorGUILayout.LabelField(issue.Description, new GUIStyle(EditorStyles.label)
                        {
                            wordWrap = true,
                            richText = true,
                            fontSize = 11
                        });

                        EditorGUILayout.EndVertical();

                        if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(48)))
                        {
                            Selection.activeGameObject = issue.Target;
                            EditorGUIUtility.PingObject(issue.Target);
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space(6);

                    bool hasSelection = Selection.activeGameObject != null &&
                                          Selection.activeGameObject.GetComponent<RectTransform>() != null;

                    GUI.enabled = hasSelection && !_isSearching;
                    if (GUILayout.Button("Исправить выделенный", GUILayout.Height(32)))
                        FixSelected();
                    GUI.enabled = true;

                    if (!hasSelection)
                        EditorGUILayout.HelpBox("Выделите объект в сцене или Hierarchy, затем нажмите «Исправить выделенный».", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Проблем не найдено. Все Stretch-якоря настроены корректно.", MessageType.Info);
                }
            }
            else if (!_isSearching)
            {
                EditorGUILayout.HelpBox("Нажмите «Проверить всю сцену» для анализа.", MessageType.Info);
            }
        }

        private void SearchScene()
        {
            _isSearching = true;
            _results.Clear();
            _searchCompleted = false;

            var sceneRects = FindObjectsOfType<RectTransform>(true);
            foreach (var rt in sceneRects)
            {
                if (ShouldSkip(rt))
                    continue;

                if (TryGetIssue(rt, out var issue))
                    _results.Add(issue);
            }

            _isSearching = false;
            _searchCompleted = true;
            Repaint();
        }

        private bool ShouldSkip(RectTransform rt)
        {
            var go = rt.gameObject;

            if (!_showDisabledAndHidden)
            {
                if (!go.activeInHierarchy)
                    return true;

                if (go.hideFlags != HideFlags.None || rt.hideFlags != HideFlags.None)
                    return true;
            }

            if (!_skipTextAndTMP)
                return false;

            if (go.GetComponent<UnityEngine.UI.Text>() != null)
                return true;

#if UNITY_2018_1_OR_NEWER
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", false);
            if (tmpType != null && go.GetComponent(tmpType) != null)
                return true;
#endif

            var tmpUGUIType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", false);
            if (tmpUGUIType != null && go.GetComponent(tmpUGUIType) != null)
                return true;

            return false;
        }

        private bool TryGetIssue(RectTransform rt, out UIIssue issue)
        {
            issue = null;
            var parent = rt.parent as RectTransform;
            if (parent == null)
                return false;

            bool isStretched = Mathf.Abs(rt.anchorMin.x - rt.anchorMax.x) > Epsilon ||
                               Mathf.Abs(rt.anchorMin.y - rt.anchorMax.y) > Epsilon;

            if (!isStretched)
            {
                if (!_checkPointAnchors)
                    return false;

                issue = new UIIssue
                {
                    Target = rt.gameObject,
                    Path = GetHierarchyPath(rt),
                    Description = "Точечный якорь: anchorMin и anchorMax совпадают. Рекомендуется привязать к углам (Stretch)."
                };
                return true;
            }

            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            Vector2 min = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 max = parent.InverseTransformPoint(worldCorners[2]);
            Vector2 parentSize = parent.rect.size;

            if (parentSize.x <= Epsilon || parentSize.y <= Epsilon)
                return false;

            Vector2 idealMin = new Vector2(
                min.x / parentSize.x + parent.pivot.x,
                min.y / parentSize.y + parent.pivot.y
            );
            Vector2 idealMax = new Vector2(
                max.x / parentSize.x + parent.pivot.x,
                max.y / parentSize.y + parent.pivot.y
            );

            bool minMatch = Mathf.Abs(rt.anchorMin.x - idealMin.x) < Epsilon && Mathf.Abs(rt.anchorMin.y - idealMin.y) < Epsilon;
            bool maxMatch = Mathf.Abs(rt.anchorMax.x - idealMax.x) < Epsilon && Mathf.Abs(rt.anchorMax.y - idealMax.y) < Epsilon;
            bool offsetMatch = Mathf.Abs(rt.offsetMin.x) < Epsilon && Mathf.Abs(rt.offsetMin.y) < Epsilon &&
                               Mathf.Abs(rt.offsetMax.x) < Epsilon && Mathf.Abs(rt.offsetMax.y) < Epsilon;

            if (minMatch && maxMatch && offsetMatch)
                return false;

            var sb = new StringBuilder();
            if (!minMatch) sb.AppendLine($"• anchorMin: текущий {rt.anchorMin:F4} ≠ идеальный {idealMin:F4}");
            if (!maxMatch) sb.AppendLine($"• anchorMax: текущий {rt.anchorMax:F4} ≠ идеальный {idealMax:F4}");
            if (!offsetMatch)
            {
                if (rt.offsetMin != Vector2.zero) sb.AppendLine($"• offsetMin: {rt.offsetMin:F4} (ожидается 0,0)");
                if (rt.offsetMax != Vector2.zero) sb.AppendLine($"• offsetMax: {rt.offsetMax:F4} (ожидается 0,0)");
            }

            issue = new UIIssue
            {
                Target = rt.gameObject,
                Path = GetHierarchyPath(rt),
                Description = sb.ToString().TrimEnd()
            };

            return true;
        }

        private void FixSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return;

            var rt = selected.GetComponent<RectTransform>();
            if (rt == null) return;

            var parent = rt.parent as RectTransform;
            if (parent == null) return;

            Undo.RecordObject(rt, "Fix Anchors to Corners");

            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            Vector2 min = parent.InverseTransformPoint(worldCorners[0]);
            Vector2 max = parent.InverseTransformPoint(worldCorners[2]);
            Vector2 parentSize = parent.rect.size;

            if (parentSize.x > Epsilon && parentSize.y > Epsilon)
            {
                rt.anchorMin = new Vector2(
                    min.x / parentSize.x + parent.pivot.x,
                    min.y / parentSize.y + parent.pivot.y
                );
                rt.anchorMax = new Vector2(
                    max.x / parentSize.x + parent.pivot.x,
                    max.y / parentSize.y + parent.pivot.y
                );
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rt);
            Debug.Log($"[AnchorsToCornersChecker] Исправлен: {selected.name}");

            SearchScene();
        }

        private static string GetHierarchyPath(RectTransform rt)
        {
            var sb = new StringBuilder(rt.name);
            Transform current = rt.parent;
            while (current != null)
            {
                sb.Insert(0, current.name + "/");
                current = current.parent;
            }
            return sb.ToString();
        }
    }
}