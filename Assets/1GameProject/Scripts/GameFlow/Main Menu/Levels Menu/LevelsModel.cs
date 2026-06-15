using UnityEngine;
using YG;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{
    public class LevelsModel
    {
        // Быстрый доступ к сохранению Яндекса
        public int MaxUnlockedLevel => YG2.saves.maxUnlockedLevel;

        // Метод, который мы вызовем, когда игрок победит в уровне
        public void CompleteLevel(int completedLevelIndex)
        {
            // Если пройденный уровень равен максимальному открытому, 
            // значит мы прошли самый последний доступный уровень -> открываем следующий
            if (completedLevelIndex >= MaxUnlockedLevel)
            {
                YG2.saves.maxUnlockedLevel = completedLevelIndex + 1;
                YG2.SaveProgress();
                
                Debug.Log($"[LevelsModel] Уровень {completedLevelIndex} пройден! Открыт уровень {YG2.saves.maxUnlockedLevel}");
            }
        }

        // Проверка, доступен ли уровень для запуска
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= MaxUnlockedLevel;
        }
    }
}