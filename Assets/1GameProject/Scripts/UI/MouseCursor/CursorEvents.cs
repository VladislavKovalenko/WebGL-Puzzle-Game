using UI.Objects;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.MouseCursor
{
    [RequireComponent (typeof (UIDocument))]
    public class CursorEvents : MonoBehaviour
    {
        private const string InteractiveClassTag = "cursorIsInteractive";

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
        
            root.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
        
        }

        private void OnPointerEnter (PointerEnterEvent evt)
        {
            if (evt.target is VisualElement ve && ve.ClassListContains(InteractiveClassTag))
                CursorManager.Instance.HoverRequest(CursorState.Hover);
        }

        private void OnPointerLeave (PointerLeaveEvent evt)
        {
            if (evt.target is VisualElement ve && ve.ClassListContains(InteractiveClassTag))
            {
                CursorManager.Instance.HoverRelease(CursorState.Hover);
            }
        }
    
    
    }
}
