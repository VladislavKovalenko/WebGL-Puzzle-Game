using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _1GameProject.ScriptsAI.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScreenManager : MonoBehaviour
    {
        [Header("Screens UXML")]
        [SerializeField] private VisualTreeAsset mainMenuScreen;
        
        private UIDocument _document;
        private VisualElement _root;
        
        [Header("Buttons")]
        private Button playButton;
        private Button shopButton;
        private Button rankingSystemButton;
        private Button BackToMenuButton;

        [Header("ToggleVisualElements")]
        private VisualElement playVisualElement;
        private VisualElement shopVisualElement;
        private VisualElement rankingSystemVisualElement;
        
        
        private VisualElement elementToToggle;

        //private bool isVisible = true;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            
            playButton =  _root.Q<Button>("GameButton");
            shopButton =  _root.Q<Button>("StoreButton");
            rankingSystemButton =  _root.Q<Button>("RankButton");
            BackToMenuButton = _root.Q<Button>("BackToMenuButton");
            
            playVisualElement = _root.Q<VisualElement>("PlayScreen");
            shopVisualElement = _root.Q<VisualElement>("ShopScreen");
            rankingSystemVisualElement = _root.Q<VisualElement>("RankedScreen");
            
            // Keep shared background in root and swap only content container.
            //_ = FindOrCreate(backgroundName);
            //_contentRoot = FindOrCreate(contentRootName);
            
        }
        

        private void OnEnable()
        {
            playButton.clicked += StartGame;
            shopButton.clicked += ShowShopScreen;
            rankingSystemButton.clicked += ShowRankingScreen;
            //BackToMenuButton.clicked += TogglePlayScreen;
            
            var backButtons = _root.Query<Button>("BackToMenuButton").ToList();
        
            foreach (var button in backButtons)
            {
                button.clicked += ShowPlayScreen;
            }

        }


        private void OnDisable()
        {
            if (playButton != null) playButton.clicked -= StartGame;
            if (shopButton != null) shopButton.clicked -= ShowShopScreen;
            if (rankingSystemButton != null) rankingSystemButton.clicked -= ShowRankingScreen;
            if (BackToMenuButton != null) BackToMenuButton.clicked -= ShowRankingScreen;
            
            var backButtons = _root.Query<Button>("BackToMenuButton").ToList();
        
            foreach (var button in backButtons)
            {
                button.clicked += ShowPlayScreen;
            }

        }

        private void ShowRankingScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            rankingSystemVisualElement.style.display = DisplayStyle.Flex;
            Debug.Log("Ранговый экран");
            
        }

        private void ShowShopScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            shopVisualElement.style.display = DisplayStyle.Flex;
            Debug.Log("Магазин открыт");
            //shopVisualElement.visible = !shopVisualElement.visible;
            
            
        }
        
        private void ShowPlayScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            playVisualElement.style.display  = DisplayStyle.Flex;
            
            Debug.Log("Игровой экран");
            
        }

        private void StartGame()
        {
            SceneManager.LoadScene("GamePlay");
        }
        
    }
}
