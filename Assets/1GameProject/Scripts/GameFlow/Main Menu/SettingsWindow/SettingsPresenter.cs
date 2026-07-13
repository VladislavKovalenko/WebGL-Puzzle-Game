using System;
using UniRx;
using Zenject;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.Settings;
using UnityEngine;

namespace _1GameProject.Scripts.UI.SettingsWindow
{
    public class SettingsPresenter : IInitializable, IDisposable
    {
        private readonly SettingsModel _model;
        private readonly SettingsWindowView _view;
        private readonly SignalBus _signalBus;
        private readonly CompositeDisposable _disposables = new();

        // Временные переменные (изменяются пока двигаем слайдеры)
        private int _tempFps;
        private int _tempVolume;

        public SettingsPresenter(SettingsModel model, SettingsWindowView view, SignalBus signalBus)
        {
            _model = model;
            _view = view;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<SettingsMenuOpenSignal>(OpenWindow);

            // Подписки на кнопки UI
            _view.OnCloseClicked.Subscribe(_ => CloseWindow()).AddTo(_disposables);
            _view.OnApplyClicked.Subscribe(_ => ApplyChanges()).AddTo(_disposables);
            _view.OnResetClicked.Subscribe(_ => ResetToDefaults()).AddTo(_disposables);

            // Обработка слайдера ГРОМКОСТИ
            _view.OnVolumeChanged.Subscribe(val =>
            {
                _tempVolume = (int)val;
                _view.UpdateTexts(_tempFps, _tempVolume); 
                
                // СРАЗУ меняем громкость в игре для предпросмотра
                _model.PreviewSettings(_tempFps, _tempVolume);
            }).AddTo(_disposables);

            // Обработка слайдера FPS
            _view.OnFpsChanged.Subscribe(val =>
            {
                _tempFps = (int)val;
                _view.UpdateTexts(_tempFps, _tempVolume); 
                
                // СРАЗУ меняем FPS в игре для предпросмотра
                _model.PreviewSettings(_tempFps, _tempVolume);
            }).AddTo(_disposables);
        }

        private void OpenWindow()
        {
            // 1. Берем данные
            _tempFps = _model.CurrentFps;
            _tempVolume = _model.CurrentVolume;

            // 2. СНАЧАЛА включаем окно! Чтобы слайдеры проснулись и могли двигаться.
            _view.Show();

            // 3. И только ТЕПЕРЬ двигаем ползунки.
            _view.InitValuesSilently(_tempFps, _tempVolume);
        }

        private void CloseWindow()
        {
            // Отменяем несохраненные изменения (возвращаем громкость назад)
            _model.RevertSettingsToSaved();
            
            _view.Hide();
            _signalBus.Fire<BackToMainMenuSignal>();
        }

        private void ResetToDefaults()
        {
            // Выставляем дефолтные константы
            _tempFps = SettingsModel.DefaultFps;
            _tempVolume = SettingsModel.DefaultVolume;

            _view.InitValuesSilently(_tempFps, _tempVolume);
            
            // Сразу даем услышать дефолтную громкость
            _model.PreviewSettings(_tempFps, _tempVolume);
        }

        private void ApplyChanges()
        {
            // Сохраняем физически (Preview уже применен)
            _model.SaveSettings(_tempFps, _tempVolume);
            
            CloseWindow();
        }

        public void Dispose()
        {
            _signalBus.TryUnsubscribe<SettingsMenuOpenSignal>(OpenWindow);
            _disposables.Dispose();
        }
    }
}