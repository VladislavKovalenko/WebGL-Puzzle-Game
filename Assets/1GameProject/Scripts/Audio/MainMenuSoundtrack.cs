using UnityEngine;

namespace Audio
{
    public class MainMenuSoundtrack : MonoBehaviour
    {
        public AudioClip soundtrack;
        private AudioSource _audio;

        void Start()
        {
            _audio = GetComponent<AudioSource>();
        
            _audio.clip = soundtrack;
            _audio.loop = true;
            _audio.Play();
        }
    
    }
}
