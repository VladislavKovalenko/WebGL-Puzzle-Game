// FILE: Scripts/GameData/CampaignRouteSO.cs
using System.Collections.Generic;
using System.Linq;
using _1GameProject.Scripts.GameData.SO;
using UnityEngine;

namespace _1GameProject.Scripts.GameData
{
    public enum LevelDifficulty
    {
        Easy,
        Medium,
        Hard,
        Nightmare
    }

    [System.Serializable]
    public class DifficultyTemplate
    {
        [HideInInspector] public string Name; 
        public LevelDifficulty Difficulty;
        
        public int Cols = 4;
        public int Rows = 4;
        public int MinLen = 3;
        public int MaxLen = 5;
    }

    [System.Serializable]
    public class CampaignLevel
    {
        [HideInInspector] public string InspectorName; 
        
        public LevelDifficulty Difficulty = LevelDifficulty.Easy;
        public LevelHazardType Hazard = LevelHazardType.None;
    }

    [CreateAssetMenu(fileName = "CampaignRoute", menuName = "SO/CampaignRoute")]
    public class CampaignRouteSO : ScriptableObject
    {
        [Header("Шаблоны сложности (Настройте размеры сеток здесь)")]
        public List<DifficultyTemplate> DifficultySettings = new();

        [Header("Пул из 30 уровней (Выберите сложность и угрозу)")]
        public CampaignLevel[] Levels = new CampaignLevel[30];

        public DifficultyTemplate GetSettings(LevelDifficulty difficulty)
        {
            var template = DifficultySettings.FirstOrDefault(t => t.Difficulty == difficulty);
            if (template == null)
            {
                Debug.LogWarning($"[CampaignRoute] Не настроен шаблон для {difficulty}! Использую базовый.");
                return new DifficultyTemplate { Cols = 4, Rows = 4, MinLen = 3, MaxLen = 5 };
            }
            return template;
        }

        private void OnValidate()
        {
            if (DifficultySettings.Count == 0)
            {
                DifficultySettings.Add(new DifficultyTemplate { Difficulty = LevelDifficulty.Easy, Cols = 3, Rows = 3, MinLen = 3, MaxLen = 4 });
                DifficultySettings.Add(new DifficultyTemplate { Difficulty = LevelDifficulty.Medium, Cols = 4, Rows = 4, MinLen = 3, MaxLen = 5 });
                DifficultySettings.Add(new DifficultyTemplate { Difficulty = LevelDifficulty.Hard, Cols = 5, Rows = 5, MinLen = 4, MaxLen = 6 });
                DifficultySettings.Add(new DifficultyTemplate { Difficulty = LevelDifficulty.Nightmare, Cols = 6, Rows = 6, MinLen = 5, MaxLen = 7 });
            }

            foreach (var template in DifficultySettings)
            {
                template.Name = $"Настройки: {template.Difficulty}";
            }

            if (Levels == null || Levels.Length != 30)
            {
                System.Array.Resize(ref Levels, 30);
            }

            // === ОБНОВЛЕННОЕ ФОРМАТИРОВАНИЕ НАЗВАНИЙ ===
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i] == null) Levels[i] = new CampaignLevel();
                
                string category = (i < 10) ? "[Простой пул]" : "[Сложный пул]";
                
                // Если есть угроза, добавляем её через "+", иначе оставляем пустоту
                string hazardText = Levels[i].Hazard != LevelHazardType.None ? $" + {Levels[i].Hazard}" : "";
                
                Levels[i].InspectorName = $"Ур {i + 1} {category} - {Levels[i].Difficulty}{hazardText}";
            }
        }
    }
}