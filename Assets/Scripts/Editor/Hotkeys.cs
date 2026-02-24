using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Corowan.Editor
{
    public class UnityExtededShortKeys : ScriptableObject
    {
 
        //edit the keys here .
        [MenuItem("HotKey/Play (with asset refresh) _b")]
        static void PlayGame()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
            EditorApplication.ExecuteMenuItem("Edit/Play Mode/Play");
        }

        [MenuItem("HotKey/Stop (or play) _#B")]
        static void StopOrPlay() => EditorApplication.ExecuteMenuItem("Edit/Play Mode/Play");

        [MenuItem("HotKey/Refresh Assets _F5")]
        static void DoRefresh() => EditorApplication.ExecuteMenuItem("Assets/Refresh");

        [MenuItem("HotKey/Create Folder _F12")]
        static void DoCreateFolder() => EditorApplication.ExecuteMenuItem("Assets/Create/Folder");

        [MenuItem("HotKey/Create Empty C# Script #5")]
        static void DoCreateEmptyScript() => EditorApplication.ExecuteMenuItem("Assets/Create/Scripting/Empty C# Script");
        
        
        
        //Хоткеи настроек
        [MenuItem("HotKey/Input System Settings _1")]
        static void DoOpenInputSystemSettings() => EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        
    }
}
