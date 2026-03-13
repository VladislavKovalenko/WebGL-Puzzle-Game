using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public enum NodeType { Manager, Data, Mixed }
public enum LayoutMode { RowLeftToRight, RowTopToBottom }

public class ScriptDependencySelectorWindow : EditorWindow
{
    private DefaultAsset selectedFolder;
    private List<string> namespaces = new();
    private string selectedNamespace = "";
    private int selectedIndex = 0;
    private bool ignoreNamespace = false;

    [MenuItem("Tools/Построить граф зависимостей папки скриптов")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptDependencySelectorWindow>();
        window.titleContent = new GUIContent("Script Dependency Selector");
    }

    private void OnGUI()
    {
        GUILayout.Label("Выберите папку с пользовательскими скриптами", EditorStyles.boldLabel);
        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Папка", selectedFolder, typeof(DefaultAsset), false);
        ignoreNamespace = EditorGUILayout.Toggle("Игнорировать namespace", ignoreNamespace);

        if (selectedFolder != null && !ignoreNamespace)
        {
            string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                namespaces = ScriptDependencyAnalyzer.ExtractNamespacesFromFolder(folderPath);
                if (namespaces.Count == 0) namespaces.Add("(нет namespace)");
                selectedIndex = EditorGUILayout.Popup("Namespace", selectedIndex, namespaces.ToArray());
                selectedNamespace = namespaces[selectedIndex];
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("🗂️ Цветовая легенда узлов графа:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("🟥 Manager — создаёт другие классы\n⬛ Data — используется другими\n🟦 Mixed — и создаёт, и используется", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Построить граф", GUILayout.Height(30)))
        {
            if (selectedFolder == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Выберите папку в Project.", "OK");
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("Ошибка", "Выбранный объект не является папкой.", "OK");
                return;
            }

            ScriptDependencyGraphViewWindow.OpenWithFolder(folderPath, ignoreNamespace ? null : selectedNamespace);
        }
    }
}

public class ScriptDependencyGraphViewWindow : EditorWindow
{
    private ScriptDependencyGraphView graphView;
    private string folderPath;
    private string namespaceFilter;
    private bool initialized = false;
    private LayoutMode layoutMode = LayoutMode.RowLeftToRight;

    public static void OpenWithFolder(string path, string nsFilter)
    {
        var window = CreateInstance<ScriptDependencyGraphViewWindow>();
        window.folderPath = path;
        window.namespaceFilter = nsFilter;
        window.titleContent = new GUIContent("Script Dependency Graph");
        window.Show();
    }

    private void OnEnable()
    {
        graphView = new ScriptDependencyGraphView();
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);

        var exportButton = new Button(() =>
        {
            string filePath = "Assets/ScriptDependencyGraph.png";
            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log("Граф сохранён: " + filePath);
        }) { text = "Экспорт в PNG" };

        var sortLTRButton = new Button(() =>
        {
            layoutMode = LayoutMode.RowLeftToRight;
            graphView.PopulateFromFolder(folderPath, namespaceFilter, layoutMode);
        }) { text = "Сортировка: сверху-вниз" };

        var sortTTBButton = new Button(() =>
        {
            layoutMode = LayoutMode.RowTopToBottom;
            graphView.PopulateFromFolder(folderPath, namespaceFilter, layoutMode);
        }) { text = "Сортировка: слева-направо" };

        rootVisualElement.Add(exportButton);
        rootVisualElement.Add(sortLTRButton);
        rootVisualElement.Add(sortTTBButton);
    }

    private void OnGUI()
    {
        if (!initialized && !string.IsNullOrEmpty(folderPath))
        {
            graphView.PopulateFromFolder(folderPath, namespaceFilter, layoutMode);
            initialized = true;
        }
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }
}

public class ScriptDependencyGraphView : GraphView
{
    private Dictionary<string, ScriptNodeData> nodes = new();

    public ScriptDependencyGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public void PopulateFromFolder(string folderPath, string namespaceFilter, LayoutMode layoutMode)
{
    DeleteElements(graphElements.ToList());
    nodes = ScriptDependencyAnalyzer.AnalyzeFolder(folderPath, namespaceFilter);

    Dictionary<string, Node> visualNodes = new();

    var managers = nodes.Where(n => n.Value.Type == NodeType.Manager).ToList();
    var mixed = nodes.Where(n => n.Value.Type == NodeType.Mixed).ToList();
    var data = nodes.Where(n => n.Value.Type == NodeType.Data).ToList();

    // Ряд 1: Managers с Output only
    var managerLevel1 = managers.Where(m => m.Value.UsedBy.Count == 0 && m.Value.DependsOn.Count > 0).ToList();
    var managerLevel1Keys = managerLevel1.Select(m => m.Key).ToHashSet();

    // Ряд 2: Managers, которые вызываются только узлами из ряда 1
    var managerLevel2 = managers.Where(m =>
        !managerLevel1Keys.Contains(m.Key) &&
        m.Value.UsedBy.All(u => managerLevel1Keys.Contains(u)) &&
        m.Value.DependsOn.Count > 0
    ).ToList();
    var managerLevel2Keys = managerLevel2.Select(m => m.Key).ToHashSet();

    // Ряд 3: Остальные менеджеры
    var managerOther = managers.Where(m =>
        !managerLevel1Keys.Contains(m.Key) &&
        !managerLevel2Keys.Contains(m.Key)
    ).ToList();

    // Ряд 4: Mixed, которых вызывают менеджеры из ряда 1
    var mixedLevel2 = mixed.Where(m =>
        m.Value.UsedBy.Any(u => managerLevel1Keys.Contains(u))
    ).ToList();
    var mixedLevel2Keys = mixedLevel2.Select(m => m.Key).ToHashSet();

    // Ряд 5: Остальные mixed
    var mixedOther = mixed.Where(m => !mixedLevel2Keys.Contains(m.Key)).ToList();

    // Метод размещения
    void PlaceNodes(List<KeyValuePair<string, ScriptNodeData>> nodeList, float rowOrColumn)
    {
        float spacing = 350f;
        int index = 0;

        foreach (var kvp in nodeList)
        {
            var node = new Node
            {
                title = kvp.Key,
                userData = kvp.Key
            };

            Rect position = layoutMode switch
            {
                LayoutMode.RowLeftToRight => new Rect(50f + index * spacing, rowOrColumn, 300, 120),
                LayoutMode.RowTopToBottom => new Rect(rowOrColumn, 50f + index * spacing, 300, 120),
                _ => new Rect(50f + index * spacing, rowOrColumn, 300, 120)
            };

            node.SetPosition(position);
            index++;

            node.Add(new Label($"→ Использует: {kvp.Value.DependsOn.Count}"));
            node.Add(new Label($"← Используется: {kvp.Value.UsedBy.Count}"));

            foreach (var dep in kvp.Value.DependsOn)
            {
                if (kvp.Value.AccessMethods.TryGetValue(dep, out var methods))
                {
                    node.Add(new Label($"→ {dep} [{string.Join(", ", methods.Distinct())}]"));
                }
            }

            switch (kvp.Value.Type)
            {
                case NodeType.Manager: node.style.backgroundColor = new Color(0.6f, 0.3f, 0.3f); break;
                case NodeType.Data: node.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f); break;
                case NodeType.Mixed: node.style.backgroundColor = new Color(0.2f, 0.2f, 0.4f); break;
            }

            string path = ScriptDependencyAnalyzer.FindScriptPathByClass(kvp.Key);
            var script = !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<MonoScript>(path) : null;

            node.Add(new Button(() => { if (script != null) AssetDatabase.OpenAsset(script); }) { text = "Открыть" });
            node.Add(new Button(() => { if (script != null) EditorGUIUtility.PingObject(script); }) { text = "Project" });

            AddElement(node);
            visualNodes[kvp.Key] = node;
        }
    }

    // Размещение по 6 уровням
    float rowHeight = 400f;
    PlaceNodes(managerLevel1, 0 * rowHeight);
    PlaceNodes(managerLevel2, 1 * rowHeight);
    PlaceNodes(managerOther, 2 * rowHeight);
    PlaceNodes(mixedLevel2, 3 * rowHeight);
    PlaceNodes(mixedOther, 4 * rowHeight);
    PlaceNodes(data, 5 * rowHeight);

    // Связи между узлами
    foreach (var kvp in nodes)
    {
        foreach (var dep in kvp.Value.DependsOn)
        {
            if (visualNodes.ContainsKey(kvp.Key) && visualNodes.ContainsKey(dep))
            {
                var fromNode = visualNodes[kvp.Key];
                var toNode = visualNodes[dep];

                var outputPort = fromNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                var inputPort = toNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));

                fromNode.outputContainer.Add(outputPort);
                toNode.inputContainer.Add(inputPort);

                fromNode.RefreshPorts();
                toNode.RefreshPorts();

                var edge = new Edge { output = outputPort, input = inputPort };
                outputPort.Connect(edge);
                inputPort.Connect(edge);
                AddElement(edge);
            }
        }
    }
}




}

public class ScriptNodeData
{
    public string ClassName;
    public List<string> DependsOn = new();
    public List<string> UsedBy = new();
    public string SourceCode;
    public NodeType Type;
    public Dictionary<string, List<string>> AccessMethods = new();
}

public static class ScriptDependencyAnalyzer
{
    public static Dictionary<string, ScriptNodeData> AnalyzeFolder(string folderPath, string namespaceFilter)
    {
        var result = new Dictionary<string, ScriptNodeData>();
        var allScripts = LoadUserScriptsInFolder(folderPath, namespaceFilter);

        foreach (var kvp in allScripts)
        {
            result[kvp.Key] = new ScriptNodeData
            {
                ClassName = kvp.Key,
                SourceCode = kvp.Value
            };
        }

        foreach (var node in result.Values)
        {
            string code = StripCommentsAndStrings(node.SourceCode);
            foreach (var other in result.Values)
            {
                if (node.ClassName == other.ClassName) continue;

                var methods = GetReferenceMethods(code, other.ClassName);
                if (methods.Count > 0)
                {
                    node.DependsOn.Add(other.ClassName);
                    other.UsedBy.Add(node.ClassName);

                    if (!node.AccessMethods.ContainsKey(other.ClassName))
                        node.AccessMethods[other.ClassName] = new List<string>();

                    node.AccessMethods[other.ClassName].AddRange(methods);
                }
            }

            var type = FindTypeByClassName(node.ClassName);
            if (type != null)
            {
                var baseType = type.BaseType?.Name;
                if (!string.IsNullOrEmpty(baseType) && result.ContainsKey(baseType))
                    node.DependsOn.Add(baseType);

                foreach (var iface in type.GetInterfaces())
                    if (result.ContainsKey(iface.Name))
                        node.DependsOn.Add(iface.Name);
            }

            bool createsOthers = result.Values.Any(o => node.AccessMethods.ContainsKey(o.ClassName));
            bool usedByOthers = node.UsedBy.Count > 0;

            node.Type = createsOthers && usedByOthers ? NodeType.Mixed :
                        createsOthers ? NodeType.Manager :
                        usedByOthers ? NodeType.Data : NodeType.Data;
        }

        return result;
    }

    public static List<string> ExtractNamespacesFromFolder(string folderPath)
    {
        var result = new HashSet<string>();
        string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        foreach (string path in files)
        {
            if (path.Contains("/Editor/") || path.Contains("\\Editor\\")) continue;

            string assetPath = path.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            Type type = script?.GetClass();
            if (type == null) continue;

            string ns = type.Namespace ?? "";
            if (ns.StartsWith("Unity") || ns.StartsWith("System") || ns.StartsWith("TMPro") || ns.StartsWith("TextMeshPro")) continue;

            result.Add(string.IsNullOrEmpty(ns) ? "(нет namespace)" : ns);
        }

        return result.OrderBy(n => n).ToList();
    }

    public static Dictionary<string, string> LoadUserScriptsInFolder(string folderPath, string namespaceFilter)
    {
        var result = new Dictionary<string, string>();
        string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        foreach (string path in files)
        {
            if (path.Contains("/Editor/") || path.Contains("\\Editor\\")) continue;

            string assetPath = path.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            Type type = script?.GetClass();
            if (type == null) continue;

            string ns = type.Namespace ?? "";
            if (ns.StartsWith("Unity") || ns.StartsWith("System") || ns.StartsWith("TMPro") || ns.StartsWith("TextMeshPro")) continue;

            if (namespaceFilter != null)
            {
                if (namespaceFilter != "(нет namespace)" && ns != namespaceFilter) continue;
                if (namespaceFilter == "(нет namespace)" && !string.IsNullOrEmpty(ns)) continue;
            }

            string className = type.Name;
            if (!string.IsNullOrEmpty(className))
            {
                result[className] = script.text;
            }
        }

        return result;
    }

    public static string FindScriptPathByClass(string className)
    {
        string[] guids = AssetDatabase.FindAssets("t:Script");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;

            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script?.GetClass()?.Name == className)
                return path;
        }
        return null;
    }

    public static Type FindTypeByClassName(string className)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == className)
                    return type;
            }
        }
        return null;
    }

    private static string StripCommentsAndStrings(string code)
    {
        string noStrings = Regex.Replace(code, "\".*?\"", "");
        string noComments = Regex.Replace(noStrings, @"//.*?$|/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
        return noComments;
    }

    public static List<string> GetReferenceMethods(string code, string className)
    {
        var methods = new List<string>();

        string genericPattern = $@"\b(GetComponent|GetOrAddComponent|FindObjectOfType|Instantiate)<\s*{Regex.Escape(className)}\s*>";
        string constructorPattern = $@"new\s+{Regex.Escape(className)}\s*\(";
        string methodCallPattern = $@"\b{Regex.Escape(className)}\s*\.\s*\w+\s*\(";
        string variableDeclaration = $@"\b{Regex.Escape(className)}\s+[a-zA-Z_][a-zA-Z0-9_]*\s*(=|;)";

        if (Regex.IsMatch(code, genericPattern)) methods.Add("component");
        if (Regex.IsMatch(code, constructorPattern)) methods.Add("constructor");
        if (Regex.IsMatch(code, methodCallPattern)) methods.Add("method");
        if (Regex.IsMatch(code, variableDeclaration)) methods.Add("variable");

        return methods;
    }
}
