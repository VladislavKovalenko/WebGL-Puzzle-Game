using System;
using UnityEngine;
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

        [Header("ToggleVisualElements")]
        private VisualElement playVisualElement;
        private VisualElement shopVisualElement;
        private VisualElement rankingSystemVisualElement;
        
        
        private VisualElement elementToToggle;

        private bool isVisible = true;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            
            playButton =  _root.Q<Button>("PlayButton");
            shopButton =  _root.Q<Button>("ShopButton");
            rankingSystemButton =  _root.Q<Button>("RankingSystemButton");
            
            playVisualElement = _root.Q<VisualElement>("PlayScreen");
            shopVisualElement = _root.Q<VisualElement>("ShopScreen");
            rankingSystemVisualElement = _root.Q<VisualElement>("RankedScreen");
            
            // Keep shared background in root and swap only content container.
            //_ = FindOrCreate(backgroundName);
            //_contentRoot = FindOrCreate(contentRootName);
            
        }
        

        private void OnEnable()
        {
            playButton.clicked += TogglePlayScreen;
            shopButton.clicked += ToggleShopScreen;
            rankingSystemButton.clicked += ToggleRankingScreen;

        }


        private void OnDisable()
        {
            if (playButton != null) playButton.clicked -= TogglePlayScreen;
            if (shopButton != null) shopButton.clicked -= ToggleShopScreen;
            if (rankingSystemButton != null) rankingSystemButton.clicked -= ToggleRankingScreen;

        }

        private void ToggleRankingScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            rankingSystemVisualElement.style.display = DisplayStyle.Flex;
            Debug.Log("Ранговый экран");
            
        }

        private void ToggleShopScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            shopVisualElement.style.display = DisplayStyle.Flex;
            Debug.Log("Магазин открыт");
            //shopVisualElement.visible = !shopVisualElement.visible;
            
            
        }
        
        private void TogglePlayScreen()
        {
            rankingSystemVisualElement.style.display = DisplayStyle.None;
            playVisualElement.style.display  = DisplayStyle.None;
            shopVisualElement.style.display = DisplayStyle.None;
            
            playVisualElement.style.display  = DisplayStyle.Flex;
            Debug.Log("Игровой экран");
            
        }
        
    }
}
