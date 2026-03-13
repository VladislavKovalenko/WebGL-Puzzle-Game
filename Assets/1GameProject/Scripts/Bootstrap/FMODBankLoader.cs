    using System;
    using System.Collections;
    using System.Collections.Generic;
    using _1GameProject.Scripts.Bootstrap;
    using FMOD.Studio;
    using FMODUnity;
    using UnityEngine;

    namespace Audio
    {
        public class FMODBankLoader : MonoBehaviour, IBootLoadable
        {
            [Header("Банки для загрузки в порядке загрузки")]
            public List<String> banksToLoad = new List<string>()
            {
                "MasterBank.strings",
                "MasterBank",
            };

            public string LoadingLabel => _currentStatus;
            public bool IsReady() => _isReady;

            private string _currentStatus = "FMOD waiting,,,";
            private bool _isReady = false;

            public void Initialize()
            {
                StartCoroutine(LoadBanks());
            }

            private IEnumerator LoadBanks()
            {
                for (int i = 0; i < banksToLoad.Count; i++)
                {
                    string bankName = banksToLoad[i];
                    _currentStatus = $"FMOD: Loading {bankName} ({i + 1}/{banksToLoad.Count})";

                    RuntimeManager.LoadBank(bankName, true);
                    while (!RuntimeManager.HasBankLoaded(bankName))
                    {
                        yield return null;
                    }
                }

                _currentStatus = "FMOD: Loading sample data...";
                yield return StartCoroutine(WaitForSampleData());

                _currentStatus = "FMOD: Ready";
                _isReady = true;

            }

            private IEnumerator WaitForSampleData()
            {
                bool allLoaded = false;
                while (!allLoaded)
                {
                    allLoaded = true;
                    foreach (string bankName in banksToLoad)
                    {
                        Bank bank;

                        var result = RuntimeManager.StudioSystem.getBank("bank:/" + bankName, out bank);

                        if (result == FMOD.RESULT.OK)
                        {
                            bank.getSampleLoadingState(out LOADING_STATE sampleState);
                            if (sampleState != LOADING_STATE.LOADED)
                            {
                                allLoaded = false;
                                break;
                            }
                        }

                    }
                    
                    yield return null;
                }
            }
        }
    }
            
     