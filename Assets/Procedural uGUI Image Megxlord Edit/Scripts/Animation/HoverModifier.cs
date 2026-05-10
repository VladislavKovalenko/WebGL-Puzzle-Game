using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.ProceduralImage
{
    public class HoverModifier : ProceduralImageAnimation, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverRadius = 40f;
        [SerializeField] private float normalRadius = 20f;
        [SerializeField] private float transitionDuration = 0.2f;

        private FreeModifier freeModifier;
        private float currentVelocity;
        private float target;

        void Start()
        {
            freeModifier = GetComponent<FreeModifier>();
            target = normalRadius;
        }

        void Update()
        {
            float current = GetCurrent();
            float smooth = Mathf.SmoothDamp(current, target, ref currentVelocity, transitionDuration);
            SetRadius(smooth);
        }

        float GetCurrent()
        {
            if (freeModifier != null)
            {
                if (freeModifier.UniformRadius)
                    return freeModifier.UniformValue;
                return freeModifier.Radius.x;
            }
            return 0f;
        }

        void SetRadius(float r)
        {
            if (freeModifier != null)
            {
                if (freeModifier.UniformRadius)
                    freeModifier.UniformValue = r;
                else
                    freeModifier.Radius = new Vector4(r, r, r, r);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => target = hoverRadius;
        public void OnPointerExit(PointerEventData eventData) => target = normalRadius;
    }
}