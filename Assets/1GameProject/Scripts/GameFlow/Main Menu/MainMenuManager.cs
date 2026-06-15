using _1GameProject.Scripts.Events;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Main_Menu
{
    public class MainMenuManager : MonoBehaviour
    {
        
        [Inject] SignalBus SignalBus;
        
        [Header("Кнопки")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _openLevelsButton;
        [SerializeField] private Button _openStoreButton;
        
        [Header("Объекты")]
        public  GameObject MainMenu;
        public  GameObject Levels;
        public  GameObject Store;
        

        private void Start()
        {
            _startGameButton.OnClickAsObservable()
                .Subscribe(_ => StartGame())
                .AddTo(this);   
            
            _openLevelsButton.OnClickAsObservable()
                .Subscribe(_ => OpenLevels())
                .AddTo(this);
            
            _openStoreButton.OnClickAsObservable()
                .Subscribe(_ => OpenStore())
                .AddTo(this);
            
            _settingsButton.OnClickAsObservable()
                .Subscribe(_ => OpenSettings())
                .AddTo(this);

            _startGameButton.OnPointerEnterAsObservable();
            //TODO надо обдумать звук для кнопок или я могу тут вызывать Button Sound Manager напрямую
            //Либо я могу событие сделать и он подсосется, но это уже бессмысленно, архитектуру надо было тогда сразу
            //строить как сложные события (события 2 (наведение на кнопку и нажатие), а на них тег кнопки (обычная, выбирающая, закрывающая и т.д.)) и по тегу звуки выбирать
            //Тег не обязательно в инспекторе, можно свой enum написать 
            //Либо я могу как компонент сделать. Button Sound Manager компонент, который сам определяет что за тип кнопки
            //И уже подбирает под действие звук. Возможно для uGUI это попроще даже будет. Но это надо на каждую кнопку руками скрипт кидать.
            //С другой стороны у компонента преимущество, ему не нужно дублировать события в Installer DI для меню и сцены отдельно
            //Но как будт нарушается единство кода.

        }
        

        public void StartGame()
        {
            SceneManager.LoadScene("GamePlay");
        }
        
        public void OpenLevels()
        {
            Debug.Log("Сигнал получен");
            MainMenu.SetActive(false);
            Levels.SetActive(true);
            
            //прикольно, что можно обращаться через компонент к объекту
            _settingsButton.gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            // 1. Выключаем Главное Меню
            //MainMenu.SetActive(false);
            
            // 2. Стреляем сигналом! Менеджер больше сам окно не включает!
            SignalBus.Fire<SettingsMenuOpenSignal>();
        }

        public void OpenStore()
        {
            MainMenu.SetActive(false);
            Store.SetActive(true);
        }

        public void BackToMainMenu()
        {
            //Settings
            Levels.SetActive(false);
            Store.SetActive(false);
            MainMenu.SetActive(true);
        }
        
        
        
        
    }
}

