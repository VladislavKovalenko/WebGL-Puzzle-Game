namespace _1GameProject.Scripts.Event_System
{
    public interface IEvent {}
    
    //лучше использовать структуры для оптимизации, они находятся в стеке, а не в куче и меньше нагружают сборщик мусора
    public struct TestEvent : IEvent {}

    public struct PlayerEvent : IEvent
    {
        public int health;
        public int mana;
        
    }
}