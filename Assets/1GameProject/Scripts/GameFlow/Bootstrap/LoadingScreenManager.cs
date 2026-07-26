using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Zenject;

namespace _1GameProject.Scripts.Bootstrap
{
    public class LoadingScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject spinnerObject;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private float spinDuration = 2f;

        private Tween _spinTween;
        private Tween _textTween; // Ссылка на анимацию текста

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 100;
        }

        private void OnEnable()
        {
            if (spinnerObject != null)
            {
                _spinTween = spinnerObject.transform.DORotate(new Vector3(0, 0, 360), spinDuration, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetLink(spinnerObject); // Связываем анимацию с объектом
            }
        }

        private void OnDisable()
        {
            // Очищаем анимации при выключении
            if (_spinTween != null) _spinTween.Kill();
            if (_textTween != null) _textTween.Kill();
        }

        public void ShowReady(string promptMessage = "НАЖМИТЕ ЛЮБУЮ КНОПКУ")
        {
            Debug.Log($"[LoadingScreen] ShowReady: {promptMessage}");

            if (statusText != null)
            {
                statusText.text = promptMessage;
                
                // Добавили SetLink(statusText.gameObject), чтобы анимация умерла вместе с текстом
                _textTween = statusText.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(statusText.gameObject); 
            }
            
            if (_spinTween != null) _spinTween.timeScale = 0.5f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void UpdateProgress(string msg)
        {
             if (statusText != null && !string.IsNullOrEmpty(msg)) statusText.text = msg;
        }
    }
}