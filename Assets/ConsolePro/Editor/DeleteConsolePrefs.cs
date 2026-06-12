// FILE: Assets/ConsolePro/Editor/DeleteConsolePrefs.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DeletePrefs
{
	[MenuItem("Tools/ConsolePro/Delete Console Pro Toggle Prefs")]
	public static void DeleteConsolePrefs()
	{
		if(EditorPrefs.HasKey("ConsolePro3ToggleDict"))
		{
			Debug.Log("Current Console Pro Toggle Prefs: " +
			          EditorPrefs.GetString("ConsolePro3ToggleDict"));
			Debug.Log("Deleting Console Pro Toggle Prefs...");
			EditorPrefs.DeleteKey("ConsolePro3ToggleDict");
			Debug.Log("Console Pro Toggle Prefs deleted.");
		}
		else
		{
			Debug.Log("Console Pro Toggle Prefs not found. Nothing to delete.");
		}
	}
}
#endif