using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PolycountViewer : EditorWindow
{
    private Dictionary<GameObject, int> objectVertexCounts = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, int> objectColliderVertexCounts = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, int> totalPrefabVertexCounts = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, Dictionary<GameObject, int>> prefabVertexDetails = new Dictionary<GameObject, Dictionary<GameObject, int>>();
    private GameObject[] lastSelectedObjects;

    private int totalVertexCount = 0;
    private int totalColliderVertexCount = 0;
    private int totalLODVertexCount = 0;
    private int totalLOD0VertexCount = 0;

    private Vector2 scrollPosition;

    [MenuItem("Window/Polycount Viewer")]
    public static void ShowWindow()
    {
        GetWindow<PolycountViewer>("Polycount Viewer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Polycount Viewer", EditorStyles.boldLabel);

        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects != lastSelectedObjects)
        {
            lastSelectedObjects = selectedObjects;
            UpdatePolycount();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 40));

        if (selectedObjects.Length > 1)
        {
            GUILayout.Space(10);
            GUILayout.Label("Total Polycount", EditorStyles.boldLabel);
            GUILayout.Label($"Total Polycount for Selected Objects: {totalVertexCount}");
            GUILayout.Label($"Total Polycount for Selected MeshCollider Objects: {totalColliderVertexCount}");
            GUILayout.Label($"Total Polycount for LOD Objects: {totalLODVertexCount}");
            GUILayout.Label($"Total Polycount for LOD0 Objects: {totalLOD0VertexCount}");

            GUILayout.Space(10);
        }

        GUILayout.Label("Objects Details", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (selectedObjects.Length > 0)
        {
            foreach (var obj in selectedObjects)
            {
                GUILayout.Label($"Selected Object: {obj.name}");
                GUILayout.Label($"Vertex Count: {objectVertexCounts[obj]}");
                GUILayout.Label($"Mesh Collider Vertex Count: {objectColliderVertexCounts[obj]}");

                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    GUILayout.Label($"Total Prefab Vertex Count: {totalPrefabVertexCounts[obj]}");
                    GUILayout.Label("Prefab Details:");
                    foreach (var kvp in prefabVertexDetails[obj])
                    {
                        GUILayout.Label($"{kvp.Key.name}: {kvp.Value} vertices");
                    }
                }
                GUILayout.Space(10);
            }
        }
        else
        {
            GUILayout.Label("Select one or more objects to view their polycount.");
        }

        GUILayout.EndScrollView();
    }

    private void Update()
    {
        Repaint();
    }

    private void UpdatePolycount()
    {
        totalVertexCount = 0;
        totalColliderVertexCount = 0;
        totalLODVertexCount = 0;
        totalLOD0VertexCount = 0;

        objectVertexCounts.Clear();
        objectColliderVertexCounts.Clear();
        totalPrefabVertexCounts.Clear();
        prefabVertexDetails.Clear();

        foreach (var obj in lastSelectedObjects)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            int vertexCount = (meshFilter && meshFilter.sharedMesh) ? meshFilter.sharedMesh.vertexCount : 0;
            objectVertexCounts[obj] = vertexCount;
            totalVertexCount += vertexCount;

            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            int colliderVertexCount = (meshCollider && meshCollider.sharedMesh) ? meshCollider.sharedMesh.vertexCount : 0;
            objectColliderVertexCounts[obj] = colliderVertexCount;
            totalColliderVertexCount += colliderVertexCount;

            int totalPrefabCount = 0;
            Dictionary<GameObject, int> prefabDetails = new Dictionary<GameObject, int>();

            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh)
                    {
                        totalPrefabCount += mf.sharedMesh.vertexCount;
                        prefabDetails[mf.gameObject] = mf.sharedMesh.vertexCount;
                        totalLODVertexCount += mf.sharedMesh.vertexCount;
                    }
                }

                LODGroup lodGroup = obj.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    LOD[] lods = lodGroup.GetLODs();
                    if (lods.Length > 0 && lods[0].renderers.Length > 0)
                    {
                        foreach (var renderer in lods[0].renderers)
                        {
                            MeshFilter lod0MeshFilter = renderer.GetComponent<MeshFilter>();
                            if (lod0MeshFilter && lod0MeshFilter.sharedMesh)
                            {
                                totalLOD0VertexCount += lod0MeshFilter.sharedMesh.vertexCount;
                            }
                        }
                    }
                }
            }

            totalPrefabVertexCounts[obj] = totalPrefabCount;
            prefabVertexDetails[obj] = prefabDetails;
        }
    }
}








