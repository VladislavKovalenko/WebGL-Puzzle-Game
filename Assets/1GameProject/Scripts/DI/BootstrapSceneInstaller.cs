using _1GameProject.Scripts.Bootstrap;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using _1GameProject.Scripts.Events;
using Audio;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class BootstrapSceneInstaller : MonoInstaller
    {
        [SerializeField] private GameObject LoadingScreenManagerPrefab;
        
        [SerializeField] private GameObject LoadManagerPrefab;
        
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);

            Container.DeclareSignal<AllServicesisLoadedSignal>();
            
            Container.Bind<LoadingScreenManager>()
                .FromComponentInNewPrefab(LoadingScreenManagerPrefab)
                .AsSingle()
                .NonLazy();
            
            Container.Bind<LoadManager>()
                .FromComponentInNewPrefab(LoadManagerPrefab)
                .AsSingle()
                .NonLazy();

        }
    }
}