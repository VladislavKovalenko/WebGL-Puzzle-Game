using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace _1GameProject.Scripts.GameManagement
{
    public class MainMenuManager : MonoBehaviour
    {
        public  GameObject MainMenu;
        public  GameObject Ranks;
        public  GameObject Settings;
        public  GameObject Store;


        public void StartGame()
        {
            SceneManager.LoadScene("GamePlay");
        }
        
        public void OpenRanks()
        {
            MainMenu.SetActive(false);
            Ranks.SetActive(true);
        }

        public void OpenSettings()
        {
        }

        public void OpenStore()
        {
            MainMenu.SetActive(false);
            Store.SetActive(true);
        }

        public void BackToMainMenu()
        {
            //Settings
            Ranks.SetActive(false);
            Store.SetActive(false);
            MainMenu.SetActive(true);
        }
        
        
        
        
    }
}