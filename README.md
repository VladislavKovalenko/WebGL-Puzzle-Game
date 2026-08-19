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
- WebGL build for Yandex Games with a dedicated bootstrap flow
- Browser-aware audio initialization and focus handling

---

## Why this repo is worth looking at

The gameplay is built around a simple word-search mechanic, while most of the engineering effort went into three systems:

1. **How the game reliably boots audio on Web**, where browsers block sound until user interaction.
2. **How every board is generated procedurally** and guaranteed to be fully solvable without hand-authoring.
3. **How session state and persistent progression are kept separate**, so a failed run cannot corrupt saved data.

Those three systems are described below.

---

## 1. Bootstrap Flow

**Files:** `Bootstraper.cs`, `LoadManager.cs`, `LoadingScreenManager.cs`, `FMODBankLoader.cs`, `FMODFocusHandler.cs`

Browsers refuse to play audio until the user interacts with the page. A naive WebGL build that tries to autoplay sound may produce silence or console errors. This project handles the process with a dedicated **Bootstrap scene**:

- `IAsyncInitService` is the contract implemented by every startup service.
- `Bootstraper` collects all registered `IAsyncInitService` instances through Zenject and awaits them in parallel with `UniTask.WhenAll`.
- Each initialization task is wrapped in a `try/catch` block, so one failing service does not hang the entire boot sequence.
- `FMODBankLoader` loads FMOD banks through `RuntimeManager.LoadBank` and waits until they are ready.
- Once all services finish initializing, `Bootstraper` directly calls `LoadManager.OnServicesReady()`. The startup flow does not use an `AllServicesAreLoadedSignal`.
- `LoadManager` displays a “Press any button” prompt and calls `YG2.GameReadyAPI()`. This tells Yandex Games that the game is ready and allows the platform's loading overlay to disappear.
- After that, `LoadManager` waits for the first user input through `InputSystem.onAnyButtonPress`.
- When the user interacts with the page, `LoadManager` calls `mixerResume()` to unlock FMOD audio, then continues the startup flow and loads the Main Menu scene.
- `FMODFocusHandler` responds to `OnApplicationFocus` and `OnApplicationPause`. When the browser tab loses focus, it pauses the FMOD master bus with `setPaused(true)` and suspends the FMOD mixer with `mixerSuspend()`. When focus returns, it resumes the bus with `setPaused(false)` and calls `mixerResume()`.

The result is a controlled startup sequence that accounts for browser audio restrictions, Yandex Games loading behavior, and browser-tab focus changes without relying on Unity's default audio system.

---

## 2. Procedural Board Generation

**Files:** `BoardGenerator.cs`, `WordService.cs`, `BoardData.cs`

Every level's letter grid is generated at runtime. The approach is divided into three steps.

### Step 1 — Partition the grid into word-length slots

`GetRandomLengthPartition` splits the total cell count (`Columns × Rows`) into a random sequence of word lengths within the level's minimum and maximum range.

Any partition that requires a word length not present in the dictionary is rejected before generation begins.

### Step 2 — Carve paths for each slot

`TryPartitionGrid` and `TryBuildPath` recursively carve non-overlapping, snake-shaped paths into the grid — one path for each word length.

The algorithm uses randomized neighbor order and backtracking. Paths are validated structurally before any word is assigned to them.

### Step 3 — Assign real words

`WordService` stores a CSV-parsed dictionary grouped by word length:

```csharp
Dictionary<int, List<string>>
```

Once a valid complete partition is found, each path receives a randomly selected word of the corresponding length.

If any step fails — for example, because backtracking reaches a dead end or no word is available for a required length — the entire attempt is discarded and retried, up to 50 attempts per level load.

As a result, every generated board is structurally solvable while remaining unique.

---

## 3. Session State vs. Meta Progression

**Files:** `GameSessionModel.cs`, `LevelsModel.cs`, `GameplayModel.cs`, `GlobalGameData.cs`

State is split across three models with distinct lifetimes, and their responsibilities do not overlap.

### `GameplayModel` — scene scope

Tracks only what is happening in the current level:

- Game state: `Intro → Playing → Win / Lose`
- Number of words found
- Total number of words

It knows nothing about the campaign or saved data and is rebuilt every time the Gameplay scene is loaded.

### `GameSessionModel` — run scope

A Zenject singleton that lives for the duration of the application session. It tracks:

- `GlobalLives`, shared across the entire run
- Current level index
- Whether the player took damage during the current level, which is used to determine whether a perfect-clear bonus life should be granted

It reads the campaign structure from `CampaignRouteSO` and builds level configurations on demand.

### `LevelsModel` — persistent scope

The only layer that writes to `YG2.saves`. It tracks:

- Unlocked levels
- Completed levels

It is updated only on explicit win or loss events in `LevelEndPresenter`, rather than continuously.

The result is that a failed run can drain lives and trigger a campaign reset without modifying persistent progression until the run is definitively over. Scene-local logic remains local, while cross-run progression stays in the save layer.

---

## Reactive Presentation Layer

Gameplay and menus are structured using a Model → View → Presenter architecture and wired through Zenject:

- `BoardPresenter`
- `GrandpaPresenter`
- `LevelEndPresenter`
- `IntroSlidePresenter`
- `SettingsPresenter`
- `HazardPresenter`

State changes such as `GameState`, `GrandpaState`, and `GlobalLives` are represented by `ReactiveProperty<T>` values and consumed through UniRx `.Subscribe()` calls in presenters.

Presenters own the interaction logic, while views remain passive. Views do not poll the state; they react to state changes.

### Event Architecture

The project uses a deliberate combination of communication mechanisms rather than one unified event bus:

- **Zenject `SignalBus`** is used for selected cross-scene events:
  - `BackToMainMenuSignal`
  - `SettingsMenuOpenSignal`
- **`UniRx ReactiveProperty`** is used for gameplay state propagation:
  - Lives
  - Game state
  - Words found
- **Direct injected references** are used for tightly coupled presenter-service pairs where an additional signal layer would only add unnecessary indirection.
- **`Bootstraper` directly calls `LoadManager.OnServicesReady()`** after all asynchronous initialization services complete. This startup transition is not dispatched through a signal.
- `UserGestureSignal` is declared in `ProjectInstaller`, but it is not used in the current implementation.

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

```text
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

- Unity version: `6000.5.4f1`
- Install Extenject: (https://github.com/Mathijs-Bakker/Extenject#installation-)
- Install Unitask: (https://github.com/cysharp/unitask#getting-started)
- If build errors persist, this might be needed: (https://github.com/Mathijs-Bakker/Extenject#unirx-integration)
- Open the `Bootstrap` scene and press Play. This is the entry point.
- The bootstrap flow initializes registered services, loads FMOD banks, displays the ready prompt, and waits for the first user interaction before continuing to the Main Menu.

