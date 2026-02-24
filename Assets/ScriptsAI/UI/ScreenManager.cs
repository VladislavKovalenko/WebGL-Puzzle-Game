using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScreenManager : MonoBehaviour
    {
        public enum ScreenId
        {
            MainMenu,
            Shop,
            Play
        }

        [Header("Screens UXML")]
        [SerializeField] private VisualTreeAsset mainMenuScreen;
        [SerializeField] private VisualTreeAsset shopScreen;
        [SerializeField] private VisualTreeAsset playScreen;

        [Header("Root Names")]
        [SerializeField] private string backgroundName = "Background";
        [SerializeField] private string contentRootName = "ScreenRoot";

        [Header("Buttons")]
        [SerializeField] private string toMainMenuButtonName = "ToMainMenuButton";
        [SerializeField] private string toShopButtonName = "ToShopButton";
        [SerializeField] private string toPlayButtonName = "ToPlayButton";

        [Header("Start")]
        [SerializeField] private ScreenId startScreen = ScreenId.MainMenu;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _contentRoot;
        private VisualElement _activeScreen;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;

            // Keep shared background in root and swap only content container.
            _ = FindOrCreate(backgroundName);
            _contentRoot = FindOrCreate(contentRootName);
        }

        private void Start()
        {
            ShowScreen(startScreen);
        }

        public void ShowScreen(ScreenId screen)
        {
            if (_activeScreen != null)
            {
                _activeScreen.RemoveFromHierarchy();
                _activeScreen = null;
            }

            var asset = GetAsset(screen);
            if (asset == null)
            {
                Debug.LogError($"Screen asset is not assigned: {screen}");
                return;
            }

            _activeScreen = asset.Instantiate();
            _contentRoot.Add(_activeScreen);
            BindNavigation(_activeScreen);
        }

        private void BindNavigation(VisualElement screenRoot)
        {
            BindButton(screenRoot, toMainMenuButtonName, () => ShowScreen(ScreenId.MainMenu));
            BindButton(screenRoot, toShopButtonName, () => ShowScreen(ScreenId.Shop));
            BindButton(screenRoot, toPlayButtonName, () => ShowScreen(ScreenId.Play));
        }

        private void BindButton(VisualElement root, string buttonName, Action onClick)
        {
            if (string.IsNullOrWhiteSpace(buttonName))
            {
                return;
            }

            var button = root.Q<Button>(buttonName);
            if (button != null)
            {
                button.clicked += onClick;
            }
        }

        private VisualTreeAsset GetAsset(ScreenId screen)
        {
            return screen switch
            {
                ScreenId.MainMenu => mainMenuScreen,
                ScreenId.Shop => shopScreen,
                ScreenId.Play => playScreen,
                _ => null
            };
        }

        private VisualElement FindOrCreate(string name)
        {
            var element = _root.Q<VisualElement>(name);
            if (element != null)
            {
                return element;
            }

            var created = new VisualElement { name = name };
            _root.Add(created);
            return created;
        }
    }
}
