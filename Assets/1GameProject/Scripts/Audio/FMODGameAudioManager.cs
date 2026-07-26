using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.Audio
{
    public class FMODGameAudioManager : MonoBehaviour
    {
        private EventInstance musicInstance;
        [Inject] private SoundLibrarySO soundLibrary;
        
        private void Start()
        {
            // Проверка: назначена ли вообще музыка в SO?
            if (soundLibrary == null || soundLibrary.mainMenu.IsNull)
            {
                Debug.LogError("❌ [FMODGameAudioManager] В SoundLibrarySO не назначен ивент mainMenu!");
                return;
            }

            musicInstance = RuntimeManager.CreateInstance(soundLibrary.mainMenu);
            
            // Проверка: загрузился ли ивент из банка FMOD?
            if (!musicInstance.isValid())
            {
                Debug.LogError("❌ [FMODGameAudioManager] FMOD не смог создать EventInstance. Банк не загружен или ивента нет в банке!");
                return;
            }

            musicInstance.start();
            Debug.Log("✅ [FMODGameAudioManager] Музыка главного меню запущена!");
        }

        private void OnDestroy()
        {
            if (musicInstance.isValid())
            {
                musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                musicInstance.release();
            }
        }
    }
}