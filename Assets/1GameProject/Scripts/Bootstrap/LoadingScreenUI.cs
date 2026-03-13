using UnityEngine;
using UnityEngine.UIElements;

namespace _1GameProject.Scripts.Bootstrap
{
    public class LoadingScreenUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private ProgressBar _bar;
        private Label _label;
        private VisualElement _clickPanel;

        private void Awake()
        {
            if (uiDocument == null) return;
            
            var root = uiDocument.rootVisualElement;
            _bar = root.Q<ProgressBar>("bar");
            _label = root.Q<Label>("loading-label");
            _clickPanel = root.Q<VisualElement>("click-panel");
            
            if(_clickPanel != null) _clickPanel.style.display = DisplayStyle.None;
        }
        
        public void UpdateProgress(float progressNormalized, string message = null)
        {
            if(_bar != null)  _bar.value = progressNormalized*100f;
            if(_label != null && message != null)  _label.text = message;
        }
        
        public void ShowReady()
        {
            if(_label != null) _label.text = "Готово! Нажмите для запуска";
            if(_clickPanel != null) _clickPanel.style.display = DisplayStyle.Flex;
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            // или uiDocument.visualTreeAsset = null; / enabled = false;
        }
        
        
    }
}