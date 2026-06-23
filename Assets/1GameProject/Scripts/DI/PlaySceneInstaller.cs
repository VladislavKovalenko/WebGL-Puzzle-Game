using _1GameProject.Scripts.Audio;
using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class PlaySceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            //Audio
            Container.Bind<FMODGameAudioManager>().FromComponentInHierarchy().AsSingle();
            
            //UI
            Container.Bind<UIButtonSound>().FromComponentInHierarchy().AsSingle();
            
            

            //Container.BindInterfacesTo<>().AsSingle();
            // etc.
        }
    }
}