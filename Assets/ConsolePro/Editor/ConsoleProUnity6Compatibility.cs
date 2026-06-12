// FILE: Assets/ConsolePro/Editor/ConsoleProUnity6Compatibility.cs
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
		private const BindingFlags StaticFlags =
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static bool s_Applied;
		private static int s_Attempts;

		static ConsoleProUnity6Compatibility()
		{
			EditorApplication.delayCall += TryApplyPatchImmediate;
			EditorApplication.update += TryApplyPatch;
		}

		private static void TryApplyPatchImmediate()
		{
			TryApplyPatchInternal();
		}

		private static void TryApplyPatch()
		{
			if(s_Applied)
			{
				EditorApplication.update -= TryApplyPatch;
				return;
			}

			s_Attempts++;
			if(s_Attempts > 300)
			{
				EditorApplication.update -= TryApplyPatch;
				Debug.LogError("[ConsolePro Compat] Патч не применён за 300 попыток. " +
				               "Проверьте наличие ConsolePro.Editor.dll");
				return;
			}

			TryApplyPatchInternal();
		}

		private static void TryApplyPatchInternal()
		{
			if(s_Applied)
			{
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
				if(initMethod == null)
				{
					Debug.LogError("[ConsolePro Compat] Метод Init не найден!");
					return;
				}

				initMethod.Invoke(null, null);

				FieldInfo instanceIdFieldHolder =
					nativeLogsType.GetField("_logEntryInstanceIDField", StaticFlags);
				FieldInfo identifierFieldHolder =
					nativeLogsType.GetField("_logEntryIdentifierField", StaticFlags);

				if(instanceIdFieldHolder == null)
				{
					Debug.LogError("[ConsolePro Compat] _logEntryInstanceIDField не найден в NativeLogs!");
					return;
				}

				if(identifierFieldHolder == null)
				{
					Debug.LogError("[ConsolePro Compat] _logEntryIdentifierField не найден в NativeLogs!");
					return;
				}

				FieldInfo currentInstanceIdField =
					instanceIdFieldHolder.GetValue(null) as FieldInfo;

				if(currentInstanceIdField != null)
				{
					Debug.Log("[ConsolePro Compat] _logEntryInstanceIDField уже заполнен: " +
					          $"{currentInstanceIdField.Name}. Патч не нужен.");
					s_Applied = true;
					EditorApplication.update -= TryApplyPatch;
					return;
				}

				FieldInfo identifierField =
					identifierFieldHolder.GetValue(null) as FieldInfo;

				if(identifierField == null)
				{
					return;
				}

				instanceIdFieldHolder.SetValue(null, identifierField);

				Debug.Log($"[ConsolePro Compat] Подмена выполнена: " +
				          $"_logEntryInstanceIDField = " +
				          $"{identifierField.DeclaringType?.Name}.{identifierField.Name} " +
				          $"({identifierField.FieldType.Name})");

				MethodInfo resetGrabbedMethod =
					nativeLogsType.GetMethod("ResetGrabbed", StaticFlags);

				if(resetGrabbedMethod != null)
				{
					resetGrabbedMethod.Invoke(null, null);
					Debug.Log("[ConsolePro Compat] ResetGrabbed вызван.");
				}
				else
				{
					Debug.LogWarning("[ConsolePro Compat] ResetGrabbed не найден — пропускаем.");
				}

				FieldInfo endFuncField =
					nativeLogsType.GetField("_endGettingEntriesFunc", StaticFlags);

				if(endFuncField?.GetValue(null) is Action endGettingEntries)
				{
					try
					{
						endGettingEntries();
						Debug.Log("[ConsolePro Compat] EndGettingEntries вызван принудительно.");
					}
					catch(Exception ex)
					{
						Debug.LogWarning($"[ConsolePro Compat] EndGettingEntries исключение (ожидаемо): {ex.Message}");
					}
				}

				s_Applied = true;
				EditorApplication.update -= TryApplyPatch;
				Debug.Log("[ConsolePro Compat] Патч успешно применён!");
			}
			catch(Exception ex)
			{
				if(s_Attempts > 10)
				{
					Debug.LogWarning($"[ConsolePro Compat] Исключение на попытке {s_Attempts}: {ex.Message}");
				}
			}
		}

		private static Type FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for(int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					Type type = assemblies[i].GetType(fullName, false);
					if(type != null)
					{
						return type;
					}
				}
				catch
				{
					// пропускаем проблемные сборки
				}
			}

			return null;
		}
	}
}
#endif