    using UnityEngine;
using UnityEngine.SceneManagement;

namespace _1GameProject.Scripts.GameManagement
{
    
    public class GameManager :  MonoBehaviour
    {
        
        [Header("Настройки победы")]
        [SerializeField] private int wordsToWin = 5; 
        
        private GameState _currentState = GameState.MainMenu;
        private int _foundWordsCount = 0;
        
        // private EventBinding<WordFoundEvent> _wordFoundBinding;
        // private EventBinding<GameRestartRequestEvent> _restartBinding;
        
        private void OnEnable()
        {
            // _wordFoundBinding = new EventBinding<WordFoundEvent>(OnWordFound);
            // EventBus<WordFoundEvent>.Register(_wordFoundBinding);
            // _restartBinding = new EventBinding<GameRestartRequestEvent>(OnRestartRequested);
            // EventBus<GameRestartRequestEvent>.Register(_restartBinding);
        }

        private void OnDisable()
        {
            // EventBus<WordFoundEvent>.Deregister(_wordFoundBinding);
            // EventBus<GameRestartRequestEvent>.Deregister(_restartBinding);
        }
        
        private void Start()
        {
            // Начинаем с главного меню (не запускаем игру автоматически)
            SetState(GameState.MainMenu);
        }

        // Публичный метод для старта игры (вызывается из UI)
        public void StartGame()
        {
            if (_currentState != GameState.MainMenu) return;
            
            _foundWordsCount = 0;
            SetState(GameState.Playing);
            //EventBus<GameStartedEvent>.Raise(new GameStartedEvent());
            
            // Здесь же можно сгенерировать поле через BoardGenerator,
            // но BoardGenerator сам подпишется на GameStartedEvent.
        }

        // Обработчик найденного слова
        // private void OnWordFound(WordFoundEvent evt)
        // {
        //     if (_currentState != GameState.Playing) return;
        //
        //     _foundWordsCount++;
        //     if (_foundWordsCount >= wordsToWin)
        //     {
        //         SetState(GameState.Victory);
        //         EventBus<LevelCompleteEvent>.Raise(new LevelCompleteEvent());
        //     }
        // }

        // Перезапуск (например, после победы нажали кнопку "Играть снова")
        // private void OnRestartRequested(GameRestartRequestEvent evt)
        // {
        //     if (_currentState == GameState.Victory || _currentState == GameState.MainMenu)
        //     {
        //         StartGame();
        //     }
        // }

        // Смена состояния с публикацией события
        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;
            
            GameState oldState = _currentState;
            _currentState = newState;
            
            // Оповещаем всех о смене состояния
            // EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent
            // {
            //     //NewState = newState,
            //     //OldState = oldState
            // });
            
            Debug.Log($"GameManager: состояние изменено с {oldState} на {newState}");
        }

        // Для отладки можно добавить метод получения текущего состояния
        public GameState GetCurrentState() => _currentState;
    
        
        public void BackToMainMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }
        
        
        
        
    }
}