using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _1GameProject.Scripts.Audio
{
    public class UIButtonSoundService : MonoBehaviour
    {
        [Inject] private SoundLibrarySO _soundLibrary;

        void Start()
        {
            // Получаем все корневые объекты ТОЛЬКО в текущей сцене (Главное Меню)
            var rootObjects = gameObject.scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // Ищем все кнопки, даже выключенные (Store, Settings, Levels)
                var buttons = root.GetComponentsInChildren<Button>(true);
                
                foreach (var button in buttons)
                {
                    // Проверяем, чтобы не повесить звук дважды на одну кнопку
                    if (button.GetComponent<UIButtonSound>() == null)
                    {
                        var buttonSound = button.gameObject.AddComponent<UIButtonSound>();
                        buttonSound.Init(_soundLibrary);
                    }
                }
            }
        }
    }
}