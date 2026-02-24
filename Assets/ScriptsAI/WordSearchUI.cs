using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.WordSearch
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class WordSearchUI : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private WordSearchGameController gameController;

        [Header("UI Element Names")]
        [SerializeField] private string gridName = "Grid";
        [SerializeField] private string wordsListName = "WordsList";
        [SerializeField] private string selectedWordName = "SelectedWord";
        [SerializeField] private string progressName = "Progress";
        [SerializeField] private string statusName = "Status";
        [SerializeField] private string restartButtonName = "RestartButton";

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _grid;
        private VisualElement _wordsList;
        private Label _selectedWord;
        private Label _progress;
        private Label _status;
        private Button _restartButton;

        private readonly Dictionary<(int x, int y), Label> _cellLabels = new();
        private readonly Dictionary<string, Label> _wordLabels = new();
        private readonly SelectionPath _selectionPath = new();

        private bool _isDragging;
        private int _pointerId = -1;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            BindUi();
        }

        private void OnEnable()
        {
            if (gameController == null)
            {
                Debug.LogError("WordSearchGameController is not assigned.");
                enabled = false;
                return;
            }

            gameController.GridGenerated += OnGridGenerated;
            gameController.WordFound += OnWordFound;
            gameController.PuzzleCompleted += OnPuzzleCompleted;
            OnGridGenerated();
        }

        private void OnDisable()
        {
            if (gameController == null)
            {
                return;
            }

            gameController.GridGenerated -= OnGridGenerated;
            gameController.WordFound -= OnWordFound;
            gameController.PuzzleCompleted -= OnPuzzleCompleted;
        }

        private void BindUi()
        {
            _grid = FindOrCreate<VisualElement>(gridName);
            _wordsList = FindOrCreate<VisualElement>(wordsListName);
            _selectedWord = FindOrCreate<Label>(selectedWordName);
            _progress = FindOrCreate<Label>(progressName);
            _status = FindOrCreate<Label>(statusName);
            _restartButton = FindOrCreate<Button>(restartButtonName);

            _restartButton.clicked += () =>
            {
                gameController.Generate();
                ResetSelectionVisuals();
            };
        }

        private T FindOrCreate<T>(string name) where T : VisualElement, new()
        {
            var element = _root.Q<T>(name);
            if (element != null)
            {
                return element;
            }

            var created = new T { name = name };
            _root.Add(created);
            return created;
        }

        private void OnGridGenerated()
        {
            BuildGrid();
            BuildWordsList();
            _selectedWord.text = string.Empty;
            _status.text = "Find all words";
            UpdateProgress();
        }

        private void BuildGrid()
        {
            _grid.Clear();
            _cellLabels.Clear();
            _grid.style.flexDirection = FlexDirection.Column;

            var data = gameController.Grid;
            for (var y = 0; y < gameController.Height; y++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                _grid.Add(row);

                for (var x = 0; x < gameController.Width; x++)
                {
                    var letter = data[x, y];
                    var cell = new Label(letter.ToString())
                    {
                        name = $"Cell_{x}_{y}"
                    };
                    cell.AddToClassList("word-cell");
                    cell.userData = (x, y);

                    cell.RegisterCallback<PointerDownEvent>(OnCellPointerDown);
                    cell.RegisterCallback<PointerEnterEvent>(OnCellPointerEnter);
                    cell.RegisterCallback<PointerUpEvent>(OnCellPointerUp);

                    row.Add(cell);
                    _cellLabels[(x, y)] = cell;
                }
            }
        }

        private void BuildWordsList()
        {
            _wordsList.Clear();
            _wordLabels.Clear();

            foreach (var word in gameController.Words)
            {
                var item = new Label(word.Value);
                item.AddToClassList("word-item");
                if (word.IsFound)
                {
                    item.AddToClassList("word-found");
                }

                _wordsList.Add(item);
                _wordLabels[word.Value] = item;
            }
        }

        private void OnCellPointerDown(PointerDownEvent evt)
        {
            if (evt.target is not Label cell || cell.userData is not (int x, int y) c)
            {
                return;
            }

            _isDragging = true;
            _pointerId = evt.pointerId;
            cell.CapturePointer(_pointerId);
            StartSelection(c.Item1, c.Item2);
            evt.StopPropagation();
        }

        private void OnCellPointerEnter(PointerEnterEvent evt)
        {
            if (!_isDragging || evt.target is not Label cell || cell.userData is not (int x, int y) c)
            {
                return;
            }

            TryExtendSelection(c.Item1, c.Item2);
        }

        private void OnCellPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || evt.pointerId != _pointerId)
            {
                return;
            }

            if (evt.target is Label cell && cell.HasPointerCapture(_pointerId))
            {
                cell.ReleasePointer(_pointerId);
            }

            CompleteSelection();
            _isDragging = false;
            _pointerId = -1;
        }

        private void StartSelection(int x, int y)
        {
            ResetSelectionVisuals();
            _selectionPath.Clear();
            _selectionPath.Add(x, y);
            SetCellSelected(x, y, true);
            _selectedWord.text = _selectionPath.BuildWord(gameController.Grid);
        }

        private void TryExtendSelection(int x, int y)
        {
            if (_selectionPath.Contains(x, y))
            {
                return;
            }

            var last = _selectionPath.Cells.Last();
            var dx = Mathf.Abs(last.x - x);
            var dy = Mathf.Abs(last.y - y);
            var isNeighbor = dx <= 1 && dy <= 1 && (dx + dy > 0);
            if (!isNeighbor)
            {
                return;
            }

            _selectionPath.Add(x, y);
            SetCellSelected(x, y, true);
            _selectedWord.text = _selectionPath.BuildWord(gameController.Grid);
        }

        private void CompleteSelection()
        {
            var candidate = _selectionPath.BuildWord(gameController.Grid);
            if (gameController.TrySubmitSelection(candidate, _selectionPath.Cells, out var found))
            {
                MarkFound(found);
                _status.text = $"Found: {found.Value}";
            }
            else
            {
                ResetSelectionVisuals();
                _status.text = "No match";
            }

            _selectionPath.Clear();
            _selectedWord.text = string.Empty;
            UpdateProgress();
        }

        private void OnWordFound(WordData word)
        {
            if (_wordLabels.TryGetValue(word.Value, out var item))
            {
                item.AddToClassList("word-found");
            }
        }

        private void OnPuzzleCompleted()
        {
            _status.text = "Puzzle completed";
        }

        private void MarkFound(WordData word)
        {
            foreach (var pos in word.Cells)
            {
                if (_cellLabels.TryGetValue(pos, out var cell))
                {
                    cell.RemoveFromClassList("word-cell-selected");
                    cell.AddToClassList("word-cell-found");
                }
            }
        }

        private void ResetSelectionVisuals()
        {
            foreach (var kv in _cellLabels)
            {
                if (!kv.Value.ClassListContains("word-cell-found"))
                {
                    kv.Value.RemoveFromClassList("word-cell-selected");
                }
            }
        }

        private void SetCellSelected(int x, int y, bool isSelected)
        {
            if (!_cellLabels.TryGetValue((x, y), out var cell) || cell.ClassListContains("word-cell-found"))
            {
                return;
            }

            if (isSelected)
            {
                cell.AddToClassList("word-cell-selected");
            }
            else
            {
                cell.RemoveFromClassList("word-cell-selected");
            }
        }

        private void UpdateProgress()
        {
            _progress.text = $"{gameController.FoundCount}/{gameController.TotalWords}";
        }
    }
}
