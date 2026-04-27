using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;

namespace _1GameProject.Scripts.Bootstrap
{
    public class Bootstraper : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject loadingScreenPrefab;

        [Inject] private DiContainer _container;
        [Inject] private SignalBus _signalBus;
        [Inject] private IAnalyticsService _analytics;
        //[Inject] private IAudioService _audio;

        private LoadingScreenUgui _loadingUI;

        private async UniTaskVoid Start()
        {
            try
            {
                await InitializeBootstrap();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstraper] Fatal error: {ex}");
            }
        }

        private async UniTask InitializeBootstrap()
        {
            // --- Защита от null с детальным логом ---
            if (_container == null)
            {
                Debug.LogError("[Bootstraper] _container is NULL! Убедись, что на сцене есть SceneContext с Installer.");
                return;
            }

            if (loadingScreenPrefab == null)
            {
                Debug.LogError("[Bootstraper] loadingScreenPrefab не назначен в инспекторе!");
                return;
            }

            if (_analytics == null)
            {
                Debug.LogError("[Bootstraper] IAnalyticsService не инжектирован! Проверь Installer.");
                return;
            }

            // if (_audio == null)
            // {
            //     Debug.LogError("[Bootstraper] IAudioService не инжектирован! Проверь Installer.");
            //     return;
            // }

            if (_signalBus == null)
            {
                Debug.LogError("[Bootstraper] SignalBus не инжектирован! Проверь SignalBusInstaller.Install().");
                return;
            }

            // 1. Создаём UI из префаба через Zenject
            _loadingUI = _container.InstantiatePrefabForComponent<LoadingScreenUgui>(loadingScreenPrefab);
            
            if (_loadingUI == null)
            {
                Debug.LogError("[Bootstraper] Не удалось создать LoadingScreenUgui. Убедись, что на префабе висит этот компонент.");
                return;
            }

            _loadingUI.UpdateProgress(0f, "Загрузка...");

            // 2. Инициализируем сервисы параллельно
            var tasks = new List<UniTask>
            {
                SafeInitialize(_analytics.Initialize(), "Analytics"),
                //SafeInitialize(_audio.Initialize(), "Audio")
            };

            await UniTask.WhenAll(tasks);

            _loadingUI.UpdateProgress(1f, "Готово!");

            // 3. Сигнал о готовности
            _signalBus.Fire(new ServicesLoadedSignal());
        }

        private async UniTask SafeInitialize(UniTask task, string serviceName)
        {
            try
            {
                await task;
                Debug.Log($"[Bootstraper] {serviceName} initialized.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstraper] {serviceName} failed: {ex.Message}");
            }
        }
    }
}