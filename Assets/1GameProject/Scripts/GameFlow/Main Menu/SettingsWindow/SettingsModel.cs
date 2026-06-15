using FMOD.Studio;
using YG;
using Zenject;
using UnityEngine;

namespace _1GameProject.Scripts.UI.SettingsWindow
{
    public class SettingsModel : IInitializable
    {
        // Константы дефолтных настроек
        public const int DefaultFps = 60;
        public const int DefaultVolume = 50;

        // Свойства для быстрого доступа к сохранениям
        // Если в сохранении Яндекса лежит 0 (например, баг или первый запуск), 
        // мы принудительно выдадим дефолтное значение (60 и 50).
        public int CurrentFps => YG2.saves.fpsCount < 30 ? DefaultFps : YG2.saves.fpsCount;
        public int CurrentVolume => YG2.saves.gameVolume == 0 ? DefaultVolume : YG2.saves.gameVolume;
        
        // Кэшируем VCA для оптимизации
        private VCA _masterVCA;
        
        public void Initialize()
        {
            // 1. ЗАЩИТА: Если в сохранении лежит некорректный FPS (например, старый сейв с нулем)
            // Мы автоматически чиним сохранение, сбрасывая его на дефолтные значения
            if (YG2.saves.fpsCount < 30)
            {
                YG2.saves.fpsCount = DefaultFps;
                YG2.saves.gameVolume = DefaultVolume;
                
                // Перезаписываем битый файл сохранений
                YG2.SaveProgress();
                
                Debug.Log("[Settings] Обнаружено старое сохранение. Настройки сброшены по умолчанию.");
            }

            // 2. Получаем VCA один раз при старте игры
            _masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
            
            // 3. Применяем сохраненные (или только что починенные) настройки
            ApplyPhysicalSettings(CurrentFps, CurrentVolume);
        }

        public void SaveSettings(int fps, int volume)
        {
            // Обновляем данные в оперативной памяти
            YG2.saves.fpsCount = fps;
            YG2.saves.gameVolume = volume;
            
            // Отправляем в локальное хранилище браузера или облако Яндекса
            YG2.SaveProgress();
            
            // Сразу же применяем их к игре
            ApplyPhysicalSettings(fps, volume);
            
        }
        
        // Метод для временного изменения (Real-time Preview)
        // Он НЕ сохраняет данные, просто меняет громкость в движке
        public void PreviewSettings(int tempFps, int tempVolume)
        {
            ApplyPhysicalSettings(tempFps, tempVolume);
        }

        // Метод отмены (возвращает всё как было)
        public void RevertSettingsToSaved()
        {
            ApplyPhysicalSettings(CurrentFps, CurrentVolume);
        }

        // Приватный метод, который физически меняет состояние игры
        private void ApplyPhysicalSettings(int targetFps, int targetVolume)
        {
            Application.targetFrameRate = targetFps;
            
            // Устанавливаем громкость через закэшированный VCA
            if (_masterVCA.isValid())
            {
                float fmodVolume = targetVolume / 100f;
                _masterVCA.setVolume(fmodVolume);
                Debug.Log($"[Settings] Применены настройки: FPS={CurrentFps}, Volume={CurrentVolume}");
            }
            
            
        }
    }
}