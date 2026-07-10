// FILE: Scripts/GameData/GlobalGameData.cs
using System.Collections.Generic;

namespace YG
{
    public partial class SavesYG
    {
        // Убираем старый int maxUnlockedLevel;
        
        // Список ID открытых уровней (по дефолту открыт только 1-й)
        public List<int> unlockedLevels = new List<int> { 1 }; 
        
        // Список ID пройденных уровней (чтобы понимать, когда мы прошли все 30)
        public List<int> completedLevels = new List<int>();

        public bool isIntroWatched = false; 
    }
}