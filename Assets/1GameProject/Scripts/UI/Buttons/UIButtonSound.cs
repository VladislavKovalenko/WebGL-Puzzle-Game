using System;
using _1GameProject.Scripts.Audio;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _1GameProject.Scripts.UI.Buttons
{
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private bool IsHovered = false;
        private static SoundLibrarySO soundLibrary;
        public ReactiveProperty<bool> IsHover;
        
        private readonly Subject<bool> OnSimpleR3Event = new();
        

        private void Start()
        {
            if(soundLibrary == null) 
                soundLibrary = Resources.Load<SoundLibrarySO>("Sound/SoundLibrary");
            
            IsHover = new ReactiveProperty<bool>(IsHovered);
            
            OnSimpleR3Event.OnNext(IsHovered);
            
            Observable.Interval(TimeSpan.FromMilliseconds(250)).Subscribe(_=>Text());
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            soundLibrary.PlayOneShot(soundLibrary.hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            soundLibrary.PlayOneShot(soundLibrary.click);

            IsHovered =  true;
        }

        private void Text()
        {
            Debug.Log("Habubu");
        }
        
    }
}