using _1GameProject.Scripts.Events;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class MainMenuInstaller  : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<GameStartSignal>();
            Container.DeclareSignal<RanksMenuOpenSignal>();
            Container.DeclareSignal<StoreOpenSignal>();
            Container.DeclareSignal<BackToMainMenuSignal>();
            
        }
    }
}