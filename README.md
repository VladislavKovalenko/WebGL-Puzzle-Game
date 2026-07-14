[🇷🇺 Читать на русском](README-RU.md)

# Vasily Ivanovich's Fillwords — Unity Web Word Puzzle with Light Horror Elements

<img width="1607" height="950" alt="image" src="https://github.com/user-attachments/assets/17c7870e-b32e-4943-ba78-8f0e56791105" />

**Platform:** Web (Yandex Games) <!-- — [▶ Play](PLACEHOLDER_LINK) -->  
**Genre:** Word Puzzle / Light Horror  
**Engine:** Unity  
**Status:** Pending moderation on Yandex Games

---

A browser word search puzzle built for Yandex Games.

The player swipes adjacent letters to find hidden words on a procedurally generated grid. Each wrong guess drains a shared life pool. Lose all lives and the entire campaign resets. Clear a level without mistakes and earn a life back.

The game wraps this mechanic in a light horror framing: an old man watches you play, and his mood tracks your remaining lives — calm when you're doing well, furious when you're not. Clearing levels unlocks new ones from a 30-level campaign with difficulty templates and an optional flashlight hazard that limits screen visibility.

---
## Features
- Procedurally generated solvable word boards
- 30-level campaign
- 400-word CSV dictionary
- FMOD audio
- Zenject + UniRx architecture
- WebGL build for Yandex Games with bootstrap flow

---

## Why this repo is worth looking at

The gameplay is built around a simple word-search mechanic, while most of the engineering effort went into three systems:

1. **How the game reliably boots audio on Web**, where browsers block sound until user interaction.
2. **How every board is generated procedurally** and guaranteed to be fully solvable without hand-authoring.
3. **How session state and persistent progression are kept separate**, so a failed run can't corrupt saved data.

Those three systems are described below.

---

## 1. Bootstrap Flow

**Files:** `Bootstraper.cs`, `LoadManager.cs`, `LoadingScreenManager.cs`, `FMODBankLoader.cs`, `FMODFocusHandler.cs`

Browsers refuse to play audio until the user interacts with the page. A naive WebGL build that autoplays sound will produce silence or console errors. This project handles it with a dedicated **Bootstrap scene**:

- `IAsyncInitService` is the contract every startup service implements.
- `Bootstraper` collects all registered `IAsyncInitService` instances via Zenject and awaits them in parallel with `UniTask.WhenAll`, wrapping each initialization task in a try/catch block so one failing service doesn't hang the whole boot sequence.
- `FMODBankLoader` loads FMOD banks and explicitly calls `mixerSuspend()` / `mixerResume()` — a workaround FMOD recommends specifically for Chrome and Safari autoplay policies.
- Only after all services report ready does `LoadManager` wait for **any input event** (`InputSystem.onAnyButtonPress`) before calling `YG2.GameReadyAPI()` and loading the Main Menu scene.
- `FMODFocusHandler` mutes the master bus on `OnApplicationFocus` / `OnApplicationPause`, so backgrounding the browser tab silences audio immediately.

The result is a controlled, ordered startup that handles the specific constraints of Web audio without relying on Unity's default audio system.

---

## 2. Procedural Board Generation

**Files:** `BoardGenerator.cs`, `WordService.cs`, `BoardData.cs`

Every level's letter grid is generated at runtime. The approach:

**Step 1 — Partition the grid into word-length slots.**  
`GetRandomLengthPartition` splits the total cell count (`Columns × Rows`) into a random sequence of word lengths within the level's min/max range. Any partition that requires a word length not present in the dictionary is rejected before generation begins.

**Step 2 — Carve paths for each slot.**  
`TryPartitionGrid` and `TryBuildPath` recursively carve non-overlapping snake-shaped paths into the grid — one per word length — using randomized neighbor order and backtracking. Paths are validated structurally before any word is assigned to them.

**Step 3 — Assign real words.**  
`WordService` holds a CSV-parsed dictionary grouped by word length (`Dictionary<int, List<string>>`). Once a valid full partition is found, each path gets a randomly drawn word of the correct length.

If any step fails — dead-end backtracking, or no word available for a required length — the whole attempt is discarded and retried, up to 50 attempts per level load.

As a result, every generated board is structurally solvable while remaining unique.

---

## 3. Session State vs. Meta Progression

**Files:** `GameSessionModel.cs`, `LevelsModel.cs`, `GameplayModel.cs`, `GlobalGameData.cs`

State is split across three models with distinct lifetimes, and they don't overlap:

**`GameplayModel` — scene scope.**  
Tracks only what's happening in the current level: game state (`Intro → Playing → Win / Lose`) and words found vs. total. Knows nothing about the campaign or saved data. Rebuilt fresh on every Gameplay scene load.

**`GameSessionModel` — run scope.**  
A Zenject singleton that lives for the app session. Tracks `GlobalLives` (shared across the entire run), the current level index, and whether the player took damage this level (used to decide whether a perfect-clear bonus life is granted). Reads campaign structure from `CampaignRouteSO` to build level configs on demand.

**`LevelsModel` — persistent scope.**  
The only layer that writes to `YG2.saves`. Tracks unlocked levels and completed levels. Written to only on explicit win/loss events in `LevelEndPresenter`, not continuously.

The result: a failed run can drain lives and trigger a campaign reset without touching persistent save data until the run is definitively over. Scene-local logic stays local. Cross-run progression stays in the save layer.

---

## Reactive Presentation Layer

Gameplay and menus are structured as Model → View → Presenter, wired through Zenject:

`BoardPresenter`, `GrandpaPresenter`, `LevelEndPresenter`, `IntroSlidePresenter`, `SettingsPresenter`, `HazardPresenter`

State changes (`GameState`, `GrandpaState`, `GlobalLives`) are `ReactiveProperty<T>` values consumed via UniRx `.Subscribe()` in presenters. Presenters own the interaction logic while views remain passive. Views don't poll state — they react to it.

**On the event architecture specifically:** this project uses a deliberate mix, not a single unified bus:
- **Zenject `SignalBus`** for cross-scene events: `AllServicesAreLoadedSignal`, `BackToMainMenuSignal`, `SettingsMenuOpenSignal`.
- **UniRx `ReactiveProperty`** for gameplay state propagation: lives, game state, words found.
- **Direct injected references** for tightly-coupled presenter/service pairs where a signal layer would add indirection without benefit.

---

## Tech Stack

| Area | Technology |
|---|---|
| Engine | Unity, C# |
| DI & Signals | [Zenject / Extenject](https://github.com/Mathijs-Bakker/Extenject) |
| Reactive State | [UniRx](https://github.com/neuecc/unirx) |
| Async Init | [UniTask](https://github.com/Cysharp/UniTask) |
| Audio | [FMOD](https://www.fmod.com) |
| UI | uGUI, [DOTween](https://dotween.demigiant.com) |
| Platform | Yandex Games SDK (YG2) |

---

## Project Structure

```
Scripts/
├── Audio/          FMOD wrappers, sound library SO, UI and gameplay sound hooks
├── DI/             Zenject installers — project scope and per-scene
├── Events/         Signal type definitions
├── GameData/       Campaign config, session model, save-linked progression
├── GameFlow/
│   ├── Bootstrap/  Async service init, loading screen, FMOD bank loading
│   ├── Level/      Board generation, gameplay loop, hazards, HUD, narrative
│   └── Main Menu/  Level select, settings, menu navigation
├── UI/             Cursor, minor UI utilities
└── Utility/        Editor-only debugging and layout visualization tools
```

---

## How to Run

- Unity version: `6000.5.0f1`
- Open the `Bootstrap` scene and press Play — this is the entry point.

<!-- PLACEHOLDER: note any FMOD bank setup steps required for a clean clone -->

---

## Contact

Vladislav, https://t.me/PureGenious
