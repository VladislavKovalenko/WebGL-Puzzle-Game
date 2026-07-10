using System;
using System.Linq;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.GameData;
using _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Main_Menu
{
    public class MainMenuManager : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private GameSessionModel _sessionModel;
        [Inject] private LevelsModel _levelsModel;
        
        [Header("Кнопки открытия")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _openLevelsButton;
        [SerializeField] private Button _openStoreButton;

        [Header("Кнопки ЗАКРЫТИЯ (Назад в меню)")]
        [SerializeField] private Button _closeStoreButton;
        [SerializeField] private Button _closeLevelsButton;
        
        [Header("Панели (Объекты)")]
        public GameObject MainMenu;
        public GameObject Levels;
        public GameObject Store;
        public GameObject Settings;
        public GameObject MainMenuSelector;

        private void Start()
        {
            _startGameButton.OnClickAsObservable().Subscribe(_ => StartGame()).AddTo(this);   
            _openLevelsButton.OnClickAsObservable().Subscribe(_ => OpenLevels()).AddTo(this);
            _openStoreButton.OnClickAsObservable().Subscribe(_ => OpenStore()).AddTo(this);
            _settingsButton.OnClickAsObservable().Subscribe(_ => OpenSettings()).AddTo(this);

            if (_closeStoreButton != null)
                _closeStoreButton.OnClickAsObservable().Subscribe(_ => BackToMainMenu()).AddTo(this);
                
            if (_closeLevelsButton != null)
                _closeLevelsButton.OnClickAsObservable().Subscribe(_ => BackToMainMenu()).AddTo(this);

            _signalBus.GetStream<BackToMainMenuSignal>()
                .Subscribe(_ => BackToMainMenu())
                .AddTo(this);

            if (_sessionModel.AutoOpenLevelsMenu)
            {
                _sessionModel.AutoOpenLevelsMenu = false;
                OpenLevels();
            }
        }

        private void Update()
        {
            bool isMainMenuActive = !Levels.activeSelf && !Store.activeSelf && !Settings.activeSelf;
            
            _settingsButton.gameObject.SetActive(isMainMenuActive);
            MainMenuSelector.SetActive(isMainMenuActive);
        }

        public void StartGame()
        {
            int maxUnlockedLevel = _levelsModel.UnlockedLevels.Max();
            _sessionModel.CurrentLevelIndex = maxUnlockedLevel - 1;
            SceneManager.LoadScene(SceneNames.Gameplay);
        }
        
        public void OpenLevels()
        {
            MainMenu.SetActive(false);
            Levels.SetActive(true);
            _settingsButton.gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            _signalBus.Fire<SettingsMenuOpenSignal>();
        }

        public void OpenStore()
        {
            MainMenu.SetActive(false);
            Store.SetActive(true);
        }

        public void BackToMainMenu()
        {
            Levels.SetActive(false);
            Store.SetActive(false);
            MainMenu.SetActive(true);
        }
    }
}

