using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace _1GameProject.Scripts.GameManagement
{
    public class MainMenuManager : MonoBehaviour
    {
        public void StartGame()
        {
            SceneManager.LoadScene("GamePlay");
        }
    }
}