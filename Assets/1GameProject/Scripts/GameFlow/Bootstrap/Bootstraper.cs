using System;
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
        //DI
        [Inject] private DiContainer _container; //для проверки UI, но спорно
        [Inject] private SignalBus _signalBus;
        
        //все наши сервисы
        [Inject] private List<IAsyncInitService> _asyncInitServices;
        
        
        [Header("UI")]
        [SerializeField] private GameObject loadingScreenPrefab;
        [Inject] private LoadingScreenManager _loadingUI;
        
        

        private async UniTaskVoid Start()
        {
            try
            {
                await InitializeBootstrap();
            }
            catch (System.Exception exept)
            {
                Debug.LogError($"[Bootstraper] Fatal error: {exept}");
            }
        }
        

        private async UniTask InitializeBootstrap()
        {
            _loadingUI.UpdateProgress(0f, "Загрузка...");
            
            //перебираем все сервисы
            var tasks = new List<UniTask>();
            
            foreach (var b in _asyncInitServices)
            {
                tasks.Add(SafeInitialize(b.Initialize(), b.GetType().Name));
            }
            
            await UniTask.WhenAll(tasks);
            
            _loadingUI.UpdateProgress(1f, "Готово!");
            
            _signalBus.Fire(new AllServicesAreLoadedSignal());
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


//Loading Screen и Load Manager существуют только в bootstrap сцене и менеджатся через реактивку
//Fmod, аналитика ... более управляем, потому что персистентны.


