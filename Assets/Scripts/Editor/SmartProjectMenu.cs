using UnityEditor;
using UnityEngine;

public class SmartProjectMenu
{
    
    [MenuItem("Analyze/Frame Debugger")]
    static void FrameDebuggerCommand1() => EditorApplication.ExecuteMenuItem("Window/Analysis/Frame Debugger");
    
    [MenuItem("Analyze/Render Graph Viewer")]
    static void RenderGraphViewer() => EditorApplication.ExecuteMenuItem("Window/Analysis/Render Graph Viewer");
    
    [MenuItem("Analyze/Rendering Debugger")]
    static void RenderingDebugger() => EditorApplication.ExecuteMenuItem("Window/Analysis/Rendering Debugger");
}
