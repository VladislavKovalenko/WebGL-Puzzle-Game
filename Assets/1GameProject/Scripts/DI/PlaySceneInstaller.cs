using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.UI.Buttons;
using _1GameProject.Scripts.UI.Test;
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
            Container.Bind<TestSignalEvents>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LearningUiComponents>().FromComponentInHierarchy().AsSingle();
            
            

            //Container.BindInterfacesTo<>().AsSingle();
            // etc.
        }
    }
}