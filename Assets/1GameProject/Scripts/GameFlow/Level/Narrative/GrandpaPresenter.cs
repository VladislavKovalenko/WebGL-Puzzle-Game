// FILE: Scripts/GameFlow/Level/Narrative/GrandpaPresenter.cs
using System;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameFlow.Level.HUD;
using _1GameProject.Scripts.GameFlow.Level.Start;
using UniRx;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Level.Narrative
{
    public class GrandpaPresenter : IInitializable, IDisposable
    {
        private readonly GameplayModel _gameplayModel;
        private readonly GameSessionModel _sessionModel;
        private readonly GrandpaView _grandpaView;
        private readonly HealthBarView _healthBarView;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public GrandpaPresenter(GameplayModel gameplayModel, GameSessionModel sessionModel, GrandpaView grandpaView, HealthBarView healthBarView)
        {
            _gameplayModel = gameplayModel;
            _sessionModel = sessionModel;
            _grandpaView = grandpaView;
            _healthBarView = healthBarView;
        }

        public void Initialize()
        {
            // Слушаем локальную модель (настроение деда)
            _gameplayModel.CurrentGrandpaState
                .Subscribe(_grandpaView.SetState)
                .AddTo(_disposables);

            // Слушаем глобальную модель (жизни)
            _sessionModel.GlobalLives
                .Subscribe(_healthBarView.UpdateLives)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}