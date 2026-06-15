using System;
using System.Text;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{
    [RequireComponent(typeof(Button))]
    public class LevelButtonView : MonoBehaviour
    {
        [Header("Настройки кнопки")]
        [Tooltip("Заполняется автоматически из имени объекта")]
        public int LevelIndex; 
        
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private GameObject _lockIcon; 

        private Button _button;

        public IObservable<int> OnLevelClicked => _button.OnClickAsObservable().Select(_ => LevelIndex);

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            // На всякий случай парсим при старте игры
            ParseLevelIndexFromName();
        }

        // Этот метод автоматически вызывается Unity прямо в Редакторе, 
        // когда ты переименовываешь объект или меняешь значения в Инспекторе!
        private void OnValidate()
        {
            // Позволяет видеть изменения текста еще до запуска игры
            ParseLevelIndexFromName();
        }

        public void SetUnlockedState(bool isUnlocked)
        {
            if (_button == null) _button = GetComponent<Button>();
            
            _button.interactable = isUnlocked;
            if (_lockIcon != null) _lockIcon.SetActive(!isUnlocked);
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
    }
}