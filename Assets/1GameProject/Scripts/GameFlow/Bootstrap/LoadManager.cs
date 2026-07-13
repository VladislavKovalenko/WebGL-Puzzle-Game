using _1GameProject.Scripts.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using YG;
using Zenject;

namespace _1GameProject.Scripts.GameFlow.Bootstrap
{
    public class LoadManager : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;

        private void OnEnable()
        {
            _signalBus.Subscribe<AllServicesAreLoadedSignal>(OnServicesLoaded);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<AllServicesAreLoadedSignal>(OnServicesLoaded);
        }

        private void OnServicesLoaded(AllServicesAreLoadedSignal signal)
        {
            HandleServicesLoadedAsync().Forget();
        }

        private async UniTaskVoid HandleServicesLoadedAsync()
        {
            Debug.Log("[LoadingFlow] Services ready, waiting for user gesture...");

            await WaitForUserGestureAsync();

            Debug.Log("[LoadingFlow] User gesture detected. Starting game...");

            YG2.GameReadyAPI();

            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private async UniTask WaitForUserGestureAsync()
        {
            bool isPressed = false;

            using var trace = InputSystem.onAnyButtonPress.CallOnce(ctrl => isPressed = true);

            await UniTask.WaitUntil(() => isPressed);
        }
    }
}
