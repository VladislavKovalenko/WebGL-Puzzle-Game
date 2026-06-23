using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace _1GameProject.Scripts.Audio
{
    public class UIButtonSoundService : MonoBehaviour
    {
        [Inject] private SoundLibrarySO _soundLibrary;
        private Canvas _canvas;

        void Start()
        {
            _canvas = Object.FindObjectOfType<Canvas>();

            foreach (var button in _canvas.GetComponentsInChildren<Button>(true))
            {

                var buttonSound = button.gameObject.AddComponent<UIButtonSound>();
                
                buttonSound.Init(_soundLibrary);
            }
        }

    }
}