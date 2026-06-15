using _1GameProject.Scripts.GameFlow.Main_Menu.Levels_Menu;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Level_End
{
    public class LevelEndManager : MonoBehaviour
    {
        [Inject] private LevelsModel _levelsModel;

        // Этот метод вызывается, когда игрок собрал нужное количество слов / очков
        public void OnPlayerVictory(int currentLevelIndex)
        {
            Debug.Log("Победа!");

            // 1. Показываем UI победы (звездочки, фейерверки)
            // _victoryPanel.Show();

            // 2. Говорим Модели обновить Яндекс Сохранения!
            _levelsModel.CompleteLevel(currentLevelIndex);
        }

        public void ReturnToMenu()
        {
            // Возвращаемся в меню. Когда меню загрузится, LevelsMenuPresenter 
            // отрисует кнопки, и следующий уровень уже будет разблокирован!
            // SceneManager.LoadScene("Main Menu");
        }
    }
}