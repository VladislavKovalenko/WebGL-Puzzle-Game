using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.Settings;
using _1GameProject.Scripts.UI.SettingsWindow;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class MainMenuInstaller  : MonoInstaller
    {
        
        public override void InstallBindings()
        {
            
            
            Container.DeclareSignal<BackToMainMenuSignal>();
            Container.DeclareSignal<SettingsMenuOpenSignal>();
            
            
            
            // привязываем класс SettingsModel и все его интерфейсы (IInitializable)
            Container.BindInterfacesAndSelfTo<SettingsModel>().AsSingle();
            
            
            Container.Bind<SettingsWindowView>()
                .FromComponentInHierarchy()
                .AsSingle();

            // Биндим Presenter (он сам вызовет свой Initialize)
            Container.BindInterfacesTo<SettingsPresenter>().AsSingle();
            
             Container.Bind<FMODGameAudioManager>().FromComponentInHierarchy().AsSingle();
             
             Container.Bind<UIButtonSoundService>().FromComponentInHierarchy().AsSingle();

        }
    }
}