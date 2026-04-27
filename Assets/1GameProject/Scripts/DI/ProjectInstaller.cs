
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
            Container.Bind<IAnalyticsService>()
                .To<AnalyticsService>()
                .AsSingle()
                .NonLazy();


            Container.Bind<FMODBankLoader>()
                .FromComponentInNewPrefab(audioManagerPrefab)
                .AsSingle()
                .NonLazy();



        }
    }
}