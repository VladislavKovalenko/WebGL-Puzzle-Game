using System.Collections.Generic;
using _1GameProject.Scripts.Bootstrap.Interfaces_for_Services;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Zenject;

namespace Audio
{
    public class FMODBankLoader : MonoBehaviour, IAsyncInitService
    {
        
        
        [Header("Банки для загрузки")]
        [FMODUnity.BankRef]
        public List<string> banks;
        

        public async UniTask Initialize()
        {
            LoadBanks();
            await CheckBanksLoaded();
        }

        private void LoadBanks()
        {
            foreach (string b in banks)
            {
                FMODUnity.RuntimeManager.LoadBank(b,true);
                Debug.Log("Loaded bank " + b);
            }
            
            //For Chrome / Safari browsers / WebGL.  Reset audio on response to user interaction (LoadBanks is called from a button press), to allow audio to be heard.
            
            FMODUnity.RuntimeManager.CoreSystem.mixerSuspend();
            FMODUnity.RuntimeManager.CoreSystem.mixerResume();
        }
        

        private async UniTask CheckBanksLoaded()
        {
            while (!FMODUnity.RuntimeManager.HaveAllBanksLoaded)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
            
            
        }
    }
}