using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace _1GameProject.Scripts.Audio
{
    public class FMODGameAudioManager : MonoBehaviour
    {
        private EventInstance musicInstance;
        private static SoundLibrarySO soundLibrary;

        private void Start()
        {
            if(soundLibrary == null) 
                soundLibrary = Resources.Load<SoundLibrarySO>("SoundLibrary");
        
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