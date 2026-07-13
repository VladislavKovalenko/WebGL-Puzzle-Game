using System;
using _1GameProject.Scripts.GameData.SO;
using _1GameProject.Scripts.GameFlow.Level.Start;
using UnityEngine;
using Zenject;
using YG;
using DG.Tweening;

namespace _1GameProject.Scripts.GameFlow.Level.Hazards
{
    public class HazardPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly LevelConfig _currentConfig;
        private readonly GameplayModel _gameplayModel;
        private readonly FlashlightView _flashlightView;

        private bool _isMobileAutoMode;
        private Vector2 _autoPosition;
        private Sequence _autoSequence;

        [Inject]
        public HazardPresenter(
            LevelConfig config,
            GameplayModel gameplayModel,
            FlashlightView flashlightView)
        {
            _currentConfig = config;
            _gameplayModel = gameplayModel;
            _flashlightView = flashlightView;
        }

        public void Initialize()
        {
            _flashlightView.Disable();

            if (_currentConfig.Hazard == LevelHazardType.Flashlight)
            {
                _flashlightView.Init();

                _isMobileAutoMode = YG2.envir.isMobile || YG2.envir.isTablet;

                if (_isMobileAutoMode)
                {
                    float mobileRadius = Screen.height * 0.30f;
                    _flashlightView.SetRadius(mobileRadius);
                    _flashlightView.SetSoftness(mobileRadius * 0.25f);

                    _autoPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
                    StartAutoFlashlight();
                }
                else
                {
                    float pcRadius = Screen.height * 0.15f;
                    _flashlightView.SetRadius(pcRadius);
                    _flashlightView.SetSoftness(pcRadius * 0.25f);
                }
            }
        }

        private void StartAutoFlashlight()
        {
            float minX = Screen.width * _flashlightView.MinXPercent;
            float maxX = Screen.width * _flashlightView.MaxXPercent;
            float minY = Screen.height * _flashlightView.MinYPercent;
            float maxY = Screen.height * _flashlightView.MaxYPercent;

            float targetX = UnityEngine.Random.Range(minX, maxX);
            float targetY = UnityEngine.Random.Range(minY, maxY);

            Vector2 nextPoint = new Vector2(targetX, targetY);

            float moveDuration = UnityEngine.Random.Range(1.5f, 3.5f);
            float pauseDuration = UnityEngine.Random.Range(0.2f, 1.0f);

            _autoSequence = DOTween.Sequence()
                .Append(DOTween.To(() => _autoPosition, x => _autoPosition = x, nextPoint, moveDuration).SetEase(Ease.InOutSine))
                .AppendInterval(pauseDuration)
                .OnComplete(StartAutoFlashlight);
        }

        public void Tick()
        {
            if (_gameplayModel.CurrentState.Value != GameState.Playing)
            {
                if (_currentConfig.Hazard == LevelHazardType.Flashlight)
                    _flashlightView.Disable();
                return;
            }

            if (_currentConfig.Hazard == LevelHazardType.Flashlight)
            {
                if (_isMobileAutoMode)
                {
                    _flashlightView.UpdatePosition(_autoPosition);
                }
                else
                {
                    _flashlightView.UpdatePosition(Input.mousePosition);
                }
            }
        }

        public void Dispose()
        {
            if (_autoSequence != null)
            {
                _autoSequence.Kill();
            }
        }
    }
}