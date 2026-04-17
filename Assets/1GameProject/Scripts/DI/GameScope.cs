using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.UI.Buttons;
using VContainer;
using VContainer.Unity;

namespace _1GameProject.Scripts.DI
{
    public class GameScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<FMODGameAudioManager>();
            builder.RegisterComponentInHierarchy<UIButtonSound>();

        }
        
    }
}