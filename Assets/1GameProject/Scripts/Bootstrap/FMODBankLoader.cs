using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Audio
{
    public class FMODBankLoader : MonoBehaviour
    {
        [Header("Банки для загрузки в порядке загрузки")]
        public List<string> banksToLoad = new()
        {
            "MasterBank.strings",
            "MasterBank",
        };

        public string LoadingLabel { get; private set; } = "FMOD: Waiting...";
        public bool IsReady { get; private set; }

        public async UniTask LoadAsync()
        {
            await LoadBanks();
            await WaitForSampleData();

            LoadingLabel = "FMOD: Ready";
            IsReady = true;
        }

        private async UniTask LoadBanks()
        {
            for (int i = 0; i < banksToLoad.Count; i++)
            {
                string bankName = banksToLoad[i];
                LoadingLabel = $"FMOD: Loading {bankName} ({i + 1}/{banksToLoad.Count})";

                RuntimeManager.LoadBank(bankName, true);

                while (!RuntimeManager.HasBankLoaded(bankName))
                {
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                }
            }

            LoadingLabel = "FMOD: Loading sample data...";
        }

        private async UniTask WaitForSampleData()
        {
            bool allLoaded = false;
            while (!allLoaded)
            {
                allLoaded = true;
                foreach (string bankName in banksToLoad)
                {
                    var result = RuntimeManager.StudioSystem.getBank("bank:/" + bankName, out Bank bank);

                    if (result == FMOD.RESULT.OK)
                    {
                        bank.getSampleLoadingState(out LOADING_STATE state);
                        if (state != LOADING_STATE.LOADED)
                        {
                            allLoaded = false;
                            break;
                        }
                    }
                }

                if (!allLoaded)
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
        }
    }
}