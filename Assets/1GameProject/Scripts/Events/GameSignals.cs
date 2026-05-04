using UnityEngine;

namespace _1GameProject.Scripts.Events
{
    //Bootstrap Signals
    public class AllServicesisLoadedSignal { }
    
    
    
    
    
    //Marker-Interface
    public interface IEvent { }
    
    //Main Menu Signals
    public class GameStartSignal { }
    public class RanksMenuOpenSignal { }
    public class StoreOpenSignal { }
    public class BackToMainMenuSignal { }
    
    //Settings Signals
    public class SettingsMenuOpenSignal { }
    
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
