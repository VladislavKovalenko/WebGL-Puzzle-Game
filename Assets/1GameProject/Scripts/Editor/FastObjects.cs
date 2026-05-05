using UnityEditor;

namespace Corowan.Editor
{
    public class FastObjects
    {
        //Асмдефы
        [MenuItem("FastObjects/Make Asmdef ")]
        static void DoCreateASMDEF() => EditorApplication.ExecuteMenuItem("Assets/Create/Scripting/Assembly Definition");
        
        [MenuItem("FastObjects/Make Asmdef Ref ")]
        static void DoCreateASMDEFRef() => EditorApplication.ExecuteMenuItem("Assets/Create/Scripting/Assembly Definition Reference");
        
        
        
    }
}