using UnityEngine;

namespace _1GameProject.Scripts.Event_System
{
    public class Hero : MonoBehaviour
    {
        public int hp = 100;
        public int mana = 100;
        
        EventBinding<TestEvent> testEventBinding;
        EventBinding<PlayerEvent> playerEventBinding;

        void OnEnable()
        {
            testEventBinding = new EventBinding<TestEvent>(HandleTestEvent);
            EventBus<TestEvent>.Register(testEventBinding);
            playerEventBinding = new EventBinding<PlayerEvent>(HandlePlayerEvent);
            EventBus<PlayerEvent>.Register(playerEventBinding);
        }

        void OnDisable()
        {
            EventBus<TestEvent>.Deregister(testEventBinding);
            EventBus<PlayerEvent>.Deregister(playerEventBinding);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EventBus<TestEvent>.Raise(new TestEvent());
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                EventBus<PlayerEvent>.Raise(new PlayerEvent
                {
                    health = hp,
                    mana = mana
                });
            }
        }

        void HandleTestEvent(TestEvent testEvent)
        {
            Debug.Log("Тестовый ивент сработал");
        }

        void HandlePlayerEvent(PlayerEvent playerEvent)
        {
            Debug.Log($"Здоровья у нас: {playerEvent.health}, а маны {playerEvent.mana}");
        }
        
    }
}