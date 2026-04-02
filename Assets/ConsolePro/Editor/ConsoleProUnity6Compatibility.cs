#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlyingWormConsole3Compatibility
{
	[InitializeOnLoad]
	internal static class ConsoleProUnity6Compatibility
	{
		private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		private static bool s_Applied;
		private static int s_Attempts;

		static ConsoleProUnity6Compatibility()
		{
			EditorApplication.update += TryApplyPatch;
		}

		private static void TryApplyPatch()
		{
			if(s_Applied)
			{
				EditorApplication.update -= TryApplyPatch;
				return;
			}

			s_Attempts++;
			if(s_Attempts > 120)
			{
				EditorApplication.update -= TryApplyPatch;
				return;
			}

			try
			{
				Type nativeLogsType = FindType("FlyingWormConsole3.NativeLogs");
				if(nativeLogsType == null)
				{
					return;
				}

				MethodInfo initMethod = nativeLogsType.GetMethod("Init", StaticFlags);
				initMethod?.Invoke(null, null);

				FieldInfo instanceIdFieldField = nativeLogsType.GetField("_logEntryInstanceIDField", StaticFlags);
				FieldInfo identifierFieldField = nativeLogsType.GetField("_logEntryIdentifierField", StaticFlags);
				if(instanceIdFieldField == null || identifierFieldField == null)
				{
					return;
				}

				FieldInfo instanceIdField = instanceIdFieldField.GetValue(null) as FieldInfo;
				if(instanceIdField != null)
				{
					s_Applied = true;
					EditorApplication.update -= TryApplyPatch;
					return;
				}

				FieldInfo identifierField = identifierFieldField.GetValue(null) as FieldInfo;
				if(identifierField == null)
				{
					return;
				}

				instanceIdFieldField.SetValue(null, identifierField);

				MethodInfo resetGrabbedMethod = nativeLogsType.GetMethod("ResetGrabbed", StaticFlags);
				resetGrabbedMethod?.Invoke(null, null);

				FieldInfo endGettingEntriesFuncField = nativeLogsType.GetField("_endGettingEntriesFunc", StaticFlags);
				if(endGettingEntriesFuncField?.GetValue(null) is Action endGettingEntries)
				{
					try
					{
						endGettingEntries();
					}
					catch
					{
						// Ignore stale state mismatches; the next refresh will re-enter cleanly.
					}
				}

				s_Applied = true;
				EditorApplication.update -= TryApplyPatch;
				Debug.Log("Console Pro Unity 6 compatibility patch applied.");
			}
			catch
			{
				// Keep retrying during startup while Unity loads editor assemblies.
			}
		}

		private static Type FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for(int i = 0; i < assemblies.Length; i++)
			{
				Type type = assemblies[i].GetType(fullName, false);
				if(type != null)
				{
					return type;
				}
			}

			return null;
		}
	}
}
#endif
