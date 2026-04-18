[🇷🇺 Читать на русском](README-RU.md)

# Product Specifications
**Platform**: Web (Yandex Games).

**Genre**: Logic, Puzzle.

# Tech Stack
[FMOD](https://www.fmod.com) - for resolving web audio issues via library imports on the bootstrap scene.

[Extenject](https://github.com/Mathijs-Bakker/Extenject) (prev Vcontainer) - DI. The primary reason for the switch was the implementation of Zenject signals.

[UniRX](https://github.com/neuecc/unirx) (prev R3) - Event System for event arhitecture. RX simpler & have no external dependencies from .NET.

[uGUI](https://docs.unity3d.com/Packages/com.unity.ugui@2.5/manual/index.html) - old ui.

[Ui Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIElements.html) - main menu implementation.

[DoTween](https://dotween.demigiant.com) - button and word animations.

[Odin](https://odininspector.com) - Expand basic editor functionality.


# Architecture
1. Bootstrap scene is loaded.
2. DI is connected.
3. Event bus is enabled.
4. Systems are further divided into modules under the AsmDef architecture.
5. The entire game is controlled via an event system.

