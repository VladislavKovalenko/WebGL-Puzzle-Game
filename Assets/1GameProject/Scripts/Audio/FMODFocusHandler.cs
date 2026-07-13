using FMODUnity;
using UnityEngine;

namespace _1GameProject.Scripts.Audio
{
    public class FMODFocusHandler : MonoBehaviour
    {
        private FMOD.Studio.Bus _masterBus;

        private void Start()
        {
            _masterBus = RuntimeManager.GetBus("bus:/");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetMute(!hasFocus);
        }

        private void OnApplicationPause(bool isPaused)
        {
            SetMute(isPaused);
        }

        private void SetMute(bool mute)
        {
            if (_masterBus.isValid())
            {
                _masterBus.setMute(mute);
            }
        }
    }
}
