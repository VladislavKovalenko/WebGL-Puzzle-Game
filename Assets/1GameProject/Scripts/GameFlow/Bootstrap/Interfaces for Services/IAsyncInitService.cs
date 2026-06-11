using Cysharp.Threading.Tasks;

namespace _1GameProject.Scripts.Bootstrap.Interfaces_for_Services
{
    public interface IAsyncInitService
    {
        UniTask Initialize();
        
        //этот интерфейс нужен для await UniTask.WhenAll(tasks);
        //чтобы ждать полной инициализации
    }
}