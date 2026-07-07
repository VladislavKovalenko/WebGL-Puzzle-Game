using System.Collections.Generic;
using UnityEngine;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator
{
    // Информации об одном спрятанном слове
    public class WordData
    {
        public string Word;                     // Само слово (например "КОШКА")
        public List<Vector2Int> Path;           // Координаты ячеек по порядку
        public bool IsFound = false;            // Найдено ли игроком
    }

    // Полные данные уровня
    public class BoardData
    {
        public int Columns;
        public int Rows;
        public char[,] Grid;                    // Двумерный массив букв
        public List<WordData> Words;            // Список загаданных слов
    }
}