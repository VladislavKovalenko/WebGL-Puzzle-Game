using System;
using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

public class FMODAudioManager : MonoBehaviour
{
    private bool isLoaded;
    private EventInstance musicInstance;
    [SerializeField] private EventReference musicEvent;

    private void Start()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
    }

    void OnDestroy()
    {
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
    }
    
    
}


// isLoaded = FMODUnity.RuntimeManager.HaveAllBanksLoaded();
//
// if (isLoaded)


// RuntimeManager.PlayOneShot(musicEvent);