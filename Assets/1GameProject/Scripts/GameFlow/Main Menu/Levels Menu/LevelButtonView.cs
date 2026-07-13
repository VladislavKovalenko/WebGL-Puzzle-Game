using System;
using System.Text;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{

    public enum LevelFrame
    {
       Starter,
       Neophyte,
       Amateur,
       Hardcore
       
    }
    
    
    [RequireComponent(typeof(Button))]
    public class LevelButtonView : MonoBehaviour
    {
        [Header("Тип рамки")] 
        [SerializeField] private LevelFrame Type;
        
        [Header("Настройки рамки")]  
        [SerializeField] private Sprite Starter;
        [SerializeField] private Sprite Neophyte;
        [SerializeField] private Sprite Amateur;
        [SerializeField] private Sprite Hardcore;
        
        public Image _frameImage;
        
        [Header("Иконки статуса")]
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _completedIcon;

        [Header("Настройки кнопки")]
        [Tooltip("Заполняется автоматически из имени объекта")]
        public int LevelIndex; 
        
        [SerializeField] private TextMeshProUGUI _levelText;

        private Button _button;

        public IObservable<int> OnLevelClicked => _button.OnClickAsObservable().Select(_ => LevelIndex);

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            _frameImage =  GetComponent<Image>();
            
            // На всякий случай парсим при старте игры
            ParseLevelIndexFromName();

            SetupButtonFrame();
        }
        
        

        // Этот метод автоматически вызывается Unity прямо в Редакторе, 
        // когда ты переименовываешь объект или меняешь значения в Инспекторе!
        private void OnValidate()
        {
            // Позволяет видеть изменения текста еще до запуска игры
            ParseLevelIndexFromName();
            
            SetupButtonFrame();
        }

        public void UpdateState(bool isUnlocked, bool isCompleted)
        {
            if (_button == null) _button = GetComponent<Button>();

            if (isCompleted)
            {
                _button.interactable = false;
                if (_lockIcon != null) _lockIcon.SetActive(false);
                if (_completedIcon != null) _completedIcon.SetActive(true);
            }
            else if (isUnlocked)
            {
                _button.interactable = true;
                if (_lockIcon != null) _lockIcon.SetActive(false);
                if (_completedIcon != null) _completedIcon.SetActive(false);
            }
            else
            {
                _button.interactable = false;
                if (_lockIcon != null) _lockIcon.SetActive(true);
                if (_completedIcon != null) _completedIcon.SetActive(false);
            }
        }

        /// <summary>
        /// Автоматически извлекает номер из имени объекта и настраивает кнопку.
        /// </summary>
        private void ParseLevelIndexFromName()
        {
            // Если текст не назначен, пытаемся найти его автоматически
            if (_levelText == null)
            {
                _levelText = GetComponentInChildren<TextMeshProUGUI>();
            }

            string extractedNumbers = ExtractNumbers(gameObject.name);

            // Пытаемся превратить строку в число (int)
            if (!string.IsNullOrEmpty(extractedNumbers) && int.TryParse(extractedNumbers, out int parsedIndex))
            {
                LevelIndex = parsedIndex; // Сохраняем для логики

                if (_levelText != null)
                {
                    _levelText.text = LevelIndex.ToString(); // Обновляем визуал
                }
            }
        }

        private string ExtractNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            StringBuilder numberBuilder = new StringBuilder();
            foreach (char symbol in text)
            {
                if (char.IsDigit(symbol)) numberBuilder.Append(symbol);
            }
            return numberBuilder.ToString();
        }
        
        private void SetupButtonFrame()
        {
            _frameImage.sprite = Type switch
            {
                LevelFrame.Starter  => Starter,
                LevelFrame.Neophyte => Neophyte,
                LevelFrame.Amateur  => Amateur,
                LevelFrame.Hardcore => Hardcore,
                _                   => Starter
            };
        }
        
        
    }
}