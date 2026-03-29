using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.UI.Test
{
    public class LearningUiComponents : MonoBehaviour
    {
        private Image image;
        
        Button button;
        

        void Start()
        {
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            
            image.color = Color.red;

            button.transition = Selectable.Transition.SpriteSwap;

            var colors = new ColorBlock();

            button.colors = colors;
    


        }
        
    }
}