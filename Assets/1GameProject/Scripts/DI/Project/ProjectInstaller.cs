using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.Bootstrap;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameData.SO;
using _1GameProject.Scripts.GameFlow.Bootstrap;
using _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu;
using Audio;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private GameObject loadManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;

        public override void InstallBindings()
        {
            //контейнер сигналов
            SignalBusInstaller.Install(Container);
            
            //Сигналы
            Container.DeclareSignal<UserGestureSignal>();
            
            //Объекты
            Container.Bind<LoadingScreenManager>()
                .FromComponentInNewPrefab(loadingScreenPrefab)
                .AsSingle()
                .NonLazy();

            Container.Bind<LoadManager>()
                .FromComponentInNewPrefab(loadManagerPrefab)
                .AsSingle()
                .NonLazy();

            // === ДАЛЕЕ ВСЁ КАК БЫЛО ===
            Container.Bind<IAsyncInitService>()
                .To<AnalyticsService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<FMODBankLoader>()
                .FromComponentInNewPrefab(audioManagerPrefab)
                .AsSingle()
                .NonLazy();

            Container.Bind<IAsyncInitService>().To<FMODBankLoader>().FromResolve();

            Container.Bind<FMODFocusHandler>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
            
            Container.Bind<LevelsModel>().AsSingle();

            Container.Bind<GameSessionModel>().AsSingle().NonLazy();
        }
    }
}
