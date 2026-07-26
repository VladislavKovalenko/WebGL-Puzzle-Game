using _1GameProject.Scripts.Bootstrap;
using _1GameProject.Scripts.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using YG;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Bootstrap
{
    public class LoadManager : MonoBehaviour
    {
        [Inject] private LoadingScreenManager _screenManager;
        [Inject] private SignalBus _signalBus;

        public void OnServicesReady()
        {
            HandleServicesLoadedAsync().Forget();
        }

        private async UniTaskVoid HandleServicesLoadedAsync()
        {
            var operation = SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            
            // 1. БЛОКИРУЕМ запуск сцены Главного Меню
            operation.allowSceneActivation = false;

            // 2. Ждем, пока сцена загрузится в оперативную память 
            // (Unity останавливает прогресс на 0.9, когда allowSceneActivation = false)
            while (operation.progress < 0.9f)
            {
                await UniTask.Yield();
            }

            // 3. Показываем "Нажмите любую кнопку"
            _screenManager.ShowReady("НАЖМИТЕ ЛЮБУЮ КНОПКУ");
            
            // 4. Говорим Яндексу, что мы готовы (его черная шторка пропадет)
            YG2.GameReadyAPI();
            
            // 5. Ждем клика пользователя по экрану
            await WaitForUserGestureAsync(() =>
            {
                // Будим FMOD
                FMODUnity.RuntimeManager.CoreSystem.mixerResume();
                var masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
                if (masterBus.isValid())
                {
                    masterBus.setMute(false);
                }
                Debug.Log("[LoadManager] Клик получен! FMOD разбужен.");
            });

            // 6. ТОЛЬКО ТЕПЕРЬ разрешаем сцене запуститься!
            // Именно сейчас в Главном Меню вызовется Start() и заиграет музыка
            operation.allowSceneActivation = true;

            // Ждем, пока Unity переключит сцену
            while (!operation.isDone)
            {
                await UniTask.Yield();
            }

            // Прячем загрузочный экран
            if (_screenManager != null)
            {
                _screenManager.Hide();
                Destroy(_screenManager.gameObject);
            }

            Destroy(gameObject);
        }

        private async UniTask WaitForUserGestureAsync(System.Action onGesture = null)
        {
            bool isPressed = false;

            // Подписка на клавиатуру/геймпад
            using var trace = UnityEngine.InputSystem.InputSystem.onAnyButtonPress.CallOnce(ctrl =>
            {
                if (!isPressed)
                {
                    isPressed = true;
                    onGesture?.Invoke();
                }
            });

            // Надежная проверка для WebGL (касания экрана и клики мышкой)
            while (!isPressed)
            {
                if (UnityEngine.InputSystem.Pointer.current != null && 
                    UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    isPressed = true;
                    onGesture?.Invoke();
                }
                await UniTask.Yield();
            }
        }
    }
}