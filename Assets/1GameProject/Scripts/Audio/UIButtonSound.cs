using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace _1GameProject.Scripts.Audio
{
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        
        private SoundLibrarySO soundLibrary;
        

        public void OnPointerEnter(PointerEventData eventData) => soundLibrary.PlayOneShot(soundLibrary.hover);
        public void OnPointerClick(PointerEventData eventData) => soundLibrary.PlayOneShot(soundLibrary.click);
        
        public void Init(SoundLibrarySO soundLibrary)
        {
            this.soundLibrary = soundLibrary;
        }
        
    }
}