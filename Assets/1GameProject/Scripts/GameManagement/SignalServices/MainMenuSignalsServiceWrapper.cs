using _1GameProject.Scripts.Events;
using UnityEngine;
using Zenject;
using UnityEngine;

namespace _1GameProject.Scripts.GameManagement.SignalServices
{
    public class MainMenuSignalsServiceWrapper : MonoBehaviour
    {
        [Inject] SignalBus SignalBus;

        public void GameStart()
        {
            SignalBus.Fire(new GameStartSignal());
        }
        
        public void RanksIsOpen()
        {
            SignalBus.Fire(new RanksMenuOpenSignal());
            Debug.Log("Сигнал отправлен");
        }
        public void SettingsIsOpen()
        {
            SignalBus.Fire(new SettingsMenuOpenSignal());
        }
        
        public void StoreIsOpen()
        {
            SignalBus.Fire(new StoreOpenSignal());
        }
        
        public void BackToMainMenu()
        {
            SignalBus.Fire(new BackToMainMenuSignal());
        }
        
        
        
        
        
        
    }
}