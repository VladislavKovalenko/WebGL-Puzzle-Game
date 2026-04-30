using System;
using _1GameProject.Scripts.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zenject;
using UniRx;

namespace _1GameProject.Scripts.GameManagement
{
    public class MainMenuManager : MonoBehaviour
    {
        [Inject] SignalBus SignalBus;
        
        public  GameObject MainMenu;
        public  GameObject Ranks;
        public  GameObject Settings;
        public  GameObject Store;

        private void Start()
        {
            SignalBus.GetStream<GameStartSignal>()
                .Subscribe(_ => StartGame())
                .AddTo(this);
            
            SignalBus.GetStream<RanksMenuOpenSignal>()
                .Subscribe(_ => OpenRanks())
                .AddTo(this);

            SignalBus.GetStream<StoreOpenSignal>()
                .Subscribe(_ => OpenStore())
                .AddTo(this);

            SignalBus.GetStream<BackToMainMenuSignal>()
                .Subscribe(_ => BackToMainMenu())
                .AddTo(this);
        }
        

        public void StartGame()
        {
            SceneManager.LoadScene("GamePlay");
        }
        
        public void OpenRanks()
        {
            Debug.Log("Сигнал получен");
            MainMenu.SetActive(false);
            Ranks.SetActive(true);
        }

        public void OpenSettings()
        {
        }

        public void OpenStore()
        {
            MainMenu.SetActive(false);
            Store.SetActive(true);
        }

        public void BackToMainMenu()
        {
            //Settings
            Ranks.SetActive(false);
            Store.SetActive(false);
            MainMenu.SetActive(true);
        }
        
        
        
        
    }
}