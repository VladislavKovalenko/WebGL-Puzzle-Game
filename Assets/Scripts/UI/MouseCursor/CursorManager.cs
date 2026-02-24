using UnityEngine;
using Cursor = UnityEngine.Cursor;

namespace UI.Objects
{
    public enum CursorState
    {
        Hover,
        Default
    }

    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance;
    
        public Texture2D defaultCursor;
        public Texture2D hoverCursor;
        
        //фикс проблемы с отклонением интерактивной области от видимой
        public Vector2 defaultCursorHotspot = Vector2.zero;
        public Vector2 hoverCursorHotspot = Vector2.zero;

        private int _hoverRequests;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyDefault();
            
            if (defaultCursorHotspot == Vector2.zero && defaultCursor != null)
                defaultCursorHotspot = new Vector2(defaultCursor.width / 2f, defaultCursor.height / 2f);
            
            if (hoverCursorHotspot == Vector2.zero && hoverCursor != null)
                hoverCursorHotspot = new Vector2(hoverCursor.width / 2f, hoverCursor.height / 2f);
        }

        public void HoverRequest(CursorState state)
            {
                if (state == CursorState.Hover)
                    _hoverRequests++;

                UpdateCursor();
            }

        public void HoverRelease(CursorState state)
        {
            if (state == CursorState.Hover)
                _hoverRequests = Mathf.Max(0, _hoverRequests - 1);
            
            UpdateCursor();
        }
        
        private void UpdateCursor()
        {
            if (_hoverRequests > 0)
                ApplyHover();
            else
                ApplyDefault();
        }

        
        private void ApplyHover()
        {
            Cursor.SetCursor(hoverCursor,hoverCursorHotspot, CursorMode.Auto);
        }

        private void ApplyDefault()
        {
            Cursor.SetCursor(defaultCursor,defaultCursorHotspot, CursorMode.Auto);
        }
        

    
    }
}