using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameFlow.Level.End;
using _1GameProject.Scripts.GameFlow.Level.Hazards;
using _1GameProject.Scripts.GameFlow.Level.HUD;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator.SO;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator.BoardMVP;
using _1GameProject.Scripts.GameFlow.Level.Narrative;
using _1GameProject.Scripts.GameFlow.Level.Start;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class PlaySceneInstaller : MonoInstaller
    {
        [SerializeField] private TextAsset _wordsCsv;
        
        [SerializeField] private BoardView _boardView; 
        
        [SerializeField] private GrandpaView _grandpaView;
        [SerializeField] private HealthBarView _healthBarView;
        
        [SerializeField] private LevelEndWindowView _levelEndWindowView; // Перетащите сюда панель из Canvas
        
        public override void InstallBindings()
        {
            // === AUDIO & UI ===
            Container.Bind<FMODGameAudioManager>().FromComponentInHierarchy().AsSingle();
            Container.Bind<UIButtonSound>().FromComponentInHierarchy().AsSingle();
            
            // === СЛОВАРЬ ===
            Container.Bind<WordService>()
                .AsSingle()
                .WithArguments(_wordsCsv)
                .NonLazy();

            // === ГЛОБАЛЬНАЯ СЕССИЯ (Проверка заглушки) ===
            var session = Container.Resolve<GameSessionModel>();
            
            if (session.CurrentConfig == null) // ИСПРАВЛЕНО: CurrentConfig вместо CurrentLevelConfig
            {
                Debug.LogWarning("Запуск сцены без меню! Создаю тестовый конфиг 4x4.");
                session.CurrentConfig = ScriptableObject.CreateInstance<LevelConfigSO>();
                session.CurrentConfig.Columns = 4;
                session.CurrentConfig.Rows = 4;
                session.CurrentConfig.MinWordLength = 3; // Добавлено, чтобы генератор не сломался
                session.CurrentConfig.MaxWordLength = 6; // Добавлено
                session.CurrentConfig.Hazard = LevelHazardType.Flashlight;
            }

            // Биндим конфиг из глобальной сессии!
            Container.Bind<LevelConfigSO>().FromInstance(session.CurrentConfig).AsSingle();

            // === ИГРОВАЯ ДОСКА ===
            Container.Bind<BoardGenerator>().AsSingle();
            Container.Bind<BoardView>().FromInstance(_boardView).AsSingle();
            Container.BindInterfacesAndSelfTo<BoardPresenter>().AsSingle().NonLazy();
            
            // === ЛОКАЛЬНАЯ МОДЕЛЬ СЦЕНЫ ===
            // ИСПРАВЛЕНО: BindInterfacesAndSelfTo, чтобы Zenject вызвал Initialize()
            Container.BindInterfacesAndSelfTo<GameplayModel>().AsSingle().NonLazy();

            // === НАРРАТИВ И ЖИЗНИ (ДЕД) ===
            Container.Bind<GrandpaView>().FromInstance(_grandpaView).AsSingle();
            Container.Bind<HealthBarView>().FromInstance(_healthBarView).AsSingle();
            Container.BindInterfacesTo<GrandpaPresenter>().AsSingle().NonLazy();

            // === КОНЕЦ ИГРЫ ===
            Container.Bind<LevelEndWindowView>().FromInstance(_levelEndWindowView).AsSingle();
            Container.BindInterfacesTo<LevelEndPresenter>().AsSingle().NonLazy();
            
            // === ФОНАРИК ===
            Container.Bind<FlashlightView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesTo<HazardPresenter>().AsSingle();
            
        }
    }
}