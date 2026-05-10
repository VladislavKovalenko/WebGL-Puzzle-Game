using UnityEngine;

namespace UnityEngine.UI.ProceduralImage
{
    [RequireComponent(typeof(ProceduralImage))]
    public abstract class ProceduralImageAnimation : MonoBehaviour
    {
        protected ProceduralImage proceduralImage;
        protected ProceduralImageModifier modifier;

        protected virtual void Awake()
        {
            proceduralImage = GetComponent<ProceduralImage>();
            modifier = GetComponent<ProceduralImageModifier>();
        }
    }
}