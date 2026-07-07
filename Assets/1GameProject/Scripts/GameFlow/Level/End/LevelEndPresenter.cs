// FILE: Scripts/GameFlow/Level/End/LevelEndPresenter.cs
using System;
using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameFlow.Level.Start;
using _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu;
using UniRx;
using UnityEngine.SceneManagement;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Level.End
{
    public class LevelEndPresenter : IInitializable, IDisposable
    {
        private readonly GameplayModel _gameplayModel;
        private readonly GameSessionModel _sessionModel;
        private readonly LevelEndWindowView _view;
        private readonly LevelsModel _levelsModel;
        private readonly SoundLibrarySO _soundLibrary;
        
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelEndPresenter(
            GameplayModel gameplayModel, 
            GameSessionModel sessionModel, 
            LevelEndWindowView view, 
            LevelsModel levelsModel, 
            SoundLibrarySO soundLibrary)
        {
            _gameplayModel = gameplayModel;
            _sessionModel = sessionModel;
            _view = view;
            _levelsModel = levelsModel;
            _soundLibrary = soundLibrary;
        }

        public void Initialize()
        {
            _view.Hide();

            // Реагируем только на состояния Win или Lose
            _gameplayModel.CurrentState
                .Where(state => state == GameState.Win || state == GameState.Lose)
                .Subscribe(HandleGameOver)
                .AddTo(_disposables);

            _view.OnContinueClicked.Subscribe(_ => ReturnToMenu()).AddTo(_disposables);
            _view.OnToMenuClicked.Subscribe(_ => ReturnToMenu()).AddTo(_disposables);
        }

        private void HandleGameOver(GameState finalState)
        {
            if (finalState == GameState.Win)
            {
                _view.ShowWin();
                _soundLibrary.PlayOneShot(_soundLibrary.winSound); 
                
                // В будущем сохраняем реальный прогресс
                _levelsModel.CompleteLevel(_sessionModel.CurrentLevel); 
            }
            else if (finalState == GameState.Lose)
            {
                _view.ShowLose();
                _soundLibrary.PlayOneShot(_soundLibrary.screamerSound); 
            }
        }

        private void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}