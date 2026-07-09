// FILE: Scripts/GameFlow/Main_Menu/Levels_Menu/LevelsMenuPresenter.cs
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using _1GameProject.Scripts.GameData;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{
    public class LevelsMenuPresenter : MonoBehaviour
    {
        [Inject] private LevelsModel _levelsModel;
        [Inject] private GameSessionModel _sessionModel;

        [SerializeField] private List<LevelButtonView> _levelButtons;
        [SerializeField] private GameObject _levelsPrefab;
        

        private void Start()
        {
            foreach (var btn in _levelButtons)
            {
                btn.OnLevelClicked.Subscribe(StartLevel).AddTo(this);
            }
            
             
        }

        private void OnEnable()
        {
            RefreshLevelsUI();
        }

        private void RefreshLevelsUI()
        {
            foreach (var btn in _levelButtons)
            {
                bool isUnlocked = _levelsModel.IsLevelUnlocked(btn.LevelIndex);
                btn.gameObject.SetActive(isUnlocked);
                btn.SetUnlockedState(isUnlocked);
            }
        }

        private void StartLevel(int levelId)
        {
            if (!_levelsModel.IsLevelUnlocked(levelId)) return;

            // Индексы массивов начинаются с 0 (1-й уровень = индекс 0)
            _sessionModel.CurrentLevelIndex = levelId - 1;

            SceneManager.LoadScene(SceneNames.Gameplay);
        }
    }
}