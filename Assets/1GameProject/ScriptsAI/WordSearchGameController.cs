using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.WordSearch
{
    public sealed class WordSearchGameController : MonoBehaviour
    {
        [SerializeField] private WordSearchConfig config;

        private List<WordData> _words;
        private WordSearchValidator _validator;

        public char[,] Grid { get; private set; }
        public int Width => config.width;
        public int Height => config.height;
        public IReadOnlyList<WordData> Words => _words;
        public int FoundCount { get; private set; }
        public int TotalWords => _words?.Count ?? 0;

        public event Action GridGenerated;
        public event Action<WordData> WordFound;
        public event Action PuzzleCompleted;

        private void Awake()
        {
            Generate();
        }

        public void Generate()
        {
            if (config == null)
            {
                throw new InvalidOperationException("WordSearchConfig is not assigned.");
            }

            var seed = config.ResolveSeed();
            var generator = new WordSearchGenerator(config.width, config.height, config.maxPlacementAttemptsPerWord, config.allowDiagonalChance, seed);
            Grid = generator.Generate(config.NormalizedWords(), out _words);
            _validator = new WordSearchValidator(_words);
            FoundCount = 0;

            GridGenerated?.Invoke();
        }

        public bool TrySubmitSelection(string candidateWord, IReadOnlyList<(int x, int y)> path, out WordData foundWord)
        {
            foundWord = null;
            if (string.IsNullOrWhiteSpace(candidateWord) || path == null || path.Count == 0)
            {
                return false;
            }

            if (!_validator.IsPathAdjacent(path))
            {
                return false;
            }

            if (!_validator.TryResolveWord(candidateWord, out var match))
            {
                return false;
            }

            if (match.IsFound)
            {
                return false;
            }

            match.IsFound = true;
            FoundCount++;
            foundWord = match;
            WordFound?.Invoke(match);

            if (FoundCount >= TotalWords)
            {
                PuzzleCompleted?.Invoke();
            }

            return true;
        }
    }
}
