using _1GameProject.Scripts.GameFlow.Level.Start;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.Narrative
{
    public class GrandpaView : MonoBehaviour
    {
        [SerializeField] private Image _grandpaImage;

        [Header("Спрайты состояний")]
        [SerializeField] private Sprite _spriteCalm;
        [SerializeField] private Sprite _spriteWary;
        [SerializeField] private Sprite _spriteFrowning;
        [SerializeField] private Sprite _spriteAngry;
        [SerializeField] private Sprite _spriteFurious;
        [SerializeField] private Sprite _spriteDefeated;

        public void SetState(GrandpaState state)
        {
            _grandpaImage.color = Color.white; 
            
            _grandpaImage.sprite = state switch
            {
                GrandpaState.Calm => _spriteCalm,
                GrandpaState.Wary => _spriteWary,
                GrandpaState.Frowning => _spriteFrowning,
                GrandpaState.Angry => _spriteAngry,
                GrandpaState.Furious => _spriteFurious,
                GrandpaState.Defeated => _spriteDefeated,
                _ => _spriteCalm
            };
        }
    }
}
