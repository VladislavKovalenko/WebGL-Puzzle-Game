using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Zenject;
using _1GameProject.Scripts.Events;

namespace _1GameProject.Scripts.Bootstrap
{
    public class LoadingScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject spinnerObject;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [SerializeField] private float spinDuration = 2f;

        private Tween _spinTween;
        
        [Inject] private SignalBus _signalBus;

        private void OnEnable()
        {
            _signalBus.Subscribe<AllServicesAreLoadedSignal>(OnServicesLoaded);

            if (spinnerObject != null)
            {
                _spinTween = spinnerObject.transform.DORotate(new Vector3(0, 0, 360), spinDuration, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart);
            }
            
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<AllServicesAreLoadedSignal>(OnServicesLoaded);
            
            if (_spinTween != null) _spinTween.Kill();
        }

        private void OnServicesLoaded(AllServicesAreLoadedSignal signal)
        {
            ShowReady("Нажмите на экран для старта");
        }

        public void ShowReady(string promptMessage = "Нажмите для старта")
        {
            if (statusText != null)
            {
                statusText.text = promptMessage;
                statusText.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            }
            
            if (_spinTween != null) _spinTween.timeScale = 0.5f;
            
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void UpdateProgress(float progress, string msg)
        {
             if (statusText != null && !string.IsNullOrEmpty(msg)) statusText.text = msg;
                 
        }
    }
}