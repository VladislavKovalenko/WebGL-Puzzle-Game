using _1GameProject.Scripts.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace _1GameProject.Scripts.UI.Test
{
    public class TestSignalEvents : MonoBehaviour, IPointerEnterHandler
    {
        [Inject] SignalBus _signalBus;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _signalBus.Fire<TestGameSignal>();
        }
    }
}