using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.Bootstrap;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameData.SO;
using _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu;
using Audio;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GameObject audioManagerPrefab;

        public override void InstallBindings()
        {
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
