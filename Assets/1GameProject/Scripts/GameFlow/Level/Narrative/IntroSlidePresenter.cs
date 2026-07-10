using System;
using UniRx;
using Zenject;
using _1GameProject.Scripts.GameFlow.Level.Start;
using YG;

namespace _1GameProject.Scripts.GameFlow.Level.Narrative
{
    public class IntroSlidePresenter : IInitializable, IDisposable
    {
        private readonly IntroSlideView _view;
        private readonly GameplayModel _gameplayModel;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public IntroSlidePresenter(IntroSlideView view, GameplayModel gameplayModel)
        {
            _view = view;
            _gameplayModel = gameplayModel;
        }

        public void Initialize()
        {
            if (_gameplayModel.CurrentState.Value == GameState.Intro)
            {
                _view.Show();
                _view.OnNextClicked.Subscribe(_ => CloseIntroAndStartGame()).AddTo(_disposables);
            }
            else
            {
                _view.Hide();
            }
        }

        private void CloseIntroAndStartGame()
        {
            _view.Hide();

            YG2.saves.isIntroWatched = true;
            YG2.SaveProgress();

            _gameplayModel.CurrentState.Value = GameState.Playing;
        }

        public void Dispose() => _disposables.Dispose();
    }
}