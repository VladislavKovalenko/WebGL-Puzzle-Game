using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.Narrative
{
    public class IntroSlideView : MonoBehaviour
    {
        [SerializeField] private Button _nextButton;

        public IObservable<Unit> OnNextClicked => _nextButton.OnClickAsObservable();

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}