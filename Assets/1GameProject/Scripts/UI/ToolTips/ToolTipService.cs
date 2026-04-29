using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

namespace _1GameProject.Scripts.UI.ToolTips
{
    public class ToolTipService : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        //public TextMeshProUGUI TooltipLabel; в целом тут этим управлять не нужно пока, оно лишнее
        public GameObject TooltipObject;
        public float TimerLength = 2;
        
        private CancellationTokenSource _cts;

        public async void OnPointerEnter(PointerEventData eventData)
        {
            Cancel();
            _cts = new CancellationTokenSource();
            ShowDelayed(_cts.Token).Forget();
        }

       

        public void OnPointerExit(PointerEventData eventData)
        {
            Cancel();
            TooltipObject.SetActive(false);
        }

        private void OnDestroy() => Cancel();

        
        private void TooltipToCursorPosition()
        {
            if (TooltipObject.TryGetComponent<RectTransform>(out var rect))
            {
                rect.position = Input.mousePosition;
            }
            else
            {
                TooltipObject.transform.position = Input.mousePosition;
            }
        }
        
        private async UniTaskVoid ShowDelayed(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(TimerLength), cancellationToken: token);
            TooltipToCursorPosition();
            TooltipObject.SetActive(true);
        }
        
        private void Cancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        
    }
}


//обязательно отключать рейкаст таргеты у всех тултип объектов, у дочерних тоже, иначе OnPointerExit
//будет сразу отрабатывать после появления подсказки, т.к. перекрывается рейкаст другим объектом и считается выходом

//основная проблема этого скрипта, в том, что если курсор сходит с объекта - нужно убивать таймер. Иначе подсказка
//будет показана в рандомном месте на экране.