
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
        
        //В целом сюда можно сразу биндить SO, так будет даже быстрее.

        public override void InstallBindings()
        {
            Container.Bind<IAsyncInitService>()
                .To<AnalyticsService>()
                .AsSingle()
                .NonLazy();
            
            //можно и без префаба, но настраивать FMOD будет неудобно
            Container.Bind<FMODBankLoader>()
                .FromComponentInNewPrefab(audioManagerPrefab)
                .AsSingle()
                .NonLazy();

            Container.Bind<IAsyncInitService>().To<FMODBankLoader>().FromResolve();
            
            //Levels Model глобальная для проекта, потому что к ней нужен доступ и из главного меню и из игры при начале и завершении уровня.
            Container.Bind<LevelsModel>().AsSingle();
            
            Container.Bind<GameSessionModel>().AsSingle().NonLazy();
            

        }
    }
}