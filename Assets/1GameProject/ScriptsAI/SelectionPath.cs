using System.Collections.Generic;
using System.Text;

namespace PuzzleGame.WordSearch
{
    public sealed class SelectionPath
    {
        private readonly List<(int x, int y)> _cells = new();

        public IReadOnlyList<(int x, int y)> Cells => _cells;

        public void Clear()
        {
            _cells.Clear();
        }

        public bool Contains(int x, int y)
        {
            return _cells.Contains((x, y));
        }

        public void Add(int x, int y)
        {
            _cells.Add((x, y));
        }

        public string BuildWord(char[,] grid)
        {
            var sb = new StringBuilder(_cells.Count);
            foreach (var c in _cells)
            {
                sb.Append(grid[c.x, c.y]);
            }
            return sb.ToString();
        }
    }
}
