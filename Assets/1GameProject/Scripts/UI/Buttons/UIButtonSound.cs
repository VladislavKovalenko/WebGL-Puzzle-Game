using _1GameProject.Scripts.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _1GameProject.Scripts.UI.Buttons
{
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private static SoundLibrarySO soundLibrary;

        private void Start()
        {
            if(soundLibrary == null) 
                soundLibrary = Resources.Load<SoundLibrarySO>("SoundLibrary");
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            soundLibrary.PlayOneShot(soundLibrary.hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            soundLibrary.PlayOneShot(soundLibrary.click);
        }
    }
}