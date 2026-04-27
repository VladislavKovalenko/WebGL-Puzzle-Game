using Cysharp.Threading.Tasks;

namespace _1GameProject.Scripts.Bootstrap.Interfaces_for_Services
{
    public interface IAnalyticsService
    {
        UniTask Initialize();
        void TrackEvent(string eventName);
    }
}