using FMODUnity;
using UnityEngine;
using YG;

namespace _1GameProject.Scripts.Audio
{
    public class FMODFocusHandler : MonoBehaviour
    {
        private FMOD.Studio.Bus _masterBus;
        private bool _isCurrentlyPaused = false;

        private void Start()
        {
            _masterBus = RuntimeManager.GetBus("bus:/");
            
            YG2.onPauseGame += OnYGPauseGame;
            YG2.onHideWindowGame += OnYGHideWindow;
            YG2.onShowWindowGame += OnYGShowWindow;
        }

        private void OnDestroy()
        {
            YG2.onPauseGame -= OnYGPauseGame;
            YG2.onHideWindowGame -= OnYGHideWindow;
            YG2.onShowWindowGame -= OnYGShowWindow;
        }

        private void OnYGPauseGame(bool isPause) => ApplyAudioState(isPause);
        private void OnYGHideWindow() => ApplyAudioState(true);
        private void OnYGShowWindow() => ApplyAudioState(false);

        private void OnApplicationFocus(bool hasFocus) => ApplyAudioState(!hasFocus);
        private void OnApplicationPause(bool isPaused) => ApplyAudioState(isPaused);

        private void ApplyAudioState(bool pause)
        {
            if (_isCurrentlyPaused == pause) return; 
            _isCurrentlyPaused = pause;

            if (pause)
            {
                FMODUnity.RuntimeManager.CoreSystem.mixerSuspend();
                
                if (_masterBus.isValid())
                    _masterBus.setPaused(true);
            }
            else
            {
                FMODUnity.RuntimeManager.CoreSystem.mixerResume();
                
                if (_masterBus.isValid())
                    _masterBus.setPaused(false);
            }
        }
    }
}
