using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace MegxlordTools.Editor
{
    public static class GameScreenRenderClipboardPNG
    {
        [MenuItem("Tools/Megxlord Toolbox/Tools/Game Screen Render Clipboard PNG")]
        private static void Execute()
        {
#if UNITY_EDITOR_WIN
            EditorWindow gameView = FindGameViewWindow();
            if (gameView == null)
            {
                Debug.LogWarning("[GameScreenRenderClipboard] Game View не найден. Откройте вкладку Game.");
                return;
            }

            gameView.Focus();
            gameView.Repaint();

            // Иногда RT создаётся/обновляется не сразу
            EditorApplication.delayCall += () => CaptureAndCopy(gameView, 3);
#else
            Debug.LogWarning("[GameScreenRenderClipboard] Поддерживается только Windows.");
#endif
        }

#if UNITY_EDITOR_WIN

        private static void CaptureAndCopy(EditorWindow gameView, int retriesLeft)
        {
            Texture2D screenshot = CaptureFromGameViewTargetTexture(gameView);

            if (screenshot == null)
            {
                if (retriesLeft > 0)
                {
                    gameView.Repaint();
                    EditorApplication.delayCall += () => CaptureAndCopy(gameView, retriesLeft - 1);
                    return;
                }

                Debug.LogError("[GameScreenRenderClipboard] Не удалось получить RenderTexture Game View.");
                return;
            }

            byte[] pngBytes = screenshot.EncodeToPNG();
            CopyToClipboard(pngBytes, screenshot);

            Debug.Log($"[GameScreenRenderClipboard] Готово: {screenshot.width}x{screenshot.height}, PNG {pngBytes.Length / 1024} KB");
            UnityEngine.Object.DestroyImmediate(screenshot);
        }

        // ─── Захват напрямую из RenderTexture Game View ──────────────────────

        private static Texture2D CaptureFromGameViewTargetTexture(EditorWindow gameView)
        {
            RenderTexture rt = GetGameViewTargetTexture(gameView);
            if (rt == null)
                return null;

            RenderTexture prev = RenderTexture.active;

            try
            {
                RenderTexture.active = rt;

                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                tex.Apply(false, false);

                return tex;
            }
            finally
            {
                RenderTexture.active = prev;
            }
        }

        private static RenderTexture GetGameViewTargetTexture(EditorWindow gameView)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] propertyNames =
            {
                "targetTexture",
                "renderTexture"
            };

            string[] fieldNames =
            {
                "m_TargetTexture",
                "m_RenderTexture"
            };

            Type t = gameView.GetType();
            while (t != null)
            {
                foreach (string propName in propertyNames)
                {
                    PropertyInfo prop = t.GetProperty(propName, flags);
                    if (prop != null && typeof(RenderTexture).IsAssignableFrom(prop.PropertyType))
                    {
                        if (prop.GetValue(gameView) is RenderTexture rt && rt != null)
                            return rt;
                    }
                }

                foreach (string fieldName in fieldNames)
                {
                    FieldInfo field = t.GetField(fieldName, flags);
                    if (field != null && typeof(RenderTexture).IsAssignableFrom(field.FieldType))
                    {
                        if (field.GetValue(gameView) is RenderTexture rt && rt != null)
                            return rt;
                    }
                }

                t = t.BaseType;
            }

            return null;
        }

        // ─── Буфер обмена ─────────────────────────────────────────────────────

        private static void CopyToClipboard(byte[] pngBytes, Texture2D dibSource)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogWarning("[GameScreenRenderClipboard] Не удалось открыть Clipboard.");
                return;
            }

            try
            {
                EmptyClipboard();

                // PNG — современные приложения
                uint pngFormat = RegisterClipboardFormat("PNG");
                if (pngFormat != 0)
                {
                    IntPtr hPng = GlobalAlloc(GmemMoveable, new UIntPtr((uint)pngBytes.Length));
                    if (hPng != IntPtr.Zero)
                    {
                        IntPtr ptr = GlobalLock(hPng);
                        if (ptr != IntPtr.Zero)
                        {
                            Marshal.Copy(pngBytes, 0, ptr, pngBytes.Length);
                            GlobalUnlock(hPng);
                            SetClipboardData(pngFormat, hPng);
                        }
                    }
                }

                // DIB — старые приложения
                CopyDibToClipboard(dibSource);
            }
            finally
            {
                CloseClipboard();
            }
        }

        private static void CopyDibToClipboard(Texture2D texture)
        {
            int w = texture.width;
            int h = texture.height;
            Color32[] pixels = texture.GetPixels32();

            int rowSize = ((w * 3 + 3) / 4) * 4;
            int imgSize = rowSize * h;

            IntPtr hDib = GlobalAlloc(GmemMoveable, new UIntPtr((uint)(40 + imgSize)));
            if (hDib == IntPtr.Zero) return;

            IntPtr ptr = GlobalLock(hDib);
            if (ptr == IntPtr.Zero) return;

            byte[] header = new byte[40];
            BitConverter.GetBytes(40u).CopyTo(header, 0);
            BitConverter.GetBytes(w).CopyTo(header, 4);
            BitConverter.GetBytes(h).CopyTo(header, 8); // positive => bottom-up DIB
            BitConverter.GetBytes((ushort)1).CopyTo(header, 12);
            BitConverter.GetBytes((ushort)24).CopyTo(header, 14);
            BitConverter.GetBytes(0u).CopyTo(header, 16);
            BitConverter.GetBytes((uint)imgSize).CopyTo(header, 20);
            Marshal.Copy(header, 0, ptr, 40);

            byte[] row = new byte[rowSize];

            for (int y = 0; y < h; y++)
            {
                Array.Clear(row, 0, row.Length);

                // ВАЖНО:
                // Unity GetPixels32() уже идёт bottom-up,
                // а CF_DIB при positive height тоже bottom-up.
                int srcRow = y;

                for (int x = 0; x < w; x++)
                {
                    Color32 c = pixels[srcRow * w + x];
                    row[x * 3 + 0] = c.b;
                    row[x * 3 + 1] = c.g;
                    row[x * 3 + 2] = c.r;
                }

                Marshal.Copy(row, 0, IntPtr.Add(ptr, 40 + y * rowSize), rowSize);
            }

            GlobalUnlock(hDib);
            SetClipboardData(CfDib, hDib);
        }

        // ─── Поиск окна Game View ─────────────────────────────────────────────

        private static EditorWindow FindGameViewWindow()
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return null;

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows != null && windows.Length > 0)
                return windows[0] as EditorWindow;

            return EditorWindow.GetWindow(gameViewType, false, "Game", false);
        }

        // ─── WinAPI ───────────────────────────────────────────────────────────

        private const uint CfDib = 8;
        private const uint GmemMoveable = 0x0002;

        [DllImport("user32.dll")] private static extern uint RegisterClipboardFormat(string name);
        [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool EmptyClipboard();
        [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint fmt, IntPtr hMem);
        [DllImport("user32.dll")] private static extern bool CloseClipboard();

        [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
        [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr hMem);

#endif
    }
}