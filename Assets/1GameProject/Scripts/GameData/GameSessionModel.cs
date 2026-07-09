// FILE: Scripts/GameData/GameSessionModel.cs

using _1GameProject.Scripts.GameData.SO;
using UniRx;
using UnityEngine;

namespace _1GameProject.Scripts.GameData
{
    public class GameSessionModel
    {
        public const int MaxLives = 5;
        public ReactiveProperty<int> GlobalLives { get; } = new(MaxLives);
        
        private readonly CampaignRouteSO _route;

        // Открыт доступ, чтобы Меню могло записать сюда выбранный уровень
        public int CurrentLevelIndex { get; set; } = 0; 
        
        public bool TookDamageThisLevel { get; set; } = false;
        
        public bool AutoOpenLevelsMenu { get; set; } = false;

        public GameSessionModel(CampaignRouteSO route)
        {
            _route = route;
        }

        public void StartNewRun()
        {
            GlobalLives.Value = MaxLives;
            ResetLevelFlags();
        }

        public void ResetLevelFlags() => TookDamageThisLevel = false;

        public void TakeDamage()
        {
            GlobalLives.Value--;
            TookDamageThisLevel = true;
        }

        public void Heal(int amount)
        {
            GlobalLives.Value = Mathf.Min(GlobalLives.Value + amount, MaxLives);
        }

        // Инсталлер вызывает это, чтобы получить готовую сетку
        public LevelConfig GetCurrentConfig()
        {
            CampaignLevel levelData = _route.Levels[CurrentLevelIndex];
            DifficultyTemplate diffSettings = _route.GetSettings(levelData.Difficulty);

            Debug.Log($"[GameSessionModel] Грузим уровень: {CurrentLevelIndex + 1}. Сложность: {levelData.Difficulty}, Угроза: {levelData.Hazard}, Сетка: {diffSettings.Cols}x{diffSettings.Rows}");

            if (diffSettings == null)
            {
                Debug.LogError($"[GameSessionModel] Шаблон сложности {levelData.Difficulty} не найден!");
            }

            return new LevelConfig
            {
                NodeName = $"Уровень {CurrentLevelIndex + 1}",
                NodeDescription = levelData.Hazard == LevelHazardType.None ? "Обычный уровень" : "Опасный уровень",
                Columns = diffSettings.Cols,
                Rows = diffSettings.Rows,
                MinWordLength = diffSettings.MinLen,
                MaxWordLength = diffSettings.MaxLen,
                Hazard = levelData.Hazard
            };
        }
    }
}