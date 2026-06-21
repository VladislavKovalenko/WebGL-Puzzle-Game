#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using static VHierarchy.Libs.VUtils;

namespace VHierarchy
{
    public static class VHierarchyDebug
    {
        static EditorWindow GetHierarchyWindow()
        {
            var t = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = Resources.FindObjectsOfTypeAll(t).FirstOrDefault() as EditorWindow;
            if (window == null) Debug.LogError("Hierarchy окно не найдено. Открой его.");
            return window;
        }

        static string ObjNameOrNull(UnityEngine.Object obj)
        {
            return obj ? obj.name : "NULL";
        }

        static string TypeNameOrNull(object obj)
        {
            return obj != null ? obj.GetType().Name : "NULL";
        }

        [MenuItem("Tools/vHierarchy Debug/1. Dump TreeViewController Methods")]
        static void DumpTreeViewControllerMethods()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");
            var treeViewController = sceneHierarchy.GetFieldValue("m_TreeView");
            var treeViewControllerData = treeViewController.GetMemberValue("data");

            Debug.Log("=== SceneHierarchy METHODS ===");
            foreach (var m in sceneHierarchy.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name.ToLower().Contains("expand") 
                         || m.Name.ToLower().Contains("fold")
                         || m.Name.ToLower().Contains("collapse")
                         || m.Name.ToLower().Contains("setexpand"))
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  SceneHierarchy.{m.Name}({pars})");
            }

            Debug.Log("=== TreeViewController METHODS ===");
            foreach (var m in treeViewController.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name.ToLower().Contains("expand") 
                         || m.Name.ToLower().Contains("fold")
                         || m.Name.ToLower().Contains("collapse")
                         || m.Name.ToLower().Contains("item"))
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  TreeViewController.{m.Name}({pars})");
            }

            Debug.Log("=== TreeViewControllerData METHODS ===");
            foreach (var m in treeViewControllerData.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name.ToLower().Contains("expand") 
                         || m.Name.ToLower().Contains("fold")
                         || m.Name.ToLower().Contains("row")
                         || m.Name.ToLower().Contains("collapse"))
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  TreeViewControllerData.{m.Name}({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/2. Dump EntityId Structure")]
        static void DumpEntityIdStructure()
        {
            var entityIdType = typeof(UnityEngine.EntityId);
            
            Debug.Log("=== EntityId PROPERTIES ===");
            foreach (var p in entityIdType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Debug.Log($"  Property: {p.PropertyType.Name} {p.Name} (get={p.CanRead}, set={p.CanWrite})");

            Debug.Log("=== EntityId FIELDS ===");
            foreach (var f in entityIdType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Debug.Log($"  Field: {f.FieldType.Name} {f.Name}");

            Debug.Log("=== EntityId METHODS ===");
            foreach (var m in entityIdType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  Method: {m.ReturnType.Name} {m.Name}({pars})");
            }

            Debug.Log("=== EntityId CONSTRUCTORS ===");
            foreach (var c in entityIdType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var pars = string.Join(", ", c.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  Constructor: ({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/3. Dump SceneHierarchy Add-Create Methods")]
        static void DumpAddCreateMethods()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");

            Debug.Log("=== SceneHierarchy Create/Add METHODS ===");
            foreach (var m in sceneHierarchy.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.Name.ToLower().Contains("create") 
                         || m.Name.ToLower().Contains("add")
                         || m.Name.ToLower().Contains("menu"))
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  {m.Name}({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/4. Dump ExpandedIds Current State")]
        static void DumpExpandedIdsState()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");
            var treeViewController = sceneHierarchy.GetFieldValue("m_TreeView");
            var treeViewControllerState = treeViewController?.GetPropertyValue("state");

            Debug.Log("=== State TYPE ===");
            Debug.Log(treeViewControllerState?.GetType()?.FullName ?? "NULL");

            Debug.Log("=== State MEMBERS ===");
            if (treeViewControllerState != null)
            {
                foreach (var f in treeViewControllerState.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    Debug.Log($"  Field: {f.FieldType.Name} {f.Name}");

                foreach (var p in treeViewControllerState.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    Debug.Log($"  Property: {p.PropertyType.Name} {p.Name}");
            }
            
            Debug.Log("=== SceneHierarchy EXPANDED IDs related ===");
            foreach (var m in sceneHierarchy.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name.ToLower().Contains("get") && m.GetParameters().Length == 0)
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  {m.ReturnType.Name} {m.Name}({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/5. Test Toggle Expand Selected")]
        static void TestToggleExpandSelected()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject в иерархии"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            {
                Debug.LogError("Controller не найден для этого окна");
                return;
            }

            var controller = controllers[window];

            var iid = go.GetInstanceID_Safe();

            Debug.Log($"expandedIds count: {controller.expandedIds.Count}");
            Debug.Log($"GO instanceId: {iid}");
            Debug.Log($"Is in expandedIds: {controller.expandedIds.Contains(iid)}");

            controller.ToggleExpanded(iid);
            Debug.Log("ToggleExpanded вызван");
        }

        static System.Collections.Generic.Dictionary<EditorWindow, VHierarchyController> VHierarchyController_byWindow()
        {
            var field = typeof(global::VHierarchy.VHierarchy)
                .GetField("controllers_byWindow", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) 
                as System.Collections.Generic.Dictionary<EditorWindow, VHierarchyController>;
        }

        static global::VHierarchy.VHierarchyData GetData()
        {
            var field = typeof(global::VHierarchy.VHierarchy)
                .GetField("data", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) as global::VHierarchy.VHierarchyData;
        }

        static global::VHierarchy.VHierarchyPalette GetPalette()
        {
            var field = typeof(global::VHierarchy.VHierarchy)
                .GetField("palette", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) as global::VHierarchy.VHierarchyPalette;
        }

        [MenuItem("Tools/vHierarchy Debug/6. Test Bookmark GUI State")]
        static void TestBookmarkState()
        {
            var data = GetData();
            var palette = GetPalette();
            
            Debug.Log($"data != null: {data != null}");
            Debug.Log($"palette != null: {palette != null}");
            
            if (data != null)
                Debug.Log($"bookmarks count: {data.bookmarks.Count}");

            var navbarsField = typeof(global::VHierarchy.VHierarchy)
                .GetField("navbars_byWindow", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var navbars = navbarsField?.GetValue(null) 
                as System.Collections.Generic.Dictionary<EditorWindow, VHierarchyNavbar>;

            Debug.Log($"navbars count: {navbars?.Count ?? -1}");

            if (navbars != null)
                foreach (var kvp in navbars)
                    Debug.Log($"  window: {kvp.Key?.GetType()?.Name}, navbar: {kvp.Value != null}");

            Debug.Log($"navigationBarEnabled: {VHierarchyMenu.navigationBarEnabled}");
        }

        [MenuItem("Tools/vHierarchy Debug/7. Test Expand E Hotkey")]
        static void TestExpandHotkey()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject с детьми"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            { Debug.LogError("Controller не найден"); return; }

            var controller = controllers[window];
            var iid = go.GetInstanceID_Safe();

            Debug.Log($"=== TEST EXPAND ===");
            Debug.Log($"GO: {go.name}, iid: {iid}");
            Debug.Log($"childCount: {go.transform.childCount}");
            Debug.Log($"expandedIds: [{string.Join(", ", controller.expandedIds)}]");
            Debug.Log($"isExpanded: {controller.expandedIds.Contains(iid)}");
            Debug.Log($"treeViewController type: {controller.GetType().GetField("treeViewController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(controller)?.GetType()?.Name}");
            Debug.Log($"treeViewControllerData type: {controller.GetType().GetField("treeViewControllerData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(controller)?.GetType()?.Name}");

            // Тест создания EntityId
            var entityId = global::VHierarchy.VHierarchy.CreateEntityId(iid);
            Debug.Log($"Created EntityId type: {entityId?.GetType()?.Name}");
            Debug.Log($"EntityId value: {entityId}");

            // Тест GetRowIndex
            var rowIndex = controller.GetRowIndex(iid);
            Debug.Log($"GetRowIndex result: {rowIndex}");

            // Тест SetExpanded_withAnimation
            try
            {
                controller.SetExpanded_withAnimation(iid, !controller.expandedIds.Contains(iid));
                Debug.Log("SetExpanded_withAnimation: SUCCESS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SetExpanded_withAnimation FAILED: {e.Message}");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/8. Test CollapseAll")]
        static void TestCollapseAll()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            { Debug.LogError("Controller не найден"); return; }

            var controller = controllers[window];

            Debug.Log($"=== TEST COLLAPSE ALL ===");
            Debug.Log($"expandedIds count: {controller.expandedIds.Count}");
            Debug.Log($"expandedIds: [{string.Join(", ", controller.expandedIds)}]");

            try
            {
                controller.CollapseAll();
                Debug.Log("CollapseAll: SUCCESS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CollapseAll FAILED: {e.Message}\n{e.StackTrace}");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/9. Test Component Minimap")]
        static void TestComponentMinimap()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var guis = typeof(global::VHierarchy.VHierarchy)
                .GetField("guis_byWindow",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static)
                ?.GetValue(null) as System.Collections.Generic.Dictionary<EditorWindow, VHierarchyGUI>;

            Debug.Log($"=== TEST COMPONENT MINIMAP ===");
            Debug.Log($"guis_byWindow count: {guis?.Count ?? -1}");
            Debug.Log($"componentMinimapEnabled: {VHierarchyMenu.componentMinimapEnabled}");

            if (guis != null && guis.ContainsKey(window))
            {
                var gui = guis[window];
                Debug.Log($"GUI found: {gui != null}");
                Debug.Log($"GUI type: {gui?.GetType()?.Name}");
            }
            else
                Debug.LogError("GUI not found for window");

            Debug.Log($"Components on GO:");
            foreach (var c in go.GetComponents<Component>())
            {
                var icon = global::VHierarchy.VHierarchy.GetComponentIcon(c);
                Debug.Log($"  {c?.GetType()?.Name} - icon: {icon?.name ?? "NULL"}");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/10. Test Navbar Bookmarks Rendering")]
        static void TestNavbarBookmarks()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var navbarsField = typeof(global::VHierarchy.VHierarchy)
                .GetField("navbars_byWindow",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static);

            var navbars = navbarsField?.GetValue(null)
                as System.Collections.Generic.Dictionary<EditorWindow, VHierarchyNavbar>;

            Debug.Log($"=== TEST NAVBAR BOOKMARKS ===");
            Debug.Log($"navbars_byWindow is PUBLIC field: {navbarsField != null}");
            Debug.Log($"navbars count: {navbars?.Count ?? -1}");
            Debug.Log($"navigationBarEnabled: {VHierarchyMenu.navigationBarEnabled}");
            Debug.Log($"data != null: {GetData() != null}");

            if (GetData() != null)
            {
                var data = GetData();
                Debug.Log($"bookmarks count: {data.bookmarks.Count}");
                Debug.Log($"bookmarkedScenePaths count: {data.bookmarkedScenePaths.Count}");
            }

            if (navbars != null && navbars.ContainsKey(window))
            {
                var navbar = navbars[window];
                Debug.Log($"Navbar found: {navbar != null}");

                // Проверяем все поля navbar
                foreach (var f in navbar.GetType()
                    .GetFields(System.Reflection.BindingFlags.Public |
                               System.Reflection.BindingFlags.NonPublic |
                               System.Reflection.BindingFlags.Instance))
                {
                    try { Debug.Log($"  navbar.{f.Name} = {f.GetValue(navbar)}"); }
                    catch { }
                }
            }
            else
                Debug.LogError("Navbar NOT found for window");
        }

        [MenuItem("Tools/vHierarchy Debug/11. Test Plus Button")]
        static void TestPlusButton()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");

            Debug.Log($"=== TEST PLUS BUTTON ===");

            // Проверяем GameObjectCreateDropdownButton
            var dropdownMethod = sceneHierarchy.GetType().GetMethod(
                "GameObjectCreateDropdownButton",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            Debug.Log($"GameObjectCreateDropdownButton found: {dropdownMethod != null}");
            if (dropdownMethod != null)
            {
                var pars = string.Join(", ", dropdownMethod.GetParameters()
                    .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  Signature: GameObjectCreateDropdownButton({pars})");
            }

            // Все методы SceneHierarchyWindow
            Debug.Log("=== SceneHierarchyWindow METHODS (GameObject/Create) ===");
            foreach (var m in window.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name.ToLower().Contains("create") ||
                            m.Name.ToLower().Contains("gameobject") ||
                            m.Name.ToLower().Contains("dropdown"))
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters()
                    .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  Window.{m.Name}({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/12. Test EntityId Round-trip")]
        static void TestEntityIdRoundtrip()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject"); return; }

            var iid = go.GetInstanceID_Safe();
            Debug.Log($"=== TEST EntityId ROUND-TRIP ===");
            Debug.Log($"Original instanceId: {iid}");

            var entityId = global::VHierarchy.VHierarchy.CreateEntityId(iid);
            Debug.Log($"Created EntityId: {entityId}");

            var entityIdTyped = (UnityEngine.EntityId)entityId;
            var toULongMethod = typeof(UnityEngine.EntityId).GetMethod(
                "ToULong",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var rawData = toULongMethod != null
                ? (ulong)toULongMethod.Invoke(null, new object[] { entityIdTyped })
                : 0ul;
            Debug.Log($"EntityId.GetRawData(): {rawData}");
            Debug.Log($"EntityId.GetRawData() as int: {(int)(uint)rawData}");

            var extractedIid = global::VHierarchy.VHierarchy.ExtractInstanceId(entityId);
            Debug.Log($"ExtractInstanceId result: {extractedIid}");
            Debug.Log($"Round-trip OK: {iid == extractedIid}");

            // Проверяем объект по iid
            var foundObj = global::VHierarchy.Libs.VUtils.InstanceIDToObject_Safe(extractedIid);
            Debug.Log($"InstanceIDToObject_Safe result: {foundObj?.name ?? "NULL"}");

            // Проверяем объект по entityId
            var foundObj2 = global::VHierarchy.Libs.VUtils.InstanceIDToObject_Safe(iid);
            Debug.Log($"Original IID object: {foundObj2?.name ?? "NULL"}");
        }

        [MenuItem("Tools/vHierarchy Debug/13. Test Hotkey Chain Full")]
        static void TestHotkeyChainFull()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject с детьми в иерархии"); return; }
            if (go.transform.childCount == 0) { Debug.LogError("У объекта нет детей!"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            { Debug.LogError("Controller не найден"); return; }

            var controller = controllers[window];
            var iid = go.GetInstanceID_Safe();

            Debug.Log("=== HOTKEY CHAIN TEST ===");
            Debug.Log("GO: " + go.name + ", iid: " + iid + ", children: " + go.transform.childCount);
            Debug.Log("hoveredGo: " + ObjNameOrNull(VHierarchy.hoveredGo));
            Debug.Log("expandedIds before: [" + string.Join(", ", controller.expandedIds) + "]");
            Debug.Log("isExpanded before: " + controller.expandedIds.Contains(iid));

            var treeViewControllerField = controller.GetType()
                .GetField("treeViewController",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            var treeViewController = treeViewControllerField?.GetValue(controller);
            Debug.Log("treeViewController: " + TypeNameOrNull(treeViewController));

            if (treeViewController != null)
            {
                var methods = treeViewController.GetType()
                    .GetMethods(System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance)
                    .Where(m => m.Name == "ChangeFoldingForSingleItem")
                    .ToList();

                Debug.Log("ChangeFoldingForSingleItem overloads: " + methods.Count);
                foreach (var m in methods)
                {
                    var pars = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                    Debug.Log("  Signature: (" + pars + ")");
                }

                var method = methods.FirstOrDefault();
                if (method != null)
                {
                    var pType = method.GetParameters()[0].ParameterType;
                    Debug.Log("First param type: " + pType.FullName);

                    object idParam = VHierarchy.CreateEntityId(iid);
                    Debug.Log("Created param type: " + TypeNameOrNull(idParam));

                    var rawField = pType.GetField("m_rawData",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (rawField != null)
                        Debug.Log("m_rawData in created EntityId: " + rawField.GetValue(idParam));

                    try
                    {
                        method.Invoke(treeViewController, new object[] { idParam, true });
                        Debug.Log("ChangeFoldingForSingleItem(EntityId, true): SUCCESS");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("ChangeFoldingForSingleItem FAILED: " + e.GetBaseException().Message);
                    }
                }
            }

            controller.UpdateState();
            Debug.Log("expandedIds after: [" + string.Join(", ", controller.expandedIds) + "]");
            Debug.Log("isExpanded after: " + controller.expandedIds.Contains(iid));
            window.Repaint();
        }

        [MenuItem("Tools/vHierarchy Debug/14. Test hoveredGo Detection")]
        static void TestHoveredGoDetection()
        {
            Debug.Log("=== HOVERED GO DETECTION ===");
            Debug.Log("hoveredGo: " + ObjNameOrNull(VHierarchy.hoveredGo));
            Debug.Log("hoveredScene: " + VHierarchy.hoveredScene.name);
            Debug.Log("mouseOverWindow: " + TypeNameOrNull(EditorWindow.mouseOverWindow));
            Debug.Log("focusedWindow: " + TypeNameOrNull(EditorWindow.focusedWindow));

            var guis = typeof(VHierarchy)
                .GetField("guis_byWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.GetValue(null) as System.Collections.Generic.Dictionary<EditorWindow, VHierarchyGUI>;

            Debug.Log("guis_byWindow count: " + (guis != null ? guis.Count.ToString() : "-1"));

            if (guis != null)
            {
                foreach (var kvp in guis)
                {
                    var gui = kvp.Value;
                    var isTreeFocused = gui.GetType()
                        .GetField("isTreeFocused",
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)
                        ?.GetValue(gui);
                    Debug.Log("  GUI window: " + TypeNameOrNull(kvp.Key) + ", isTreeFocused: " + isTreeFocused);
                }
            }

            var globalEventHandler = typeof(EditorApplication)
                .GetFieldValue<EditorApplication.CallbackFunction>("globalEventHandler");
            Debug.Log("globalEventHandler count: " + (globalEventHandler?.GetInvocationList()?.Length ?? 0));
            if (globalEventHandler != null)
                foreach (var d in globalEventHandler.GetInvocationList())
                    Debug.Log("  handler: " + d.Method.DeclaringType?.Name + "." + d.Method.Name);
        }

        [MenuItem("Tools/vHierarchy Debug/15. Simulate E Hotkey")]
        static void SimulateEHotkey()
        {
            Debug.Log("=== SIMULATE E HOTKEY ===");

            var go = Selection.activeGameObject;
            Debug.Log("Selection.activeGameObject: " + ObjNameOrNull(go));
            Debug.Log("hoveredGo: " + ObjNameOrNull(VHierarchy.hoveredGo));

            if (go == null) { Debug.LogError("Выдели объект"); return; }
            if (go.transform.childCount == 0) { Debug.LogError("У объекта нет детей"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            { Debug.LogError("Controller не найден"); return; }

            var controller = controllers[window];
            var iid = go.GetInstanceID_Safe();

            bool isExpanded = controller.expandedIds.Contains(iid);
            Debug.Log("isExpanded before: " + isExpanded);

            controller.ToggleExpanded(iid);
            Debug.Log("ToggleExpanded(" + iid + ") вызван");

            controller.UpdateState();
            Debug.Log("isExpanded after: " + controller.expandedIds.Contains(iid));
            window.Repaint();
        }

        [MenuItem("Tools/vHierarchy Debug/16. Dump expandedIds with types")]
        static void DumpExpandedIds()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var controllers = VHierarchyController_byWindow();
            if (controllers == null || !controllers.ContainsKey(window))
            { Debug.LogError("Controller не найден"); return; }

            var controller = controllers[window];

            Debug.Log($"expandedIds count: {controller.expandedIds.Count}");
            foreach (var id in controller.expandedIds)
            {
                var obj = global::VHierarchy.Libs.VUtils.InstanceIDToObject_Safe(id);
                string typeName = obj != null ? obj.GetType().Name : "NULL(scene?)";
                string objName  = obj != null ? obj.name : "(no object)";
                Debug.Log($"  id={id}  type={typeName}  name={objName}");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/17. Dump TreeViewController Methods (Reload/Refresh/Rebuild)")]
        static void DumpTreeViewReloadMethods()
        {
            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");
            var treeViewController = sceneHierarchy.GetFieldValue("m_TreeView");
            var treeViewControllerData = treeViewController.GetMemberValue("data");

            Debug.Log("=== SceneHierarchy ALL METHODS ===");
            foreach (var m in sceneHierarchy.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  SceneHierarchy.{m.Name}({pars})");
            }

            Debug.Log("=== TreeViewController ALL METHODS ===");
            foreach (var m in treeViewController.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  TreeViewController.{m.Name}({pars})");
            }

            Debug.Log("=== TreeViewControllerData ALL METHODS ===");
            foreach (var m in treeViewControllerData.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .OrderBy(m => m.Name))
            {
                var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  TreeViewControllerData.{m.Name}({pars})");
            }
        }

        [MenuItem("Tools/vHierarchy Debug/18. Test SetExpanded Direct")]
        static void TestSetExpandedDirect()
        {
            var go = Selection.activeGameObject;
            if (!go) { Debug.LogError("Выдели GameObject с детьми"); return; }
            if (go.transform.childCount == 0) { Debug.LogError("У объекта нет детей"); return; }

            var window = GetHierarchyWindow();
            if (window == null) return;

            var sceneHierarchy = window.GetFieldValue("m_SceneHierarchy");
            var treeViewController = sceneHierarchy.GetFieldValue("m_TreeView");
            var treeViewControllerData = treeViewController.GetMemberValue("data");

            var iid = go.GetInstanceID_Safe();
            var entityId = global::VHierarchy.VHierarchy.CreateEntityId(iid);

            Debug.Log($"=== TEST SetExpanded DIRECT ===");
            Debug.Log($"GO: {go.name}, iid: {iid}");
            Debug.Log($"EntityId: {entityId}");

            Debug.Log("=== SetExpanded overloads ===");
            foreach (var m in treeViewControllerData.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name == "SetExpanded"))
            {
                var pars = string.Join(", ", m.GetParameters()
                    .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  SetExpanded({pars})");

                if (m.GetParameters().Length == 2 && 
                    m.GetParameters()[0].ParameterType.Name == "EntityId")
                {
                    try
                    {
                        var isExpandedMethod = treeViewControllerData.GetType()
                            .GetMethods(System.Reflection.BindingFlags.Public | 
                                        System.Reflection.BindingFlags.NonPublic | 
                                        System.Reflection.BindingFlags.Instance)
                            .FirstOrDefault(mm => mm.Name == "IsExpanded" && 
                                                  mm.GetParameters().Length == 1 &&
                                                  mm.GetParameters()[0].ParameterType.Name == "EntityId");

                        if (isExpandedMethod != null)
                        {
                            var isExpanded = (bool)isExpandedMethod.Invoke(
                                treeViewControllerData, new object[] { entityId });
                            Debug.Log($"  IsExpanded before: {isExpanded}");
                            
                            m.Invoke(treeViewControllerData, new object[] { entityId, !isExpanded });
                            Debug.Log($"  SetExpanded({!isExpanded}): SUCCESS");
                            
                            var isExpandedAfter = (bool)isExpandedMethod.Invoke(
                                treeViewControllerData, new object[] { entityId });
                            Debug.Log($"  IsExpanded after: {isExpandedAfter}");
                            
                            window.Repaint();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"  SetExpanded FAILED: {e.GetBaseException().Message}");
                    }
                }
            }

            Debug.Log("=== IsExpanded overloads ===");
            foreach (var m in treeViewControllerData.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name == "IsExpanded"))
            {
                var pars = string.Join(", ", m.GetParameters()
                    .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"  IsExpanded({pars})");
            }
        }
    }
}
#endif
