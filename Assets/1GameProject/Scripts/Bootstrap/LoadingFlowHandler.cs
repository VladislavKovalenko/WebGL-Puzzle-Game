using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;
using _1GameProject.Scripts.Events;

namespace _1GameProject.Scripts.Bootstrap
{
    public class LoadingFlowHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string targetScene = "Main Menu";

        [Inject] private SignalBus _signalBus;

        private void OnEnable()
        {
            _signalBus.Subscribe<ServicesLoadedSignal>(OnServicesLoaded);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<ServicesLoadedSignal>(OnServicesLoaded);
        }
        
        private void OnServicesLoaded(ServicesLoadedSignal signal)
        {
            HandleServicesLoadedAsync().Forget();
        }

        private async UniTaskVoid HandleServicesLoadedAsync()
        {
            Debug.Log("[LoadingFlow] Services ready, waiting for user gesture...");
            
            await WaitForUserGestureAsync();

            Debug.Log("[LoadingFlow] User gesture detected. Starting game...");
            
            ResumeFmodAudio();
            SceneManager.LoadScene(targetScene);
        }

        private async UniTask WaitForUserGestureAsync()
        {
            while (true)
            {
                if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    return;
                
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    return;
                
                if (Touchscreen.current != null && 
                    Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    return;

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
        }

        private void ResumeFmodAudio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                FMODResumeAudioContext();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LoadingFlow] FMOD Resume failed: {ex.Message}");
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void FMODResumeAudioContext();
#endif
    }
}