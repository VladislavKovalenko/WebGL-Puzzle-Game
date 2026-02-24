using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace PuzzleGame.WordSearch
{
    public sealed class WordSearchGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _maxAttemptsPerWord;
        private readonly float _allowDiagonalChance;
        private readonly Random _random;
        private readonly char[,] _grid;
        private char[] _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private readonly Dictionary<Direction8, (int dx, int dy)> _directionMap = new()
        {
            { Direction8.Up, (0, -1) },
            { Direction8.Down, (0, 1) },
            { Direction8.Left, (-1, 0) },
            { Direction8.Right, (1, 0) },
            { Direction8.UpLeft, (-1, -1) },
            { Direction8.UpRight, (1, -1) },
            { Direction8.DownLeft, (-1, 1) },
            { Direction8.DownRight, (1, 1) }
        };

        public WordSearchGenerator(int width, int height, int maxAttemptsPerWord, float allowDiagonalChance, int seed)
        {
            _width = width;
            _height = height;
            _maxAttemptsPerWord = Mathf.Max(1, maxAttemptsPerWord);
            _allowDiagonalChance = Mathf.Clamp01(allowDiagonalChance);
            _random = new Random(seed);
            _grid = new char[width, height];
        }

        public char[,] Generate(IReadOnlyList<string> words, out List<WordData> placedWords)
        {
            placedWords = new List<WordData>(words.Count);
            ClearGrid();
            BuildAlphabet(words);

            foreach (var word in words)
            {
                if (TryPlaceWord(word, out var cells))
                {
                    placedWords.Add(new WordData(word, cells));
                }
            }

            FillEmptyCells();
            return _grid;
        }

        private void ClearGrid()
        {
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    _grid[x, y] = '\0';
                }
            }
        }

        private bool TryPlaceWord(string word, out List<(int x, int y)> cells)
        {
            cells = null;
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            for (var attempt = 0; attempt < _maxAttemptsPerWord; attempt++)
            {
                var direction = GetRandomDirection();
                var (dx, dy) = _directionMap[direction];

                var startX = _random.Next(0, _width);
                var startY = _random.Next(0, _height);

                if (!CanPlaceWord(word, startX, startY, dx, dy))
                {
                    continue;
                }

                cells = PlaceWord(word, startX, startY, dx, dy);
                return true;
            }

            return false;
        }

        private Direction8 GetRandomDirection()
        {
            var includeDiagonal = _random.NextDouble() <= _allowDiagonalChance;
            if (!includeDiagonal)
            {
                return (Direction8)_random.Next(0, 4);
            }

            return (Direction8)_random.Next(0, 8);
        }

        private bool CanPlaceWord(string word, int startX, int startY, int dx, int dy)
        {
            var x = startX;
            var y = startY;

            for (var i = 0; i < word.Length; i++)
            {
                if (!InBounds(x, y))
                {
                    return false;
                }

                var existing = _grid[x, y];
                if (existing != '\0' && existing != word[i])
                {
                    return false;
                }

                x += dx;
                y += dy;
            }

            return true;
        }

        private List<(int x, int y)> PlaceWord(string word, int startX, int startY, int dx, int dy)
        {
            var result = new List<(int x, int y)>(word.Length);
            var x = startX;
            var y = startY;

            for (var i = 0; i < word.Length; i++)
            {
                _grid[x, y] = word[i];
                result.Add((x, y));
                x += dx;
                y += dy;
            }

            return result;
        }

        private void FillEmptyCells()
        {
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    if (_grid[x, y] == '\0')
                    {
                        _grid[x, y] = RandomLetter();
                    }
                }
            }
        }

        private char RandomLetter()
        {
            return _alphabet[_random.Next(0, _alphabet.Length)];
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private void BuildAlphabet(IReadOnlyList<string> words)
        {
            var set = new HashSet<char>();
            foreach (var w in words)
            {
                foreach (var c in w)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        set.Add(c);
                    }
                }
            }

            if (set.Count == 0)
            {
                _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
                return;
            }

            _alphabet = new char[set.Count];
            set.CopyTo(_alphabet);
        }
    }
}
