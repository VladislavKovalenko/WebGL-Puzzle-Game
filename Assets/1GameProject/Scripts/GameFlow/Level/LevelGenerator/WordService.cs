using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator
{
    public class WordService
    {
        private readonly Dictionary<int, List<string>> _wordsByLength = new();

        public WordService(TextAsset csvFile)
        {
            Parse(csvFile.text);
        }

        private void Parse(string rawText)
        {
            string[] lines = rawText.Split(
                new[] { '\r', '\n' }, 
                System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string word = line.Trim().ToUpper();
                if (string.IsNullOrEmpty(word)) continue;

                int len = word.Length;

                if (!_wordsByLength.TryGetValue(len, out var list))
                {
                    list = new List<string>();
                    _wordsByLength[len] = list;
                }

                list.Add(word);
            }

            Debug.Log($"[WordService] Loaded: {_wordsByLength.Values.Sum(l => l.Count)} words");
        }

        public string GetRandomWord(int length)
        {
            if (_wordsByLength.TryGetValue(length, out var list) && list.Count > 0)
                return list[Random.Range(0, list.Count)];

            Debug.LogWarning($"[WordService] No words with length {length}");
            return null;
        }

        public List<string> GetWords(int length) =>
            _wordsByLength.TryGetValue(length, out var list) ? list : new();
    }
}