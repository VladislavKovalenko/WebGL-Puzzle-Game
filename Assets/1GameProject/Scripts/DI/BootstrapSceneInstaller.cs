using _1GameProject.Scripts.Events;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class BootstrapSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);

            Container.DeclareSignal<ServicesLoadedSignal>();
            
            

        }
    }
}