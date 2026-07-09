using UnityEngine;

namespace _1GameProject.Scripts.GameData.SO
{
    public enum LevelHazardType
    {
        None,
        Flashlight,
        Blur
    }
    
    public class LevelConfig
    {
        [Header("Отображение в меню (для кнопок выбора)")]
        public string NodeName = "Уровень";
        public string NodeDescription = "Описание уровня";
        
        [Header("Размер поля")]
        public int Columns = 4;
        public int Rows = 4;

        [Header("Длина слов")]
        public int MinWordLength = 3;
        public int MaxWordLength = 6;
        
        [Header("Препятствия на уровне")]
        public LevelHazardType Hazard = LevelHazardType.None;
        
        // Метод клонирования, чтобы при добавлении случайного модификатора 
        // мы не перезаписали оригинальные настройки в нашем SO
        public LevelConfig Clone()
        {
            return (LevelConfig)this.MemberwiseClone();
        }
    }
}