using _1GameProject.Scripts.Events;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _1GameProject.Scripts.UI.Buttons
{
    
    
    public class HoverReactionTestComponent
    {
        [Inject] UIButtonSound buttonSound;
        
        Image Background;

        private readonly CompositeDisposable d = new();
        
        void Start()
        {
            //var d = Disposable.CreateBuilder();
            
            Observable.EveryUpdate().Subscribe(_=>
            {
                ChangeImageColor(Background);
            }).AddTo(d);
            
            buttonSound.IsHover
                .Where(isHover => isHover = true)
                .Subscribe(_ => ChangeImageColor(Background));
            
        }


        void ChangeImageColor(Image image)
        {
            image.color = Color.red;
        }

        private void OnDestroy()
        {
            d.Dispose();
        }
        
        
        
        
    }
}