using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator.BoardMVP
{
    public enum CellState
    {
        Normal,
        Selected,   // Игрок сейчас ведет по ней пальцем
        Found,      // Слово отгадано
        Error       // Ошибка (красный цвет на секунду)
    }

    public class LetterCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
    {
        [SerializeField] private TextMeshProUGUI _letterText;
        [SerializeField] private Image _backgroundImage;

        [Header("Цвета состояний")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _foundColor = Color.green;
        [SerializeField] private Color _errorColor = Color.red;

        public Vector2Int GridPosition { get; private set; }
        public char Letter { get; private set; }
        public CellState CurrentState { get; private set; } = CellState.Normal;

        // События, которые будет слушать Presenter
        private Action<LetterCellView> _onPointerDown;
        private Action<LetterCellView> _onPointerEnter;

        public void Init(Vector2Int gridPos, char letter, Action<LetterCellView> onDown, Action<LetterCellView> onEnter)
        {
            GridPosition = gridPos;
            Letter = letter;
            _letterText.text = letter.ToString();

            _onPointerDown = onDown;
            _onPointerEnter = onEnter;

            SetState(CellState.Normal);
        }

        public void SetState(CellState newState)
        {
            CurrentState = newState;
            _backgroundImage.color = newState switch
            {
                CellState.Normal => _normalColor,
                CellState.Selected => _selectedColor,
                CellState.Found => _foundColor,
                CellState.Error => _errorColor,
                _ => _normalColor
            };
        }

        public void OnPointerDown(PointerEventData eventData) => _onPointerDown?.Invoke(this);
        public void OnPointerEnter(PointerEventData eventData) => _onPointerEnter?.Invoke(this);
    }
}