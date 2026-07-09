// FILE: Scripts/GameFlow/Main_Menu/Levels_Menu/LevelsModel.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YG;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{
    public class LevelsModel
    {
        public List<int> UnlockedLevels => YG2.saves.unlockedLevels;
        public List<int> CompletedLevels => YG2.saves.completedLevels;

        public bool IsLevelUnlocked(int levelIndex) => UnlockedLevels.Contains(levelIndex);
        public bool IsLevelCompleted(int levelIndex) => CompletedLevels.Contains(levelIndex);

        // Игрок победил
        public void CompleteLevel(int levelIndex)
        {
            if (!CompletedLevels.Contains(levelIndex))
            {
                CompletedLevels.Add(levelIndex);
                YG2.SaveProgress();
            }
        }

        // Вызывается после победы. Открывает 2 случайных уровня.
        public void UnlockTwoRandomLevels()
        {
            bool changed = false;

            // 1. Ищем закрытые ЛЕГКИЕ уровни (с 2 по 10)
            var easyPool = Enumerable.Range(2, 9).Where(id => !UnlockedLevels.Contains(id)).ToList();
            if (easyPool.Count > 0)
            {
                int randomEasy = easyPool[Random.Range(0, easyPool.Count)];
                UnlockedLevels.Add(randomEasy);
                changed = true;
                Debug.Log($"[LevelsModel] Открыт легкий уровень: {randomEasy}");
            }

            // 2. Ищем закрытые СЛОЖНЫЕ уровни (с 11 по 30)
            var hardPool = Enumerable.Range(11, 20).Where(id => !UnlockedLevels.Contains(id)).ToList();
            if (hardPool.Count > 0)
            {
                int randomHard = hardPool[Random.Range(0, hardPool.Count)];
                UnlockedLevels.Add(randomHard);
                changed = true;
                Debug.Log($"[LevelsModel] Открыт сложный уровень: {randomHard}");
            }

            if (changed) YG2.SaveProgress();
        }

        // Игрок проиграл всё здоровье
        public void ResetAllProgress()
        {
            UnlockedLevels.Clear();
            UnlockedLevels.Add(1); // Оставляем только 1-й
            
            CompletedLevels.Clear();
            
            YG2.SaveProgress();
            Debug.Log("[LevelsModel] ПРОГРЕСС СБРОШЕН! Начинаем заново.");
        }
    }
}