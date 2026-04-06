using UnityEngine;

namespace _1GameProject.Scripts.EventSystem
{
    public interface IEvent {}
    
    //лучше использовать структуры для оптимизации, они находятся в стеке, а не в куче и меньше нагружают сборщик мусора
    public struct TestEvent : IEvent {}

    public struct PlayerEvent : IEvent
    {
        public int health;
        public int mana;
        
    }
    
    //Game Manager Events
    public struct GameStateChangedEvent : IEvent
    {
        //public GameState NewState;
        //public GameState OldState;
    }
    
    public struct GameStartedEvent { }
    public struct GameFinishedEvent { }
    
    // Событие для запроса перезапуска (можно вызвать из UI)
    public struct GameRestartRequestEvent : IEvent { }

    // Событие, которое публикует WordValidator при нахождении слова
    public struct WordFoundEvent : IEvent
    {
        public string Word;
        public Vector2Int[] Cells;
        public int Points;
    }
    
    public struct LevelCompleteEvent : IEvent { }
}