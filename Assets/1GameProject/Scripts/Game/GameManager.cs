using UnityEngine;
using UnityEngine.SceneManagement;

namespace _1GameProject.Scripts.Game
{
    public class GameManager :  MonoBehaviour
    {

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }
        
    }
}