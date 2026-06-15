using UnityEngine;

namespace _1GameProject.Scripts.Events
{
    //главное эти сигналы случайно не засунуть внутрь другого класса
    
    
    // Game Flow
    public class GameStateChangedEvent : IEvent { }
    public class StartGameRequestEvent : IEvent { }

    
    // Board
    public class BoardGeneratedEvent : IEvent { }
    public class CellPointerDownEvent : IEvent { }
 

    // UI
    public class ButtonHoverEvent : IEvent { }
}
