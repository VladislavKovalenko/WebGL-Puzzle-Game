using YG;
using Zenject;
using UnityEngine;
using FMODUnity;

namespace _1GameProject.Scripts.UI.SettingsWindow
{
    public class SettingsModel : IInitializable
    {
        public const int DefaultFps = 60;
        public const int DefaultVolume = 50;

        public int CurrentFps => YG2.saves.fpsCount;
        public int CurrentVolume => YG2.saves.gameVolume;

        public void Initialize()
        {
            ApplyPhysicalSettings(CurrentFps, CurrentVolume);
        }

        public void SaveSettings(int fps, int volume)
        {
            YG2.saves.fpsCount = fps;
            YG2.saves.gameVolume = volume;
            YG2.SaveProgress();

            ApplyPhysicalSettings(fps, volume);
        }

        public void PreviewSettings(int tempFps, int tempVolume)
        {
            ApplyPhysicalSettings(tempFps, tempVolume);
        }

        public void RevertSettingsToSaved()
        {
            ApplyPhysicalSettings(CurrentFps, CurrentVolume);
        }

        private void ApplyPhysicalSettings(int targetFps, int targetVolume)
        {
            Application.targetFrameRate = targetFps;

            FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");

            if (masterBus.isValid())
            {
                float fmodVolume = targetVolume / 100f;
                float curvedVolume = fmodVolume * fmodVolume;
                masterBus.setVolume(curvedVolume);
            }
            else
            {
                Debug.LogWarning("[Settings] Master Bus не найден. Возможно, банки FMOD еще грузятся.");
            }
        }
    }
}
