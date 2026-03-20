using System;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace _1GameProject.Scripts.Bootstrap
{
    public class AnalyticsBootLoader : MonoBehaviour, IBootLoadable
    {
        public string LoadingLabel => _currentStatus;
        public bool IsReady => _isReady;

        private bool _isReady = false;
        private string _currentStatus = "Analytics: Waiting...";
        

        public void Initialize()
        {
            StartCoroutine(InitAnalytics());
        }

        private IEnumerator InitAnalytics()
        {
            _currentStatus = "Analytics: Initializing...";

            // Твоя инициализация аналитики
            // Например:
            // Unity.Services.Core.UnityServices.InitializeAsync();

            // Симуляция или реальное ожидание
            yield return new WaitForSeconds(0.5f);

            // Или ждёшь реальный коллбэк:
            // while (!UnityServices.State == ServicesInitializationState.Initialized)
            //     yield return null;

            _currentStatus = "Analytics: Ready";
            _isReady = true;
        }
    }
}