
using System.Collections.Generic;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator.BoardMVP
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private LetterCellView _cellPrefab;
        [SerializeField] private Transform _gridContainer; 
        [SerializeField] private GridLayoutGroup _gridLayout;

        // Храним все созданные ячейки, чтобы Presenter мог к ним обращаться
        public Dictionary<Vector2Int, LetterCellView> Cells { get; private set; } = new();

        public void BuildGrid(BoardData data, System.Action<LetterCellView> onCellDown, System.Action<LetterCellView> onCellEnter)
        {
            Clear();

            // Настраиваем количество колонок в GridLayoutGroup
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = data.Columns;

            // Спавним ячейки
            for (int y = 0; y < data.Rows; y++)
            {
                for (int x = 0; x < data.Columns; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    char letter = data.Grid[x, y];

                    LetterCellView cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.Init(pos, letter, onCellDown, onCellEnter);
                    
                    Cells[pos] = cell;
                }
            }
        }

        public void Clear()
        {
            foreach (Transform child in _gridContainer)
            {
                Destroy(child.gameObject);
            }
            Cells.Clear();
        }
    }
}