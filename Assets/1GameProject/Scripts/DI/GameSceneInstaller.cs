using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.UI.Buttons;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class GameSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<FMODGameAudioManager>().AsSingle();
            Container.Bind<UIButtonSound>().AsSingle();

            //Container.BindInterfacesTo<>().AsSingle();
            // etc.
        }
    }
}