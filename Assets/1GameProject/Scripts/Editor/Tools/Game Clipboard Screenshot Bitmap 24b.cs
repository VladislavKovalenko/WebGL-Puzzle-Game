using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace MegxlordTools.Editor
{
    public static class GameScreenRenderClipboard
    {
        [MenuItem("Tools/Megxlord Tools/Game Screen Render Clipboard Bitmap")]
        private static void Execute()
        {
            Texture2D screenshot = CaptureFromGameView();

            if (screenshot == null)
            {
                Debug.LogWarning("[GameScreenRenderClipboard] Не удалось получить RenderTexture из Game View. " +
                    "Fallback на рендер камер. ВНИМАНИЕ: UI Screen Space — Overlay не будет захвачен!");
                screenshot = CaptureFromCameras();
            }

            if (screenshot == null) return;

            // Приводим к top-down порядку (как на экране), чтобы не зависеть от нюансов RenderTexture
            FlipTextureVertically(screenshot);

            CopyToClipboard(screenshot);
            Debug.Log($"[GameScreenRenderClipboard] Скриншот {screenshot.width}x{screenshot.height} скопирован в буфер обмена.");

            UnityEngine.Object.DestroyImmediate(screenshot);
        }

        private static Texture2D CaptureFromGameView()
        {
            RenderTexture gameViewRT = GetGameViewTargetTexture();
            if (gameViewRT == null || gameViewRT.width == 0 || gameViewRT.height == 0)
                return null;

            RepaintGameView();

            int width = gameViewRT.width;
            int height = gameViewRT.height;

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = gameViewRT;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            return tex;
        }

        private static RenderTexture GetGameViewTargetTexture()
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type playModeViewType = editorAssembly.GetType("UnityEditor.PlayModeView");
                if (playModeViewType == null) return null;

                MethodInfo getMainMethod = playModeViewType.GetMethod(
                    "GetMainPlayModeView",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (getMainMethod == null) return null;

                object playModeView = getMainMethod.Invoke(null, null);
                if (playModeView == null) return null;

                PropertyInfo targetTextureProp = playModeViewType.GetProperty(
                    "targetTexture",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (targetTextureProp != null)
                    return targetTextureProp.GetValue(playModeView) as RenderTexture;

                FieldInfo targetTextureField = playModeViewType.GetField(
                    "m_TargetTexture",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                return targetTextureField?.GetValue(playModeView) as RenderTexture;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static void RepaintGameView()
        {
            try
            {
                Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null) return;

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
                gameView?.Repaint();
            }
            catch { }
        }

        private static Texture2D CaptureFromCameras()
        {
            Vector2 gameViewSize = GetGameViewSize();
            int width = Mathf.Max(1, (int)gameViewSize.x);
            int height = Mathf.Max(1, (int)gameViewSize.y);

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            Camera[] cameras = Camera.allCameras
                .Where(c => c.enabled && c.gameObject.activeInHierarchy)
                .OrderBy(c => c.depth)
                .ToArray();

            if (cameras.Length == 0)
            {
                Debug.LogWarning("[GameScreenRenderClipboard] Активные камеры на сцене не найдены.");
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(rt);
                return null;
            }

            foreach (Camera cam in cameras)
            {
                RenderTexture prevTarget = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prevTarget;
            }

            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);

            return screenshot;
        }

        private static Vector2 GetGameViewSize()
        {
            Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            MethodInfo getSizeMethod = gameViewType?.GetMethod(
                "GetMainGameViewSize",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (getSizeMethod != null)
            {
                object result = getSizeMethod.Invoke(null, null);
                if (result is Vector2 size) return size;
            }

            return new Vector2(Screen.width, Screen.height);
        }

        private static void FlipTextureVertically(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            int w = tex.width;
            int h = tex.height;
            Color32[] flipped = new Color32[pixels.Length];

            for (int y = 0; y < h; y++)
                Array.Copy(pixels, (h - 1 - y) * w, flipped, y * w, w);

            tex.SetPixels32(flipped);
            tex.Apply();
        }

#if UNITY_EDITOR_WIN
        private static void CopyToClipboard(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int w = texture.width;
            int h = texture.height;
            int rowSize = ((w * 3 + 3) / 4) * 4;
            int imageSize = rowSize * h;
            int totalSize = 40 + imageSize;

            IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(uint)totalSize);
            if (hMem == IntPtr.Zero) return;

            IntPtr ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero) return;

            // BITMAPINFOHEADER — top-down DIB (отрицательная высота)
            byte[] header = new byte[40];
            BitConverter.GetBytes(40u).CopyTo(header, 0);
            BitConverter.GetBytes(w).CopyTo(header, 4);
            BitConverter.GetBytes(-h).CopyTo(header, 8);          // <-- отрицательная высота
            BitConverter.GetBytes((ushort)1).CopyTo(header, 12);
            BitConverter.GetBytes((ushort)24).CopyTo(header, 14);
            BitConverter.GetBytes((uint)imageSize).CopyTo(header, 20);
            Marshal.Copy(header, 0, ptr, 40);

            IntPtr pixelPtr = new IntPtr(ptr.ToInt64() + 40);
            byte[] row = new byte[rowSize];

            for (int y = 0; y < h; y++)
            {
                int srcY = y; // <-- без инверсии, т.к. DIB top-down и текстура уже перевернута
                for (int x = 0; x < w; x++)
                {
                    Color32 c = pixels[srcY * w + x];
                    row[x * 3 + 0] = c.b;
                    row[x * 3 + 1] = c.g;
                    row[x * 3 + 2] = c.r;
                }
                Marshal.Copy(row, 0, new IntPtr(pixelPtr.ToInt64() + y * rowSize), rowSize);
            }

            GlobalUnlock(hMem);

            if (!OpenClipboard(IntPtr.Zero)) return;
            EmptyClipboard();
            SetClipboardData(CF_DIB, hMem);
            CloseClipboard();
        }

        private const uint CF_DIB = 8;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();
#else
        private static void CopyToClipboard(Texture2D texture)
        {
            Debug.LogWarning("[GameScreenRenderClipboard] Копирование изображения в буфер обмена поддерживается только в редакторе под Windows.");
        }
#endif
    }
}