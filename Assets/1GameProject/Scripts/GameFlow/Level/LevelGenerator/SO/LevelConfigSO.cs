using UnityEngine;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator.SO
{
    public enum LevelHazardType
    {
        None,
        Flashlight,
        Blur
    }
    
    
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "SO/LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Размер поля")]
        public int Columns = 4;
        public int Rows = 4;

        [Header("Длина слов")]
        public int MinWordLength = 3;
        public int MaxWordLength = 6;
        
        [Header("Препятствия на уровне")]
        public LevelHazardType Hazard = LevelHazardType.None;
    }
}