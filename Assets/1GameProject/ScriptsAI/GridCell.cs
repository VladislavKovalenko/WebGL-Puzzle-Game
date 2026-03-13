using UnityEngine;

namespace PuzzleGame.WordSearch
{
    public readonly struct GridCell
    {
        public readonly int X;
        public readonly int Y;
        public readonly char Letter;

        public GridCell(int x, int y, char letter)
        {
            X = x;
            Y = y;
            Letter = letter;
        }
    }
}
