using System;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _1GameProject.Scripts.Bootstrap
{
    public class AnalyticsService : IAsyncInitService
    {
        public string LoadingLabel => _currentStatus;
        public bool IsReady => _isReady;

        private bool _isReady = false;
        private string _currentStatus = "Analytics: Waiting...";

        public async UniTask Initialize()
        {
            _currentStatus = "Analytics: Initializing...";

            try
            {
                // Тут будет инициализация SDK
                // типа await SomeSDK.InitializeAsync();

                await UniTask.Delay(500, delayType: DelayType.DeltaTime);

                _currentStatus = "Analytics: Ready";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _currentStatus = $"Analytics: Error ({ex.Message})";
                Debug.LogError($"[AnalyticsService] Init failed: {ex}");
                throw;
            }
        }

        public void TrackEvent(string eventName)
        {
            if (!_isReady)
            {
                Debug.LogWarning($"[AnalyticsService] Not ready. Event '{eventName}' skipped.");
                return;
            }

            Debug.Log($"[Analytics] Tracked: {eventName}");
        }
    }
}