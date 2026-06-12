using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.GameManagement;
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
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<BackToMainMenuSignal>();
            Container.DeclareSignal<SettingsMenuOpenSignal>();
            
            
            
            // Биндим модель (одна на всю игру)
            Container.Bind<SettingsModel>().AsSingle();
            
            
            Container.Bind<SettingsWindowView>()
                .FromComponentInHierarchy()
                .AsSingle();

            // Биндим Presenter (он сам вызовет свой Initialize)
            Container.BindInterfacesTo<SettingsPresenter>().AsSingle();
            
            
            
        }
    }
}