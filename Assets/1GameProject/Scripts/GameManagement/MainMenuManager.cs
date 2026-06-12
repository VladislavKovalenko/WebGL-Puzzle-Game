using System;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zenject;
using UniRx;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameManagement
{
    public class MainMenuManager : MonoBehaviour
    {
        
        [Header("Кнопки")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _openRanksButton;
        [SerializeField] private Button _openStoreButton;
        [SerializeField] private Button _backToMainMenuFromRankButton;
        
        [Header("Объекты")]
        public  GameObject MainMenu;
        public  GameObject Ranks;
        public  GameObject Settings;
        public  GameObject Store;
        

        private void Start()
        {
            _startGameButton.OnClickAsObservable()
                .Subscribe(_ => StartGame())
                .AddTo(this);
            
            _openRanksButton.OnClickAsObservable()
                .Subscribe(_ => OpenRanks())
                .AddTo(this);
            
            _openStoreButton.OnClickAsObservable()
                .Subscribe(_ => OpenStore())
                .AddTo(this);
            
            _backToMainMenuFromRankButton.OnClickAsObservable()
                .Subscribe(_ => BackToMainMenu())
                .AddTo(this);
            
            _settingsButton.OnClickAsObservable()
                .Subscribe(_ => OpenSettings())
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
            Settings.SetActive(true);
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