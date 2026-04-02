# Product Specifications
**Platform**: Web (Yandex Games).

**Genre**: Logic, Puzzle.

# Tech Stack
[FMOD](https://www.fmod.com) - for resolving web audio issues via library imports on the bootstrap scene.

[uGUI](https://docs.unity3d.com/Packages/com.unity.ugui@2.5/manual/index.html) - old ui.

[Ui Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIElements.html) - main menu implementation.

[DoTween](https://dotween.demigiant.com) - button and word animations.

[VContainer](https://vcontainer.hadashikick.jp) - DI 

# Architecture
1. Bootstrap scene is loaded.
2. DI is connected.
3. Event bus is enabled.
4. Systems are further divided into modules under the AsmDef architecture.
5. The entire game is controlled via an event system.

