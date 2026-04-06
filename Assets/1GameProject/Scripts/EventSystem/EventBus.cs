using System.Collections.Generic;

namespace _1GameProject.Scripts.EventSystem
{
    public class EventBus<T> where T : IEvent
    {
        private static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
        
        public static void Register(EventBinding<T> binding) => bindings.Add(binding);
        public static void Deregister(EventBinding<T> binding) => bindings.Remove(binding);
        
        //принимает события и вызывает оба действия в привязке
        public static void Raise(T @event)
        {
            foreach (var binding in bindings)
            {
                binding.OnEvent.Invoke(@event);
                binding.OnEventNoArgs.Invoke();
            }
        }
        
        //На этот моменте я понял, что своя шина событий это хуйня и лучше использовать Unity R3, там полный менеджмент из коробки.
        public static void Clear() => bindings.Clear();

        public static void ALLClear()
        {
            EventBus<TestEvent>.Clear();
            EventBus<PlayerEvent>.Clear();
        }
        
    }
}