using UnityEditor;
using UnityEngine;
using System;

public class QuickMemoryClean : EditorWindow
{
    [MenuItem("Tools/Быстрая очистка памяти")]
    public static void ShowWindow()
    {
        GetWindow<QuickMemoryClean>("Очистка памяти");
    }

    private void OnGUI()
    {
        GUILayout.Label("Очистка памяти редактора", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Очистить сейчас", GUILayout.Height(40)))
        {
            CleanMemory();
        }
    }

    private void CleanMemory()
    {
        long before = GC.GetTotalMemory(false);

        EditorUtility.DisplayProgressBar("Очистка памяти", "Выгрузка неиспользуемых ресурсов...", 0.5f);
        Resources.UnloadUnusedAssets();
        GC.Collect();
        EditorUtility.ClearProgressBar();

        long after = GC.GetTotalMemory(false);
        Debug.Log($"✅ Память очищена! До: {before / 1024 / 1024} MB, После: {after / 1024 / 1024} MB");
    }
}
