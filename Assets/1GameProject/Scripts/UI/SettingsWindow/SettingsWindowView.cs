using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.Settings
{
    public class SettingsWindowView : MonoBehaviour
    {
        [Header("Слайдеры")]
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Slider _fpsSlider;

        [Header("Тексты текущих значений")]
        [SerializeField] private TextMeshProUGUI _volumeText;
        [SerializeField] private TextMeshProUGUI _fpsText;

        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;

        // Выводим события кнопок для Presenter
        public IObservable<Unit> OnCloseClicked => _closeButton.OnClickAsObservable();
        public IObservable<Unit> OnApplyClicked => _applyButton.OnClickAsObservable();
        public IObservable<Unit> OnResetClicked => _resetButton.OnClickAsObservable();
        
        // Выводим события изменения слайдеров
        public IObservable<float> OnVolumeChanged => _volumeSlider.onValueChanged.AsObservable();
        public IObservable<float> OnFpsChanged => _fpsSlider.onValueChanged.AsObservable();

        public void SetVolume(int volume)
        {
            _volumeSlider.value = volume;
            _volumeText.text = volume.ToString();
        }

        public void SetFps(int fps)
        {
            _fpsSlider.value = fps;
            _fpsText.text = fps.ToString();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}