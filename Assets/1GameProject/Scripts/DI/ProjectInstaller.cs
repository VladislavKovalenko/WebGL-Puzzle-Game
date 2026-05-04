
using _1GameProject.Scripts.Bootstrap;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
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
            
            //можно и без префаба, но настраивать FMOD будет неудобно
            Container.Bind<FMODBankLoader>()
                .FromComponentInNewPrefab(audioManagerPrefab)
                .AsSingle()
                .NonLazy();

            Container.Bind<IAsyncInitService>().To<FMODBankLoader>().FromResolve();
            

        }
    }
}