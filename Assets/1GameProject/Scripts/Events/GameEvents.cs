using UnityEngine;

namespace _1GameProject.Scripts.Events
{
    //Marker-Interface
    public interface IEvent { }
    
    //Game Events
    #region Game Flow
    public struct GameStateChangedEvent : IEvent { }
    public struct StartGameRequestEvent : IEvent { }
    #endregion

    #region Board
    public struct BoardGeneratedEvent : IEvent { }
    public struct CellPointerDownEvent : IEvent { }
    #endregion

    #region UI
    public struct ButtonHoverEvent : IEvent { }
    #endregion
}
