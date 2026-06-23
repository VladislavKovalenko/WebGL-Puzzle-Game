using FMODUnity;
using UnityEngine;

namespace _1GameProject.Scripts.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "SO/SoundLibrary")]
    public class SoundLibrarySO : ScriptableObject
    {
        [Header("Музыка")]
        public EventReference mainMenu;
        public EventReference game;
        
        [Header("Звуки кнопок UI")]
        [Tooltip("Звук при наведении курсора на кнопку")]
        public EventReference hover;
        public EventReference click;
        
        [Header("Звуки игрового процесса")]
        public EventReference successSound;
        public EventReference failSound;
        
        
        public void PlayOneShot(EventReference reference)
        {
            if (!reference.IsNull)
                RuntimeManager.PlayOneShot(reference);
            else
                Debug.LogWarning($"[UISoundSettings] EventReference is null: {name}", this);
        }
        
    }
}

// public void PlayHover()  => PlayOneShot(hover);
// public void PlayClick()  => PlayOneShot(click);
//
// private void PlayOneShot(EventReference reference)
// {
//     if (!reference.IsNull)
//         RuntimeManager.PlayOneShot(reference);
//     else
//         Debug.LogWarning($"[UISoundSettings] EventReference is null: {name}", this);
// }