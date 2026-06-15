using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu
{
    public class LevelsMenuPresenter : MonoBehaviour
    {
        [Inject] private LevelsModel _levelsModel;

        [SerializeField] private List<LevelButtonView> _levelButtons;

        private void Start()
        {
            // 1. Подписываемся на ВСЕ кнопки ровно ОДИН раз при старте сцены.
            foreach (var btn in _levelButtons)
            {
                btn.OnLevelClicked
                    .Subscribe(levelId => StartLevel(levelId))
                    .AddTo(this);
            }
        }

        private void OnEnable()
        {
            // 2. А вот визуал (замочки) обновляем каждый раз, когда панель открывается
            RefreshLevelsUI();
        }

        private void RefreshLevelsUI()
        {
            foreach (var btn in _levelButtons)
            {
                bool isUnlocked = _levelsModel.IsLevelUnlocked(btn.LevelIndex);
                btn.SetUnlockedState(isUnlocked);
            }
        }

        private void StartLevel(int levelId)
        {
            // 3. Так как мы подписались на все кнопки, 
            // дополнительно проверяем, можно ли запускать этот уровень
            if (!_levelsModel.IsLevelUnlocked(levelId))
            {
                Debug.Log($"[LevelsMenu] Уровень {levelId} заблокирован!");
                return;
            }

            // Здесь мы будем загружать игровую сцену и передавать в неё levelId
            Debug.Log($"Загрузка уровня {levelId}...");
            
            // GlobalGameData.CurrentLevel = levelId;
            // SceneManager.LoadScene("GamePlay");
        }
    }
}