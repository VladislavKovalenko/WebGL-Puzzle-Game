// FILE: Scripts/GameData/GameSessionModel.cs
using System.Collections.Generic;
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

        public int StagesSurvived { get; private set; } = 0; // Сколько комнат прошел игрок
        public LevelConfig CurrentConfig { get; set; }
        public bool TookDamageThisLevel { get; set; } = false;

        public List<LevelConfig> AvailableChoices { get; } = new();

        public GameSessionModel(CampaignRouteSO route)
        {
            _route = route;
        }

        public void StartNewRun()
        {
            GlobalLives.Value = MaxLives;
            StagesSurvived = 0;
            ResetLevelFlags();
            GenerateChoices();
        }

        public void AdvanceStage()
        {
            StagesSurvived++;
            ResetLevelFlags();
            GenerateChoices();
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

        private void GenerateChoices()
        {
            AvailableChoices.Clear();

            // 1. Выбираем случайный ПРОСТОЙ уровень (из первых 10, индексы 0-9)
            int easyIndex = Random.Range(0, 10);
            AvailableChoices.Add(CreateConfigFromTemplate(_route.Levels[easyIndex], easyIndex + 1));

            // 2. Выбираем случайный СЛОЖНЫЙ уровень (из оставшихся 20, индексы 10-29)
            int hardIndex = Random.Range(10, 30);
            AvailableChoices.Add(CreateConfigFromTemplate(_route.Levels[hardIndex], hardIndex + 1));
        }

        private LevelConfig CreateConfigFromTemplate(CampaignLevel template, int realLevelNumber)
        {
            // Достаем настройки сетки (Cols, Rows, Min, Max) по выбранной сложности
            DifficultyTemplate diffSettings = _route.GetSettings(template.Difficulty);

            return new LevelConfig
            {
                NodeName = $"Уровень {realLevelNumber}",
                NodeDescription = template.Hazard == LevelHazardType.None ? "Обычный путь" : "Опасный путь",
                
                // Берем размеры из глобального шаблона сложности
                Columns = diffSettings.Cols,
                Rows = diffSettings.Rows,
                MinWordLength = diffSettings.MinLen,
                MaxWordLength = diffSettings.MaxLen,
                
                // А модификатор берем из самого уровня
                Hazard = template.Hazard
            };
        }
    }
}