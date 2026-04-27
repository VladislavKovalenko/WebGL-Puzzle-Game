using UnityEngine;

namespace _1GameProject.Scripts.Events
{
    //Bootstrap Signals
    public class ServicesLoadedSignal { }
    
    //Marker-Interface
    public interface IEvent { }
    
    //Game Events
    #region Game Flow
    public class GameStateChangedEvent : IEvent { }
    public class StartGameRequestEvent : IEvent { }
    #endregion

    //Test Events
    public class TestGameSignal{ }
    
    #region Board
    public class BoardGeneratedEvent : IEvent { }
    public class CellPointerDownEvent : IEvent { }
    #endregion

    #region UI
    public class ButtonHoverEvent : IEvent { }
    #endregion
}
