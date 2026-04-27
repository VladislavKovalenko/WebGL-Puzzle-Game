using _1GameProject.Scripts.Events;
using MyBox;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace _1GameProject.Scripts.UI.Test
{
    public class LearningUiComponents : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        readonly CompositeDisposable _disposables = new();

        
        GameObject thisMyObject;
        
        [AutoProperty] public RectTransform rectTransform;
        
        
        

        void Start()
        {
            rectTransform.SetPositionX(23);
            
            thisMyObject = GameObject.Find("Image (7)");
            BoxCollider2D bC = thisMyObject.AddComponent<BoxCollider2D>();
            
            _signalBus.GetStream<TestGameSignal>()
                .Subscribe((_ => Debug.Log("Нихуя ты навел на кнопочку"))).AddTo(_disposables);
        }
        
        public void Dispose() => _disposables.Dispose();
    }
}