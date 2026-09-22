---
name: scenes
description: Reference for the scene system — ISceneService lifecycle, scene hierarchy, service priority, registration patterns, and scene composition. Use when adding scene services, understanding the update loop, or working with scene loading.
user-invocable: false
---

# Scene System

The scene system manages the game's lifecycle through hierarchical scenes containing prioritized services. Each service participates in a Load/Input/Update/Render/Unload lifecycle.

## ISceneService (`src/Olve.Engine3D/Scenes/ISceneService.cs`)

```csharp
public interface ISceneService
{
    int Priority => 0;
    Result Load() => Result.Success();
    Result Unload() => Result.Success();
    Result<Pass> Input() => Result<Pass>.Success(Pass.Pass);
    Result Update() => Result.Success();
    Result Render() => Result.Success();
}
```

All methods have default implementations. Override only what you need. `Input()` can return `Pass.Block` to stop propagation to lower-priority services.

Returns `Olve.Results.Result` (see `/olve-results` skill).

## Service Priority

Services run in ascending priority order. Use `SceneServicePriority` to declare ordering:

```csharp
// Run after these dependencies (max dependency priority + 1024)
public int Priority => SceneServicePriority.FromDependencies([meshRenderingService, shadowMapService]);

// Run before these dependents (min dependent priority - 1024)
public int Priority => SceneServicePriority.FromDependents([someService]);
```

Default priority is 0. Step between levels is 1024.

## Scene Hierarchy

```
MainMenuScene (root, LayerOrder 0)

LoadingScene (root, LayerOrder 0)

GameLogicScene (root, LayerOrder 0)
  └── GameRenderingScene (child, LayerOrder 1)
      └── GameUIScene (child, LayerOrder 2)
```

**Scene flow:** MainMenu → LoadingScene → GameLogicScene (via async task). LoadingScene shows a loading indicator, runs a background task, and auto-transitions to the game when complete. On the background thread the task: (1) pre-warms the CPU asset caches via `AssetPrewarmService` (loads every `Meshes.All` / `Textures.All` entry into the singleton mesh/texture managers); (2) builds the `GameSceneArguments`; (3) calls `SceneManager.PrepareScene(GameLogicScene, args)` — DI scope creation, the parameter service, and every GameLogicScene service `Load()` (all CPU-only, no GPU/GL). When the task completes, the main thread calls `SceneManager.CommitPreparedScene(...)` to register the loaded GameLogicScene, then `LoadAndActivateScene(GameUIScene)` to load the GPU scenes (GameRenderingScene/GameUIScene reuse the GameLogicScene scope and do their shader/framebuffer/buffer work on the main thread where the GL context lives). Return-to-main-menu goes directly Game → MainMenu (no loading screen).

Defined in `src/Olve.Trains/GameServiceRegistration.cs`:

```csharp
new SceneDefinition(SceneIds.GameLogicScene.Id, "GameScene", LayerOrder: 0),
new SceneDefinition(SceneIds.GameRenderingScene, "RenderingScene", LayerOrder: 1,
    ParentId: SceneIds.GameLogicScene.Id),
new SceneDefinition(SceneIds.GameUIScene, "UIScene", LayerOrder: 2,
    ParentId: SceneIds.GameRenderingScene),
```

**Key behaviors:**
- Child scenes share their parent's DI scope (GameRenderingScene and GameUIScene share GameLogicScene's scope)
- Loading a child automatically loads parents first
- Unloading a parent cascades to children (depth-first)
- Scenes are updated/rendered in `LayerOrder` order

## Scene Lifecycle

**Loading:** Resolve keyed services from DI → sort by Priority → call `Load()` on each → state becomes `Inactive`

**Each frame (active scenes only):** `Input()` → `Update()` → `Render()`, all in priority order

**Problems returned during a frame:** a service's non-critical problem is logged and the frame continues — the remaining services still run, and a faulting `Input()` counts as `Pass.Pass`. The fault is logged once when the service starts failing (`FaultLogger`, `Olve.Engine3D/Diagnostics/`); repeats are suppressed until it succeeds again, which logs the number of failed frames. Only problems with `Severity >= ProblemSeverities.Critical` (checked with `problems.AnyCritical()`) propagate to `GameManager`, which logs them at critical level and stops the game. Exceptions are not caught. `Load`/`Unload` call every service and return all of their problems to the caller.

**Unloading:** Call `Unload()` in reverse priority order → dispose scope if root → state becomes `Unloaded`

### SceneState

```csharp
public enum SceneState { Active, Inactive, Unloaded }
```

Scenes are ordered by `LayerOrder` (int) — lower numbers run first.

## Registration

### Adding a scene service

```csharp
// Service participates in scene lifecycle (Load/Input/Update/Render/Unload)
services.AddSceneService<TrainMovementService>(sceneId);
```

This does two things: registers the type as scoped (`TryAddScoped<T>()`) and keys it to the scene ID so the scene resolves it.

### Adding a non-scene dependency

```csharp
// Scoped singleton — injected as a dependency but not in the scene lifecycle
services.TryAddScoped<TrackService>();
```

Use this for services that hold state or provide logic but don't need `Load`/`Update`/`Render` callbacks. They live in the scene's DI scope and are shared across all services in that scope.

### Adding a scene parameter service

A scene that takes parameters is identified by a `SceneKey<TParameters>` instead of a plain `Id<IScene>` (see Scene IDs below). The key ties the scene to its parameter type, so parameters are checked at compile time.

```csharp
// Receives typed parameters before Load() runs on any scene service
services.AddSceneParameterService<GameSceneParameterService, GameSceneArguments>(SceneIds.GameLogicScene);
```

Registers the service as scoped and keys it as `ISceneParameterService<GameSceneArguments>` for the scene. The service must implement `ISceneParameterService<TParameters>` for the key's parameter type. During scene loading, `LoadParameters(args)` is called on all parameter services **before** any `ISceneService.Load()` runs.

```csharp
public interface ISceneParameterService<in T>
{
    Result LoadParameters(T parameters);
}
```

Pass parameters when loading a scene as `SceneArguments`, built with `key.With(parameters)`:

```csharp
sceneManager.LoadAndActivateScene(SceneIds.LoadingScene.Id, SceneIds.LoadingScene.With(new LoadingSceneArguments()));
sceneManager.LoadAndActivateScene(SceneIds.GameUIScene, SceneIds.GameLogicScene.With(new GameSceneArguments()));  // arguments for a parent scene
```

Each `SceneArguments` carries its target scene ID, so arguments can target any scene in the loaded hierarchy; they are applied when that scene loads. `LoadScene`, `LoadAndActivateScene` and `PrepareScene` all take `params SceneArguments[]`. The parameter service distributes values to other services (e.g., `MoneyService.Balance`), keeping those services decoupled from the parameter system.

### When to use which

| Need | Registration |
|---|---|
| Service needs `Update()` each frame | `AddSceneService` |
| Service needs `Load()`/`Unload()` for setup/teardown | `AddSceneService` |
| Service needs `Render()` | `AddSceneService` |
| Service needs typed initialization parameters | `AddSceneParameterService` |
| Service is pure state/logic, no lifecycle | `TryAddScoped` |
| Service reacts to events only | `TryAddScoped` + event registration (see `/events` skill) |

### Event scene services

See the `/events` skill for `AddEventSceneService` and `AddImmediateEventSceneService`.

## Scene IDs (`src/Olve.Trains/SceneIds.cs`)

```csharp
public static class SceneIds
{
    public static readonly Id<IScene> MainMenuScene = Id.New<IScene>();
    public static readonly SceneKey<GameSceneArguments> GameLogicScene = new(Id.New<IScene>());
    public static readonly Id<IScene> GameUIScene = Id.New<IScene>();
    public static readonly Id<IScene> GameRenderingScene = Id.New<IScene>();
    public static readonly SceneKey<LoadingSceneArguments> LoadingScene = new(Id.New<IScene>());
}
```

`SceneKey<T>` has no implicit conversion; use `.Id` where a plain `Id<IScene>` is needed (definitions, unloading, lookups).

## Where services are registered

| Scene | Registration file | Purpose |
|---|---|---|
| MainMenu | `Scenes/MainMenu/MainMenuSceneServiceRegistration.cs` | Menu UI, rendering core |
| Loading | `Scenes/Loading/LoadingSceneServiceRegistration.cs` | Loading indicator, async task infrastructure, auto-transition to game |
| GameLogic | `Scenes/GameLogic/GameLogicSceneServiceRegistration.cs` | Game state, commands, entity services, event wiring |
| GameRendering | `Scenes/GameRendering/GameRenderingSceneServiceRegistration.cs` | 3D/2D rendering, shadow maps, entity renderers |
| GameUI | `Scenes/GameUI/UISceneServiceRegistration.cs` | Tools, panels, GUI, screenshots |

## SceneManager (`src/Olve.Engine3D/Scenes/SceneManager.cs`)

Manages scene loading and the main loop:

```csharp
sceneManager.LoadAndActivateScene(SceneIds.GameUIScene);  // loads full hierarchy
sceneManager.DeactivateAndUnloadScene(SceneIds.GameLogicScene.Id);  // cascades to children
```

**Off-thread loading (root scenes only):** `PrepareScene(sceneId, arguments)` runs scope creation + parameter service + service `Load()` for a root scene without touching any shared `SceneManager` state — safe to call on a background thread (used by `LoadingService`). It returns an opaque `PreparedScene`. On the main thread, `CommitPreparedScene(prepared)` registers it as loaded (inactive); follow with `LoadAndActivateScene(...)` to load child scenes and activate. Only root scenes (no parent) can be prepared this way, since children reuse the parent's not-yet-committed scope.

The main game loop calls `sceneManager.Input()`, `sceneManager.Update()`, `sceneManager.Render()` each frame, which delegates to all active scenes in layer order.
