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

        private bool _isInputLocked = true;

        private void Start()
        {
            Observable.Timer(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _isInputLocked = false)
                .AddTo(this);

            _startGameButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => StartGame()).AddTo(this);
            _openLevelsButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => OpenLevels()).AddTo(this);
            _openStoreButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => OpenStore()).AddTo(this);
            _settingsButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => OpenSettings()).AddTo(this);

            if (_closeStoreButton != null)
                _closeStoreButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => BackToMainMenu()).AddTo(this);

            if (_closeLevelsButton != null)
                _closeLevelsButton.OnClickAsObservable().Where(_ => !_isInputLocked).Subscribe(_ => BackToMainMenu()).AddTo(this);

            _signalBus.Subscribe<BackToMainMenuSignal>(BackToMainMenu);

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

        private void OnDestroy()
        {
            if (_signalBus != null)
            {
                _signalBus.TryUnsubscribe<BackToMainMenuSignal>(BackToMainMenu);
            }
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

