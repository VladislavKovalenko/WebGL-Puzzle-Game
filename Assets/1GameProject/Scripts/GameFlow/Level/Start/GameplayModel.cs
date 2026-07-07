using UniRx;
using Zenject;
using _1GameProject.Scripts.GameData;

namespace _1GameProject.Scripts.GameFlow.Level.Start
{
    public enum GameState { Playing, Win, Lose, Pause }
    public enum GrandpaState { Calm, Wary, Frowning, Angry, Furious, Defeated }

    public class GameplayModel : IInitializable
    {
        private readonly GameSessionModel _sessionModel;

        public ReactiveProperty<GameState> CurrentState { get; } = new(GameState.Playing);
        public ReactiveProperty<GrandpaState> CurrentGrandpaState { get; } = new(GrandpaState.Calm);

        public ReactiveProperty<int> WordsFound { get; } = new(0);
        public int TotalWords { get; private set; }

        [Inject]
        public GameplayModel(GameSessionModel sessionModel)
        {
            _sessionModel = sessionModel;
        }

        public void Initialize()
        {
            CurrentState.Value = GameState.Playing;
            WordsFound.Value = 0;
            
            _sessionModel.ResetLevelFlags();

            // Дед и проигрыш реагируют на глобальные жизни
            _sessionModel.GlobalLives.Subscribe(lives =>
            {
                UpdateGrandpaState(lives);
                
                if (lives <= 0 && CurrentState.Value == GameState.Playing)
                {
                    CurrentState.Value = GameState.Lose;
                }
            });
        }

        public void SetupLevel(int totalWords)
        {
            TotalWords = totalWords;
        }

        public void AddFoundWord()
        {
            if (CurrentState.Value != GameState.Playing) return;

            WordsFound.Value++;

            if (WordsFound.Value >= TotalWords)
            {
                CurrentState.Value = GameState.Win;
                
                if (!_sessionModel.TookDamageThisLevel)
                {
                    UnityEngine.Debug.Log("[Gameplay] Идеальное прохождение! +1 жизнь");
                    _sessionModel.Heal(1);
                }
            }
        }

        private void UpdateGrandpaState(int lives)
        {
            CurrentGrandpaState.Value = lives switch
            {
                5 => GrandpaState.Calm,
                4 => GrandpaState.Wary,
                3 => GrandpaState.Frowning,
                2 => GrandpaState.Angry,
                1 => GrandpaState.Furious,
                _ => GrandpaState.Defeated
            };
        }
    }
}