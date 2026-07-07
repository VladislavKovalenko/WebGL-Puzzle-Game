// FILE: Scripts/GameFlow/Level/Hazards/HazardPresenter.cs
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator.SO;
using _1GameProject.Scripts.GameFlow.Level.Start;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Level.Hazards
{
    public class HazardPresenter : IInitializable, ITickable
    {
        private readonly LevelConfigSO _currentConfig;
        private readonly GameplayModel _gameplayModel;
        private readonly FlashlightView _flashlightView;

        [Inject]
        public HazardPresenter(
            LevelConfigSO config,
            GameplayModel gameplayModel,
            FlashlightView flashlightView)
        {
            _currentConfig = config;
            _gameplayModel = gameplayModel;
            _flashlightView = flashlightView;
        }

        public void Initialize()
        {
            Debug.Log($"[HazardPresenter] Initialize called. Hazard = {_currentConfig.Hazard}");
    
            _flashlightView.Disable();

            if (_currentConfig.Hazard == LevelHazardType.Flashlight)
            {
                Debug.Log("[HazardPresenter] Flashlight hazard detected, calling Init()");
                _flashlightView.Init();
            }
            else
            {
                Debug.Log($"[HazardPresenter] Hazard is {_currentConfig.Hazard}, NOT Flashlight");
            }
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
                Vector2 inputPos = Input.mousePosition;
                if (Input.touchCount > 0)
                    inputPos = Input.GetTouch(0).position;

                _flashlightView.UpdatePosition(inputPos);
            }
        }
    }
}