using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.HUD
{
    public class HealthBarView : MonoBehaviour
    {
        [Tooltip("Список компонентов Image жизней (от первой до пятой)")]
        [SerializeField] private List<Image> _heartImages;

        [Header("Спрайты")]
        [SerializeField] private Sprite _activeHeart;
        [SerializeField] private Sprite _emptyHeart;

        public void UpdateLives(int currentLives)
        {
            for (int i = 0; i < _heartImages.Count; i++)
            {
                // Если индекс меньше текущих жизней - ставим целое сердце, иначе пустое
                _heartImages[i].sprite = i < currentLives ? _activeHeart : _emptyHeart;
                
                // Убедимся, что цвет белый (чтобы картинка отображалась как есть)
                _heartImages[i].color = Color.white; 
            }
        }
    }
}