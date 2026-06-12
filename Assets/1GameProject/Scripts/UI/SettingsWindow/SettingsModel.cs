using YG;

namespace _1GameProject.Scripts.UI.SettingsWindow
{
    public class SettingsModel
    {
        // Константы дефолтных настроек
        public const int DefaultFps = 60;
        public const int DefaultVolume = 50;

        // Свойства для быстрого доступа к сохранениям
        public int CurrentFps => YG2.saves.fpsCount;
        public int CurrentVolume => YG2.saves.gameVolume;

        public void SaveSettings(int fps, int volume)
        {
            // Обновляем данные в оперативной памяти
            YG2.saves.fpsCount = fps;
            YG2.saves.gameVolume = volume;
            
            // Отправляем в локальное хранилище браузера или облако Яндекса
            YG2.SaveProgress();
        }
    }
}