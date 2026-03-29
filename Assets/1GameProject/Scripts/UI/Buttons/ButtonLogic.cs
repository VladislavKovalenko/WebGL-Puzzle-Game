using UnityEngine.SceneManagement;

namespace _1GameProject.Scripts.UI.Buttons
{
    public class ButtonLogic
    {
        public void BackToMain()
        {
            SceneManager.LoadScene("Main Menu");
        }
    }
}