
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

        public Rect GetBoardScreenRect()
        {
            if (_gridContainer == null || _gridContainer.childCount == 0)
                return new Rect(0, 0, Screen.width, Screen.height);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            Camera cam = null;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            foreach (Transform child in _gridContainer)
            {
                var rectTrans = child as RectTransform;
                if (rectTrans == null) continue;

                Vector3[] corners = new Vector3[4];
                rectTrans.GetWorldCorners(corners);

                for (int i = 0; i < 4; i++)
                {
                    Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);

                    if (screenPos.x < minX) minX = screenPos.x;
                    if (screenPos.x > maxX) maxX = screenPos.x;
                    if (screenPos.y < minY) minY = screenPos.y;
                    if (screenPos.y > maxY) maxY = screenPos.y;
                }
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}