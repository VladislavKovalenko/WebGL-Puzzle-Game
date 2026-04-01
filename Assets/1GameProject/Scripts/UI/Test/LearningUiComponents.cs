using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.UI.Test
{
    public class LearningUiComponents : MonoBehaviour
    { 
        GameObject thisMyObject;
        

        void Start()
        {
            thisMyObject = GameObject.Find("Image (7)");
            BoxCollider2D bC = thisMyObject.AddComponent<BoxCollider2D>();
        }
    }
}