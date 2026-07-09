using System.Collections.Generic;
using System.Linq;
using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameData.SO;
using _1GameProject.Scripts.GameFlow.Level.Start;
using UniRx;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Level.LevelGenerator.BoardMVP
{
    public class BoardPresenter : IInitializable, ITickable
    {
        private readonly BoardGenerator _generator;
        private readonly BoardView _view;
        private readonly GameplayModel _gameplayModel;
        private readonly GameSessionModel _sessionModel;
        private readonly LevelConfig _config;
        private readonly SoundLibrarySO _soundLibrary;

        private BoardData _boardData;
        private bool _isDragging = false;
        private List<LetterCellView> _currentSelection = new();

        [Inject]
        public BoardPresenter(
            BoardGenerator generator, 
            BoardView view, 
            GameplayModel gameplayModel, 
            GameSessionModel sessionModel,
            LevelConfig config,
            SoundLibrarySO soundLibrary)
        {
            _generator = generator;
            _view = view;
            _gameplayModel = gameplayModel;
            _sessionModel = sessionModel;
            _config = config;
            _soundLibrary = soundLibrary;
        }

        public void Initialize()
        {
            // Генерируем доску по полученному конфигу
            _boardData = _generator.Generate(_config);

            if (_boardData != null)
            {
                _gameplayModel.SetupLevel(_boardData.Words.Count);
                _view.BuildGrid(_boardData, OnCellPointerDown, OnCellPointerEnter);
            }
        }

        public void Tick()
        {
            if (_isDragging && (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)))
            {
                EvaluateSelection();
            }
        }

        private void OnCellPointerDown(LetterCellView cell)
        {
            if (_gameplayModel.CurrentState.Value != GameState.Playing) return;
            if (cell.CurrentState == CellState.Found) return;

            _isDragging = true;
            _currentSelection.Clear();
            AddToSelection(cell);
        }

        private void OnCellPointerEnter(LetterCellView cell)
        {
            if (_gameplayModel.CurrentState.Value != GameState.Playing) return;
            if (!_isDragging) return;
            if (cell.CurrentState == CellState.Found) return;

            if (_currentSelection.Count > 1 && cell == _currentSelection[^2])
            {
                _currentSelection[^1].SetState(CellState.Normal);
                _currentSelection.RemoveAt(_currentSelection.Count - 1);
                return;
            }

            if (_currentSelection.Contains(cell)) return;
            if (!IsAdjacent(_currentSelection[^1].GridPosition, cell.GridPosition)) return;

            AddToSelection(cell);
        }

        private void AddToSelection(LetterCellView cell)
        {
            _currentSelection.Add(cell);
            cell.SetState(CellState.Selected);

            _soundLibrary.PlayOneShot(_soundLibrary.letterSelect);
        }

        private void EvaluateSelection()
        {
            _isDragging = false;
            if (_currentSelection.Count < 2)
            {
                ResetSelection(CellState.Normal);
                return;
            }

            string selectedWord = new string(_currentSelection.Select(c => c.Letter).ToArray());
            string reversedWord = new string(selectedWord.Reverse().ToArray());
            List<Vector2Int> selectedPath = _currentSelection.Select(c => c.GridPosition).ToList();

            WordData foundWordData = null;

            foreach (var w in _boardData.Words)
            {
                if (w.IsFound) continue;

                if (w.Word == selectedWord || w.Word == reversedWord)
                {
                    bool isPathMatch = true;
                    bool isReversedPathMatch = true;

                    for (int i = 0; i < selectedPath.Count; i++)
                    {
                        if (selectedPath[i] != w.Path[i]) isPathMatch = false;
                        if (selectedPath[i] != w.Path[w.Path.Count - 1 - i]) isReversedPathMatch = false;
                    }

                    if (isPathMatch || isReversedPathMatch)
                    {
                        foundWordData = w;
                        break;
                    }
                }
            }

            if (foundWordData != null)
            {
                Debug.Log($"Найдено слово: {foundWordData.Word}");
                foundWordData.IsFound = true;

                foreach (var cell in _currentSelection)
                    cell.SetState(CellState.Found);

                _gameplayModel.AddFoundWord(); 
            }
            else
            {
                Debug.Log($"Ошибка: {selectedWord}");
                _sessionModel.TakeDamage(); 

                foreach (var cell in _currentSelection)
                    cell.SetState(CellState.Error);

                var cellsToReset = new List<LetterCellView>(_currentSelection);
                Observable.Timer(System.TimeSpan.FromSeconds(0.5f))
                    .Subscribe(_ =>
                    {
                        foreach (var cell in cellsToReset)
                        {
                            if (cell.CurrentState == CellState.Error)
                                cell.SetState(CellState.Normal);
                        }
                    });
            }

            _currentSelection.Clear();
        }

        private void ResetSelection(CellState targetState)
        {
            foreach (var cell in _currentSelection) cell.SetState(targetState);
            _currentSelection.Clear();
        }

        private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dy = Mathf.Abs(pos1.y - pos2.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
    }
}