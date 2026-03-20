using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using TouchPhase = UnityEngine.TouchPhase;

namespace _1GameProject.Scripts.Bootstrap
{
    public class BootstrapOrchestrator : MonoBehaviour
    {
        [Header("Загрузочный экран")]
        public GameObject loadingScreenPrefab;
        private LoadingScreenUI _loadingUI;
        private GameObject uiInstance;
        
        [Header("Загрузчик")] 
        [SerializeField] private GameObject[] systemPrefabs;   
        
        [Header("Настройки сцены")]
        [ShowNonSerializedField]
        private string targetScene = "Main Menu";
        
        private List<IBootLoadable> _loadables = new List<IBootLoadable>();
        private bool _allSystemsReady = false;
        private bool _userIsClicked = false;
        
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FMODResumeAudioContext();
#endif
        
        private void Awake()
        {
            // UI должен быть готов до Start других
            if (loadingScreenPrefab == null) Debug.LogError("[BootstrapOrchestrator] loadingScreenPrefab не назначен!");
            uiInstance = Instantiate(loadingScreenPrefab);
            _loadingUI =  uiInstance.GetComponent<LoadingScreenUI>();
            
        }
        
        void Start()
        {
            InstanceAndLink();
            SystemsInitialize();
            
        }

        void Update()
        {
            if (!_allSystemsReady) 
                CheckLoadingProgress();
            else if (!_userIsClicked)
                CheckForUserInput();
        }
        

        private void InstanceAndLink()
        {
            _loadables.Clear();
            
            foreach (var prefab in systemPrefabs)
            {
                if (prefab  == null) continue;
                
                var instance = Instantiate(prefab);
                var loadable = instance.GetComponent<IBootLoadable>();

                if (loadable != null)
                {
                    _loadables.Add(loadable);
                    Debug.Log($"  - {loadable.GetType().Name}");
                }

                else
                {
                    Debug.LogWarning($"[Bootstrap] {prefab.name} не содержит IBootLoadable!");
                }
            }
            
            Debug.Log($"[Bootstrap] {_loadables.Count} systems to load");
        }
        
        private void SystemsInitialize()
        {
            foreach (var loadable in _loadables)
            {
                loadable.Initialize();
            }
        }
        
        private void CheckLoadingProgress()
        {
            int readyCount = 0;
            string currentStatus = "Ожидание...";

            foreach (var loadable in _loadables)
            {
                if (loadable.IsReady) 
                    readyCount++;
                else
                {
                    if (string.IsNullOrEmpty(currentStatus) || currentStatus == "Ожидание...")
                        currentStatus = loadable.LoadingLabel ?? "Загрузка...";
                }
            }
            
            float progress01 = _loadables.Count > 0 ? (float)readyCount / _loadables.Count : 1f;
            
            _loadingUI?.UpdateProgress(progress01, currentStatus);

            if (readyCount >= _loadables.Count)
            {
                _allSystemsReady = true;
                _loadingUI?.ShowReady();
                Debug.Log("[Bootstrap] Все системы готовы");
            }
        }
        
        private void CheckForUserInput()
        {
            //Надо поискать еще способы, почему в остальных играх запуск и звук происходит без "Press any key"
            bool anyKeyboardKeyPressed = Keyboard.current?.anyKey.wasPressedThisFrame ?? false;
            if(anyKeyboardKeyPressed) Debug.Log("Клавиатура нажата");
            bool anyMouse = Mouse.current?.leftButton.wasPressedThisFrame ?? false; 
            bool anyTouch = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
            
            if(!(anyKeyboardKeyPressed || anyMouse || anyTouch)) return;
            
            _userIsClicked = true;
            Debug.Log("[Пользователь] нажал кнопку");

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                FMODResumeAudioContext();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"FMODResumeAudioContext failed: {ex.Message}");
            }
#endif
            
            uiInstance.SetActive(false); // как альтернатива _loadingUI.Hide()
            SceneManager.LoadScene(targetScene);
        }
        
        
        
    }
}