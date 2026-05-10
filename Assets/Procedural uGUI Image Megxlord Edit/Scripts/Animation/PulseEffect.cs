using UnityEngine;

namespace UnityEngine.UI.ProceduralImage
{
    public class PulseEffect : ProceduralImageAnimation
    {
        [SerializeField] private float baseRadius = 30f;
        [SerializeField] private float pulseAmount = 10f;
        [SerializeField] private float speed = 2f;

        private FreeModifier freeModifier;

        void Start()
        {
            freeModifier = GetComponent<FreeModifier>();
        }

        void Update()
        {
            float r = baseRadius + Mathf.Sin(Time.time * speed) * pulseAmount;
            r = Mathf.Max(0, r);
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