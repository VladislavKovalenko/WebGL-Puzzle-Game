// FILE: Scripts/GameFlow/Gameplay/UI/LevelEndWindowView.cs

using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.End
{
    public class LevelEndWindowView : MonoBehaviour
    {
        [Header("Экран Победы")]
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private Button _continueButton; 
        // Здесь же в _winPanel на сцене у вас будет лежать Image улыбающегося деда и текст "Поздравляю"

        [Header("Экран Поражения (Скример)")]
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private Button _toMenuButton;
        // А здесь Image страшного деда на весь экран и текст "Ты проиграл"

        // Отдаем события нажатий наружу
        public IObservable<Unit> OnContinueClicked => _continueButton.OnClickAsObservable();
        public IObservable<Unit> OnToMenuClicked => _toMenuButton.OnClickAsObservable();

        public void ShowWin()
        {
            gameObject.SetActive(true);
            _winPanel.SetActive(true);
            _losePanel.SetActive(false);
        }

        public void ShowLose()
        {
            gameObject.SetActive(true);
            _winPanel.SetActive(false);
            _losePanel.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _winPanel.SetActive(false);
            _losePanel.SetActive(false);
        }
    }
}