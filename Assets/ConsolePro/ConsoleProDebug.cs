// FILE: Assets/ConsolePro/ConsoleProDebug.cs
using UnityEngine;
using System;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

public static class ConsoleProDebug
{
    // -------------------------------------------------------
    // Публичный API
    // -------------------------------------------------------

    /// <summary>
    /// Очистить консоль
    /// </summary>
    public static void Clear()
    {
#if UNITY_EDITOR
        try
        {
            var method = ConsoleClearMethod;
            if (method != null)
            {
                method.Invoke(null, null);
            }
            else
            {
                // Фолбэк: очистка через стандартный Unity API
                ClearNativeConsole();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ConsoleProDebug] Clear() ошибка: {ex.Message}");
        }
#endif
    }

    /// <summary>
    /// Отправить лог в конкретный фильтр ConsolePro
    /// </summary>
    public static void LogToFilter(
        string inLog,
        string inFilterName,
        UnityEngine.Object inContext = null)
    {
        Debug.Log(
            inLog + "\nCPAPI:{\"cmd\":\"Filter\", \"name\":\"" + inFilterName + "\"}",
            inContext
        );
    }

    /// <summary>
    /// Отправить лог с изменённым типом в ConsolePro
    /// </summary>
    public static void LogAsType(
        string inLog,
        string inTypeName,
        UnityEngine.Object inContext = null)
    {
        Debug.Log(
            inLog + "\nCPAPI:{\"cmd\":\"LogType\", \"name\":\"" + inTypeName + "\"}",
            inContext
        );
    }

    /// <summary>
    /// Следить за переменной — один лог вместо спама
    /// </summary>
    public static void Watch(string inName, string inValue)
    {
        Debug.Log(
            inName + " : " + inValue +
            "\nCPAPI:{\"cmd\":\"Watch\", \"name\":\"" + inName + "\"}"
        );
    }

    /// <summary>
    /// Поиск по консоли
    /// </summary>
    public static void Search(string inText)
    {
        Debug.Log(
            "\nCPAPI:{\"cmd\":\"Search\", \"text\":\"" + inText + "\"}"
        );
    }

    // -------------------------------------------------------
    // Внутренняя реализация (только Editor)
    // -------------------------------------------------------

#if UNITY_EDITOR

    #region Clear Console

    private static void ClearNativeConsole()
    {
        try
        {
            var logEntriesType = FindType("UnityEditor.LogEntries");
            if (logEntriesType == null) return;

            // Unity < 2017: "Clear" / Unity 2017-5: "Clear" / Unity 6: "Clear"
            var clearMethod = logEntriesType.GetMethod(
                "Clear",
                BindingFlags.Static | BindingFlags.Public
            );

            clearMethod?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ConsoleProDebug] ClearNativeConsole ошибка: {ex.Message}");
        }
    }

    #endregion

    #region ConsolePro Window Reflection

    private static bool _checkedConsoleClearMethod = false;
    private static MethodInfo _consoleClearMethod = null;

    private static MethodInfo ConsoleClearMethod
    {
        get
        {
            if (_consoleClearMethod == null && !_checkedConsoleClearMethod)
            {
                _checkedConsoleClearMethod = true;
                var windowType = ConsoleWindowType;
                if (windowType == null) return null;

                // Пробуем найти метод очистки
                _consoleClearMethod =
                    windowType.GetMethod("ClearEntries",
                        BindingFlags.Static | BindingFlags.Public) ??
                    windowType.GetMethod("Clear",
                        BindingFlags.Static | BindingFlags.Public) ??
                    windowType.GetMethod("ClearConsole",
                        BindingFlags.Static | BindingFlags.Public);
            }
            return _consoleClearMethod;
        }
    }

    private static bool _checkedConsoleWindowType = false;
    private static Type _consoleWindowType = null;

    private static Type ConsoleWindowType
    {
        get
        {
            if (_consoleWindowType == null && !_checkedConsoleWindowType)
            {
                _checkedConsoleWindowType = true;

                // Ищем окно ConsolePro3
                _consoleWindowType = FindType("FlyingWormConsole3.ConsolePro3Window")
                    ?? FindType("ConsolePro3Window");
            }
            return _consoleWindowType;
        }
    }

    #endregion

    #region Helpers

    private static Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            catch { /* пропускаем */ }
        }
        return null;
    }

    #endregion

#endif
}