using System.Collections.Generic;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator.SO;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator
{
    public class BoardGenerator
    {
        private readonly WordService _wordService;
        
        private int _cols;
        private int _rows;
        private int[,] _grid; // 0 = пусто, >0 = ID слова
        private List<List<Vector2Int>> _paths;

        private readonly Vector2Int[] _directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        [Inject]
        public BoardGenerator(WordService wordService)
        {
            _wordService = wordService;
        }

        public BoardData Generate(LevelConfigSO config)
        {
            _cols = config.Columns;
            _rows = config.Rows;
            
            // Пытаемся сгенерировать поле (иногда алгоритм может зайти в глухой тупик, делаем несколько попыток)
            int maxAttempts = 50;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (TryGenerate(config, out BoardData data))
                {
                    Debug.Log($"[BoardGenerator] Уровень успешно сгенерирован за {i + 1} попыток!");
                    return data;
                }
            }

            Debug.LogError("[BoardGenerator] Не удалось сгенерировать поле. Попробуйте изменить настройки длин слов.");
            return null;
        }

        private bool TryGenerate(LevelConfigSO config, out BoardData boardData)
        {
            boardData = null;
            _grid = new int[_cols, _rows];
            _paths = new List<List<Vector2Int>>();

            int totalCells = _cols * _rows;
            
            // 1. Создаем случайный набор длин слов, сумма которых равна площади поля
            List<int> targetLengths = GetRandomLengthPartition(totalCells, config.MinWordLength, config.MaxWordLength);
            if (targetLengths == null) return false; // Невозможно разбить

            // 2. Пытаемся уложить эти длины змейкой на поле
            if (!TryPartitionGrid(0, targetLengths)) return false;

            // 3. Если змейки уложились, берем реальные слова из WordService
            boardData = new BoardData
            {
                Columns = _cols,
                Rows = _rows,
                Grid = new char[_cols, _rows],
                Words = new List<WordData>()
            };

            for (int i = 0; i < _paths.Count; i++)
            {
                int length = _paths[i].Count;
                string word = _wordService.GetRandomWord(length);

                if (string.IsNullOrEmpty(word))
                {
                    Debug.LogWarning($"[BoardGenerator] В словаре нет слов длины {length}!");
                    return false; // Провал, в словаре нет подходящего слова
                }

                boardData.Words.Add(new WordData { Word = word, Path = _paths[i] });

                // Записываем буквы на поле по координатам пути
                for (int j = 0; j < _paths[i].Count; j++)
                {
                    Vector2Int pos = _paths[i][j];
                    boardData.Grid[pos.x, pos.y] = word[j];
                }
            }

            return true;
        }

        // === РЕКУРСИЯ ПОИСКА ПУТЕЙ НА ПОЛЕ ===
        private bool TryPartitionGrid(int wordIndex, List<int> targetLengths)
        {
            if (wordIndex == targetLengths.Count) return true; // Все слова уложены!

            // Ищем первую пустую ячейку (сканируем слева-направо, сверху-вниз)
            Vector2Int start = new Vector2Int(-1, -1);
            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    if (_grid[x, y] == 0)
                    {
                        start = new Vector2Int(x, y);
                        break;
                    }
                }
                if (start.x != -1) break;
            }

            int length = targetLengths[wordIndex];
            List<Vector2Int> path = new List<Vector2Int> { start };
            
            _grid[start.x, start.y] = wordIndex + 1; // Отмечаем ячейку как занятую

            if (TryBuildPath(start, length - 1, wordIndex, path, targetLengths)) 
                return true;

            _grid[start.x, start.y] = 0; // Откатываем (Backtracking)
            return false;
        }

        private bool TryBuildPath(Vector2Int current, int remainingLength, int wordIndex, List<Vector2Int> path, List<int> targetLengths)
        {
            if (remainingLength == 0)
            {
                _paths.Add(new List<Vector2Int>(path)); // Сохраняем найденный путь
                
                if (TryPartitionGrid(wordIndex + 1, targetLengths)) return true; // Идем к следующему слову
                
                _paths.RemoveAt(_paths.Count - 1); // Откат
                return false;
            }

            // Получаем доступных соседей и перемешиваем для рандомной формы змейки
            List<Vector2Int> neighbors = GetEmptyNeighbors(current);
            ShuffleList(neighbors);

            foreach (Vector2Int n in neighbors)
            {
                _grid[n.x, n.y] = wordIndex + 1;
                path.Add(n);

                if (TryBuildPath(n, remainingLength - 1, wordIndex, path, targetLengths)) 
                    return true;

                // Откат
                path.RemoveAt(path.Count - 1);
                _grid[n.x, n.y] = 0;
            }

            return false;
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        private List<Vector2Int> GetEmptyNeighbors(Vector2Int pos)
        {
            List<Vector2Int> list = new List<Vector2Int>(4);
            foreach (var dir in _directions)
            {
                Vector2Int n = pos + dir;
                if (n.x >= 0 && n.x < _cols && n.y >= 0 && n.y < _rows && _grid[n.x, n.y] == 0)
                    list.Add(n);
            }
            return list;
        }

        // Разбивает общее число ячеек на случайные слагаемые (длины слов)
        private List<int> GetRandomLengthPartition(int total, int min, int max)
        {
            List<int> lengths = new List<int>();
            int currentTotal = 0;

            while (currentTotal < total)
            {
                int remaining = total - currentTotal;
                
                if (remaining < min) return null; // Недопустимый остаток (например, осталась 1 ячейка, а мин длина 3)
                
                int maxPossible = Mathf.Min(max, remaining);
                
                // Если остаток меньше (min * 2), мы обязаны взять его целиком, 
                // иначе останется "огрызок" меньше min, который невозможно заполнить.
                int length = (remaining < min * 2) ? remaining : Random.Range(min, maxPossible + 1);

                // Если в словаре вообще нет слов такой длины, страхуемся
                if (_wordService.GetWords(length).Count == 0) return null; 

                lengths.Add(length);
                currentTotal += length;
            }
            return lengths;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rnd = Random.Range(i, list.Count);
                (list[i], list[rnd]) = (list[rnd], list[i]);
            }
        }
    }
}