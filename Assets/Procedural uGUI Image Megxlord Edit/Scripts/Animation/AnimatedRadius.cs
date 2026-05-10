using UnityEngine;
using UnityEngine.UI.ProceduralImage;

namespace UnityEngine.UI.ProceduralImage
{
    public class AnimatedRadius : ProceduralImageAnimation
    {
        [SerializeField] private float targetRadius = 50f;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private float timer;
        private float startRadius;
        private bool isAnimating;
        private FreeModifier freeModifier;

        void Start()
        {
            freeModifier = GetComponent<FreeModifier>();
        }

        public void AnimateTo(float radius)
        {
            targetRadius = radius;
            startRadius = GetCurrent();
            timer = 0f;
            isAnimating = true;
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

        void Update()
        {
            if (!isAnimating) return;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float val = Mathf.Lerp(startRadius, targetRadius, curve.Evaluate(t));
            SetRadius(val);
            if (t >= 1f) isAnimating = false;
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
    }
}