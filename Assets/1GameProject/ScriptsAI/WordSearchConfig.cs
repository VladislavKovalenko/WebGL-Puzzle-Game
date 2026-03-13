using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.WordSearch
{
    [CreateAssetMenu(fileName = "WordSearchConfig", menuName = "PuzzleGame/WordSearch Config")]
    public sealed class WordSearchConfig : ScriptableObject
    {
        [Min(4)] public int width = 10;
        [Min(4)] public int height = 10;
        [Min(1)] public int maxPlacementAttemptsPerWord = 100;
        [Range(0f, 1f)] public float allowDiagonalChance = 1f;
        public int randomSeed = 0;
        public bool useRandomSeed = true;
        public List<string> words = new() { "UNITY", "PUZZLE", "CODE", "SCRIPT", "GAME" };

        public IReadOnlyList<string> NormalizedWords()
        {
            var result = new List<string>(words.Count);
            foreach (var w in words)
            {
                if (string.IsNullOrWhiteSpace(w))
                {
                    continue;
                }

                result.Add(w.Trim().ToUpperInvariant());
            }

            result.Sort((a, b) => b.Length.CompareTo(a.Length));
            return result;
        }

        public int ResolveSeed()
        {
            if (useRandomSeed)
            {
                return Environment.TickCount;
            }

            return randomSeed;
        }
    }
}
