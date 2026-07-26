using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using _1GameProject.Scripts.GameFlow.Bootstrap;

namespace _1GameProject.Scripts.Bootstrap
{
    public class Bootstraper : MonoBehaviour
    {
        [Inject] private LoadManager _loadManager;
        
        //все наши сервисы
        [Inject] private List<IAsyncInitService> _asyncInitServices;
        
        [Inject] private LoadingScreenManager _bootstrapScreenManager;
        
        
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
            _bootstrapScreenManager.UpdateProgress("ЗАГРУЗКА...");
            
            //перебираем все сервисы
            var tasks = new List<UniTask>();
            
            foreach (var b in _asyncInitServices)
            {
                tasks.Add(SafeInitialize(b.Initialize(), b.GetType().Name));
            }
            
            await UniTask.WhenAll(tasks);
            
            _loadManager.OnServicesReady();
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


