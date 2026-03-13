using System;
using System.Collections.Generic;

namespace PuzzleGame.WordSearch
{
    public sealed class WordSearchValidator
    {
        private readonly Dictionary<string, WordData> _wordByValue;

        public WordSearchValidator(List<WordData> words)
        {
            _wordByValue = new Dictionary<string, WordData>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in words)
            {
                _wordByValue[w.Value] = w;
            }
        }

        public bool IsPathAdjacent(IReadOnlyList<(int x, int y)> path)
        {
            if (path.Count <= 1)
            {
                return path.Count == 1;
            }

            for (var i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var next = path[i];

                var dx = Math.Abs(prev.x - next.x);
                var dy = Math.Abs(prev.y - next.y);
                var isNeighbor = dx <= 1 && dy <= 1 && (dx + dy > 0);
                if (!isNeighbor)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryResolveWord(string candidate, out WordData wordData)
        {
            if (_wordByValue.TryGetValue(candidate, out var direct))
            {
                wordData = direct;
                return true;
            }

            var reversed = Reverse(candidate);
            if (_wordByValue.TryGetValue(reversed, out var rev))
            {
                wordData = rev;
                return true;
            }

            wordData = null;
            return false;
        }

        private static string Reverse(string text)
        {
            var chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }
}
