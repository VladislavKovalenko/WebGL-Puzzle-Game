using UnityEngine;
using UnityEngine.EventSystems;

namespace _1GameProject.Scripts.Audio
{
    // Обязательно наследуем интерфейсы, иначе методы не вызовутся
    public class GameLetterSounds : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
    {
        private SoundLibrarySO _soundLibrary;
        
        public void Init(SoundLibrarySO soundLibrary)
        {
            _soundLibrary = soundLibrary;
        }

        public void OnPointerEnter(PointerEventData eventData) 
        {
            if (_soundLibrary != null)
                _soundLibrary.PlayOneShot(_soundLibrary.letterHover);
        }

        public void OnPointerDown(PointerEventData eventData) 
        {
            // Используем Down, так как с него начинается свайп слова
            if (_soundLibrary != null)
                _soundLibrary.PlayOneShot(_soundLibrary.letterSelect);
        }
    }
}