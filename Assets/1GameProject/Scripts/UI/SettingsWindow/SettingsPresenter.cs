using System;
using UniRx;
using Zenject;
using _1GameProject.Scripts.Events;
using _1GameProject.Scripts.UI.SettingsWindow;
using UnityEngine;

namespace _1GameProject.Scripts.Settings
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
            // Подписка на открытие окна
            _signalBus.GetStream<SettingsMenuOpenSignal>()
                .Subscribe(_ => OpenWindow())
                .AddTo(_disposables);

            // Подписки на кнопки UI
            _view.OnCloseClicked.Subscribe(_ => CloseWindow()).AddTo(_disposables);
            _view.OnApplyClicked.Subscribe(_ => ApplyChanges()).AddTo(_disposables);
            _view.OnResetClicked.Subscribe(_ => ResetToDefaults()).AddTo(_disposables);

            // Реакция на ползунки (передаем во View, чтобы обновить текст)
            _view.OnVolumeChanged.Subscribe(val =>
            {
                _tempVolume = (int)val;
                _view.SetVolume(_tempVolume); 
            }).AddTo(_disposables);

            _view.OnFpsChanged.Subscribe(val =>
            {
                _tempFps = (int)val;
                _view.SetFps(_tempFps); 
            }).AddTo(_disposables);
        }

        private void OpenWindow()
        {
            // При открытии окна берем реальные сохраненные данные
            _tempFps = _model.CurrentFps;
            _tempVolume = _model.CurrentVolume;

            // Настраиваем UI
            _view.SetFps(_tempFps);
            _view.SetVolume(_tempVolume);

            _view.Show();
        }

        private void CloseWindow()
        {
            // Если игрок нажал Close, мы просто закрываем окно.
            // Временные переменные сотрутся при следующем OpenWindow().
            _view.Hide();
            _signalBus.Fire<BackToMainMenuSignal>();
        }

        private void ResetToDefaults()
        {
            // Сбрасываем ВРЕМЕННЫЕ переменные на дефолтные
            _tempFps = SettingsModel.DefaultFps;
            _tempVolume = SettingsModel.DefaultVolume;

            // Обновляем ползунки (в файл пока не сохраняем)
            _view.SetFps(_tempFps);
            _view.SetVolume(_tempVolume);
        }

        private void ApplyChanges()
        {
            // 1. Сохраняем в Яндекс Игры
            _model.SaveSettings(_tempFps, _tempVolume);
            
            // 2. Применяем настройки к движку Unity физически
            Application.targetFrameRate = _tempFps;
            
            // TODO: Применить громкость через FMOD. Например:
            // FMODUnity.RuntimeManager.GetVCA("vca:/Master").setVolume(_tempVolume / 100f);

            // Закрываем окно
            CloseWindow();
        }

        public void Dispose() => _disposables.Dispose();
    }
}