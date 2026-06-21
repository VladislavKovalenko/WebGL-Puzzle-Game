#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using static VHierarchy.Libs.VUtils;
using static VHierarchy.Libs.VGUI;
using static VHierarchy.VHierarchy;

namespace VHierarchy
{
    public class VHierarchyController
    {
        public void UpdateExpandQueue()
        {
            if (treeViewAnimatesExpansion) return;

            if (!expandQueue_toAnimate.Any())
            {
                if (!expandQueue_toCollapseAfterAnimation.Any()) return;

                foreach (var r in expandQueue_toCollapseAfterAnimation)
                    SetExpanded(r, false);

                expandQueue_toCollapseAfterAnimation.Clear();
                return;
            }

            var id = expandQueue_toAnimate.First().id;
            var expand = expandQueue_toAnimate.First().expand;

            if (expandedIds.Contains(id) != expand)
            {
                SetExpanded(id, expand);
                if (expand) expandedIds.Add(id);
                else expandedIds.Remove(id);
            }

            expandQueue_toAnimate.RemoveAt(0);
            window.Repaint();
        }

        public List<ExpandQueueEntry> expandQueue_toAnimate = new();
        public List<int> expandQueue_toCollapseAfterAnimation = new();

        public struct ExpandQueueEntry { public int id; public bool expand; }
        public bool animatingExpansion => expandQueue_toAnimate.Any() || expandQueue_toCollapseAfterAnimation.Any();

        public void UpdateScrollAnimation()
        {
            if (!animatingScroll) return;
            var lerpSpeed = 10;
            var lerpedScrollPos = MathUtil.SmoothDamp(currentScrollPos, targetScrollPos, lerpSpeed, ref scrollPosDerivative, editorDeltaTime);

            SetScrollPos(lerpedScrollPos);
            window.Repaint();

            if (lerpedScrollPos.DistanceTo(targetScrollPos) > .4f) return;
            SetScrollPos(targetScrollPos);
            animatingScroll = false;
        }

        public float targetScrollPos;
        public float scrollPosDerivative;
        public bool animatingScroll;

        public void UpdateHighlightAnimation()
        {
            if (!animatingHighlight) return;
            var lerpSpeed = 1.3f;
            MathUtil.SmoothDamp(ref highlightAmount, 0, lerpSpeed, ref highlightDerivative, editorDeltaTime);
            window.Repaint();

            if (highlightAmount > .05f) return;
            highlightAmount = 0;
            animatingHighlight = false;
        }

        public float highlightAmount;
        public float highlightDerivative;
        public bool animatingHighlight;
        public GameObject objectToHighlight;

        public void UpdateState()
        {
            var sceneHierarchy = window?.GetFieldValue("m_SceneHierarchy");
            treeViewController = sceneHierarchy?.GetFieldValue("m_TreeView");
            treeViewControllerData = treeViewController?.GetMemberValue("data");

            var state = treeViewController?.GetPropertyValue("state");
            currentScrollPos = state?.GetMemberValue<Vector2>("scrollPos").y ?? 0;

            expandedIds.Clear();
            if (state != null)
            {
                foreach (var fieldName in new[] { "m_ExpandedIDs", "expandedIDs", "m_Expanded" })
                {
                    var field = state.GetType().GetField(fieldName,
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (field?.GetValue(state) is System.Collections.IEnumerable list)
                    {
                        foreach (var item in list)
                            expandedIds.Add(VHierarchy.ExtractInstanceId(item));
                        break;
                    }
                }
            }

            treeViewAnimatesScroll = false;
            try
            {
                foreach (var fname in new[] { "m_FramingAnimFloat", "m_ScrollAnimFloat", "m_AnimFloat" })
                {
                    var animFloat = treeViewController?.GetMemberValue(fname);
                    if (animFloat != null && animFloat.GetMemberValue<bool>("isAnimating"))
                    {
                        treeViewAnimatesScroll = true;
                        break;
                    }
                }
            }
            catch { }

            treeViewAnimatesExpansion = false;
            try
            {
                var animator = treeViewController?.GetMemberValue("m_ExpansionAnimator");
                if (animator != null)
                    treeViewAnimatesExpansion = animator.GetMemberValue<bool>("isAnimating");
            }
            catch { }
        }

        object treeViewController;
        object treeViewControllerData;
        public float currentScrollPos;
        public List<int> expandedIds = new();
        public bool treeViewAnimatesScroll;
        public bool treeViewAnimatesExpansion;

        private System.Reflection.MethodInfo _getRowMethod;
        private bool _getRowUsesEntityId;

        public int GetRowIndex(int instanceId)
        {
            if (treeViewControllerData == null) return -1;

            if (_getRowMethod == null)
            {
                foreach (var m in treeViewControllerData.GetType()
                    .GetMethods(System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance))
                {
                    if (m.Name != "GetRow" || m.GetParameters().Length != 1) continue;
                    _getRowMethod = m;
                    _getRowUsesEntityId = m.GetParameters()[0].ParameterType.Name.Contains("EntityId");
                    break;
                }
            }

            if (_getRowMethod == null) return -1;

            try
            {
                var param = _getRowUsesEntityId 
                    ? VHierarchy.CreateEntityId(instanceId) 
                    : (object)instanceId;
                return (int)_getRowMethod.Invoke(treeViewControllerData, new[] { param });
            }
            catch { return -1; }
        }

        object GetRealOrSyntheticEntityId(int id)
        {
            if (VHierarchy.realEntityIds_byInstanceId.TryGetValue(id, out var realEntityId))
                return realEntityId;

            return VHierarchy.CreateEntityId(id);
        }

        object ConvertExpandedIdForType(int id, System.Type targetType)
        {
            if (targetType == typeof(int))
                return id;

            if (targetType == typeof(UnityEngine.EntityId) || targetType.Name.Contains("EntityId"))
                return GetRealOrSyntheticEntityId(id);

            return id;
        }

        bool TryInvokeChangeFoldingForSingleItem(int id, bool expanded)
        {
            if (treeViewController == null) return false;

            var method = treeViewController.GetType()
                .GetMethods(maxBindingFlags)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "ChangeFoldingForSingleItem") return false;

                    var p = m.GetParameters();
                    return p.Length == 2 && p[1].ParameterType == typeof(bool);
                });

            if (method == null) return false;

            try
            {
                var firstParamType = method.GetParameters()[0].ParameterType;
                var arg0 = ConvertExpandedIdForType(id, firstParamType);

                method.Invoke(treeViewController, new object[] { arg0, expanded });
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool TryApplyExpandedStateToTreeViewState()
        {
            var state = treeViewController?.GetPropertyValue("state");
            if (state == null) return false;

            foreach (var fieldName in new[] { "m_ExpandedIDs", "expandedIDs", "m_Expanded" })
            {
                var field = state.GetType().GetField(fieldName, maxBindingFlags);
                if (field == null) continue;

                var fieldType = field.FieldType;
                var uniqueIds = expandedIds.Distinct().ToList();

                try
                {
                    if (fieldType.IsArray)
                    {
                        var elementType = fieldType.GetElementType();
                        var array = System.Array.CreateInstance(elementType, uniqueIds.Count);

                        for (int i = 0; i < uniqueIds.Count; i++)
                            array.SetValue(ConvertExpandedIdForType(uniqueIds[i], elementType), i);

                        field.SetValue(state, array);
                        return true;
                    }

                    var collection = field.GetValue(state);
                    if (collection == null)
                    {
                        collection = System.Activator.CreateInstance(fieldType);
                        field.SetValue(state, collection);
                    }

                    var clearMethod = collection.GetType().GetMethod("Clear", maxBindingFlags);
                    clearMethod?.Invoke(collection, null);

                    var addMethod = collection.GetType()
                        .GetMethods(maxBindingFlags)
                        .FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1);

                    if (addMethod == null) continue;

                    var itemType = addMethod.GetParameters()[0].ParameterType;

                    foreach (var id in uniqueIds)
                        addMethod.Invoke(collection, new object[] { ConvertExpandedIdForType(id, itemType) });

                    return true;
                }
                catch { }
            }

            return false;
        }

        void ReloadTree()
        {
            try { treeViewController?.InvokeMethod("ReloadData"); } catch { }
            try { window?.GetMemberValue("m_SceneHierarchy")?.InvokeMethod("ReloadData"); } catch { }
        }

        public void ToggleExpanded(int id)
        {
            UpdateState();

            bool shouldExpand = !expandedIds.Contains(id);

            if (TryInvokeChangeFoldingForSingleItem(id, shouldExpand))
            {
                EditorApplication.delayCall += () =>
                {
                    UpdateState();
                    window.Repaint();
                };
                return;
            }

            if (shouldExpand)
            {
                if (!expandedIds.Contains(id))
                    expandedIds.Add(id);
            }
            else
                expandedIds.Remove(id);

            ApplyExpandedState();
            window.Repaint();
        }

        public void CollapseAll()
        {
            UpdateState();

            var sceneHandles = GetAllSceneHandles().ToHashSet();

            foreach (var id in expandedIds.ToList())
                if (!sceneHandles.Contains(id))
                    TryInvokeChangeFoldingForSingleItem(id, false);

            StartScrollAnimation(0);

            EditorApplication.delayCall += () =>
            {
                UpdateState();
                window.Repaint();
            };
        }

        public void Isolate(int targetId)
        {
            UpdateState();

            var idsToKeep = GetParentChainIncludingScene(targetId).ToHashSet();

            foreach (var id in expandedIds.ToList())
                if (!idsToKeep.Contains(id))
                    TryInvokeChangeFoldingForSingleItem(id, false);

            foreach (var id in idsToKeep)
                if (!expandedIds.Contains(id))
                    TryInvokeChangeFoldingForSingleItem(id, true);

            EditorApplication.delayCall += () =>
            {
                UpdateState();
                window.Repaint();
            };
        }

        private List<int> GetAllSceneHandles()
        {
            var handles = new List<int>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                handles.Add(VHierarchy.GetSceneHandle(scene));
            }
            return handles;
        }

        private List<int> GetParentChainIncludingScene(int instanceId)
        {
            var chain = new List<int>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                if (VHierarchy.GetSceneHandle(scene) == instanceId)
                {
                    chain.Add(instanceId);
                    return chain;
                }
            }

            if (InstanceIDToObject_Safe(instanceId) is not GameObject go)
                return chain;

            chain.Add(go.GetInstanceID_Safe());

            var current = go.transform.parent;
            while (current != null)
            {
                chain.Add(current.gameObject.GetInstanceID_Safe());
                current = current.parent;
            }

            chain.Add(VHierarchy.GetSceneHandle(go.scene));
            return chain;
        }

        private void ApplyExpandedState()
        {
            UpdateState();

            if (!TryApplyExpandedStateToTreeViewState())
            {
                if (treeViewControllerData != null)
                {
                    var setExpandedAll = treeViewControllerData.GetType()
                        .GetMethods(maxBindingFlags)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "SetExpandedAll") return false;
                            var p = m.GetParameters();
                            return p.Length == 1 && p[0].ParameterType == typeof(bool);
                        });

                    if (setExpandedAll != null)
                    {
                        try { setExpandedAll.Invoke(treeViewControllerData, new object[] { false }); }
                        catch { }
                    }

                    var setExpanded = treeViewControllerData.GetType()
                        .GetMethods(maxBindingFlags)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "SetExpanded") return false;
                            var p = m.GetParameters();
                            return p.Length == 2 && p[1].ParameterType == typeof(bool);
                        });

                    if (setExpanded != null)
                    {
                        var firstParamType = setExpanded.GetParameters()[0].ParameterType;

                        foreach (var id in expandedIds.Distinct())
                        {
                            try
                            {
                                setExpanded.Invoke(treeViewControllerData, new object[]
                                {
                                    ConvertExpandedIdForType(id, firstParamType),
                                    true
                                });
                            }
                            catch { }
                        }
                    }
                }
            }

            ReloadTree();

            EditorApplication.delayCall += () =>
            {
                ReloadTree();
                UpdateState();
                window.Repaint();
            };
        }

        public void SetExpanded_withAnimation(int instanceId, bool expanded) => SetExpanded(instanceId, expanded);
        public void SetExpanded_withoutAnimation(int instanceId, bool expanded) => SetExpanded(instanceId, expanded);

        public void SetExpanded(int instanceId, bool expanded)
        {
            if (expanded) { if (!expandedIds.Contains(instanceId)) expandedIds.Add(instanceId); }
            else expandedIds.Remove(instanceId);

            ApplyExpandedState();
        }

        public void StartScrollAnimation(float targetScrollPos)
        {
            if (targetScrollPos.DistanceTo(currentScrollPos) < .05f) return;
            this.targetScrollPos = targetScrollPos;
            animatingScroll = true;
        }

        public void SetScrollPos(float targetScrollPos)
        {
            var stateObj = window?.GetMemberValue("m_SceneHierarchy")?.GetMemberValue("m_TreeViewState");
            if (stateObj != null) stateObj.SetMemberValue("scrollPos", Vector2.up * targetScrollPos);
        }

        public void RevealObject(GameObject go, bool expand, bool highlight, bool snapToTopMargin)
        {
            var idsToExpand = new List<int>();

            if (expand && go.transform.childCount > 0)
                idsToExpand.Add(go.GetInstanceID_Safe());

            var cur = go.transform;
            while (cur = cur.parent)
                idsToExpand.Add(cur.gameObject.GetInstanceID_Safe());

            idsToExpand.Add(VHierarchy.GetSceneHandle(go.scene));

            idsToExpand.RemoveAll(r => expandedIds.Contains(r));

            foreach (var id in idsToExpand)
            {
                if (!expandedIds.Contains(id)) expandedIds.Add(id);
            }
            
            ApplyExpandedState();

            var rowCount = treeViewControllerData?.GetMemberValue<System.Collections.ICollection>("m_Rows")?.Count ?? 0;
            var maxScrollPos = rowCount * 16 - window.position.height + 26.9f;

            var rowIndex = GetRowIndex(go.GetInstanceID_Safe());
            var rowPos = rowIndex * 16f + 8;

            var scrollAreaHeight = window.GetMemberValue<Rect>("treeViewRect").height;

            var margin = 48;
            var targetScrollPos = 0f;

            if (expand)
                targetScrollPos = (rowPos - margin).Min(maxScrollPos).Max(0);
            else
                targetScrollPos = currentScrollPos.Min(rowPos - margin).Max(rowPos - scrollAreaHeight + margin).Min(maxScrollPos).Max(0);

            if (targetScrollPos < 25) targetScrollPos = 0;

            StartScrollAnimation(targetScrollPos);

            if (!highlight) return;
            highlightAmount = 2.2f;
            animatingHighlight = true;
            objectToHighlight = go;
        }

        public VHierarchyController(EditorWindow window) => this.window = window;
        public EditorWindow window;
        public VHierarchyGUI gui => VHierarchy.guis_byWindow[window];
    }
}
#endif
