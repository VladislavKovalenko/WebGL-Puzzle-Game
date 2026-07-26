using Zenject;

namespace _1GameProject.Scripts.DI
{
    public class BootstrapperSceneContext : MonoInstaller
    {

        public override void InstallBindings()
        {
            //пустой контекст, просто для инжекта зависимостей в сцену.
        }
    }
}
