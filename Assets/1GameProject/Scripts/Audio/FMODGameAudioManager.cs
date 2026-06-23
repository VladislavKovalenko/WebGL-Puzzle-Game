using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.Audio
{
    public class FMODGameAudioManager : MonoBehaviour
    {
        private EventInstance musicInstance;
        [Inject] SoundLibrarySO soundLibrary;

        private void Start()
        {
            musicInstance = RuntimeManager.CreateInstance(soundLibrary.mainMenu);
            musicInstance.start();
        }

        void OnDestroy()
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
        
    }
}