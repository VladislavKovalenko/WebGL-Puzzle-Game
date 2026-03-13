using System.Collections.Generic;

namespace PuzzleGame.WordSearch
{
    public sealed class WordData
    {
        public string Value { get; }
        public List<(int x, int y)> Cells { get; }
        public bool IsFound { get; set; }

        public WordData(string value, List<(int x, int y)> cells)
        {
            Value = value;
            Cells = cells;
            IsFound = false;
        }
    }
}
