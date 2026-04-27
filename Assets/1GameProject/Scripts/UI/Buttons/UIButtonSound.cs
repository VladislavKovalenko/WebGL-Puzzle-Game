using System;
using _1GameProject.Scripts.Audio;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _1GameProject.Scripts.UI.Buttons
{
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private IDisposable DContainter;
        
        private bool IsHovered = false;
        private static SoundLibrarySO soundLibrary;
        public ReactiveProperty<bool> IsHover;
        
        private readonly Subject<bool> OnSimpleR3Event = new();
        

        private void Start()
        {
            var d = DContainter;
            if(soundLibrary == null) 
                soundLibrary = Resources.Load<SoundLibrarySO>("Sound/SoundLibrary");
            
            IsHover = new ReactiveProperty<bool>(IsHovered);
            
            OnSimpleR3Event.OnNext(IsHovered);
            
            DContainter = Observable.Interval(TimeSpan.FromMilliseconds(250)).Subscribe(_=>Text()).AddTo(this);
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