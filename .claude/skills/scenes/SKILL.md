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

**Scene flow:** MainMenu → LoadingScene → GameLogicScene. LoadingScene shows a loading indicator while a background task pre-warms the CPU asset caches via `AssetPrewarmService` (loads every `Meshes.All` / `Textures.All` entry into the singleton mesh/texture managers) and builds the `GameSceneArguments` (reading the save file when loading a game). When the task completes, the main thread unloads LoadingScene and calls `LoadAndActivateScene(SceneIds.GameUIScene, SceneIds.GameLogicScene.With(arguments))`, which loads and activates GameLogicScene → GameRenderingScene → GameUIScene. All scene loading happens on the main thread, where the GL context lives. Return-to-main-menu goes directly Game → MainMenu (no loading screen).

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
- Loading or activating a scene first loads/activates its ancestors
- Unloading or deactivating a scene first unloads/deactivates its descendants
- Unloading is best effort: the whole tree is deactivated, then unloaded, and every problem is collected rather than stopping at the first
- `LoadAndActivateScene` rolls back on failure: if any scene fails to load or activate, the part of the lineage it loaded is unloaded again
- Scenes are updated/rendered in `LayerOrder` order

## Scene Lifecycle

**Loading:** Create the scope (root) or borrow the parent's → set scene parameters → resolve keyed services from DI → sort by Priority → call `Load()` on each → state becomes `Inactive`

**Each frame (active scenes only):** `Input()` → `Update()` → `Render()`, all in priority order

**Problems returned during a frame:** a service's non-critical problem is logged and the frame continues — the remaining services still run, and a faulting `Input()` counts as `Pass.Pass`. The fault is logged once when the service starts failing (`FaultLogger`, `Olve.Engine3D/Diagnostics/`); repeats are suppressed until it succeeds again, which logs the number of failed frames. Only problems with `Severity >= ProblemSeverities.Critical` (checked with `problems.AnyCritical()`) propagate to `GameManager`, which logs them at critical level and stops the game. Exceptions are not caught. `Load`/`Unload` call every service and return all of their problems to the caller.

**Unloading:** Deactivate the tree → unload children → call `Unload()` on each service → state becomes `Unloaded` → remove from the store → dispose scope if root

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

### Scene parameters

A scene that takes parameters is identified by a `SceneKey<TParameters>` instead of a plain `Id<IScene>` (see Scene IDs below). The key ties the scene to its parameter type, so parameters are checked at compile time.

```csharp
// Register the scene's parameters with the defaults used when it loads without arguments
services.AddSceneParameters(SceneIds.GameLogicScene, () => new GameSceneArguments());
```

This registers a scoped `SceneParameters<GameSceneArguments>` holder and a scoped `GameSceneArguments` resolved from it. Services take the arguments through their constructor:

```csharp
public class TerrainService(..., GameSceneArguments arguments) : ISceneService
```

Pass parameters when loading a scene as `SceneArguments`, built with `key.With(parameters)`:

```csharp
sceneManager.LoadAndActivateScene(SceneIds.LoadingScene.Id, SceneIds.LoadingScene.With(new LoadingSceneArguments()));
sceneManager.LoadAndActivateScene(SceneIds.GameUIScene, SceneIds.GameLogicScene.With(new GameSceneArguments()));  // arguments for a parent scene
```

Each `SceneArguments` carries its target scene ID, so arguments can target any scene in the loaded lineage. When a scene loads, `SceneManager` creates (or borrows) its scope, sets the arguments targeting that scene, fills in the registered defaults for any parameters not provided, and only then resolves the scene's services — so constructors receive the final values. Arguments of a type the scene didn't register fail the load.

Rules:
- A scene's parameters can be injected into services resolved while that scene or its descendants load. Reading them earlier (e.g. resolving from the scope before the scene loads) throws `InvalidOperationException`.
- Defaults live in the arguments record (e.g. `GameSceneArguments.DefaultHeightmap()`, `DefaultDayStart`, `DefaultDayDuration`), not in the services.

### When to use which

| Need | Registration |
|---|---|
| Service needs `Update()` each frame | `AddSceneService` |
| Service needs `Load()`/`Unload()` for setup/teardown | `AddSceneService` |
| Service needs `Render()` | `AddSceneService` |
| Service needs typed initialization parameters | `AddSceneParameters` on the scene, then inject the arguments into the constructor |
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
sceneManager.LoadAndActivateScene(SceneIds.GameUIScene);  // loads and activates the full lineage
sceneManager.UnloadScene(SceneIds.GameLogicScene.Id);      // deactivates, then unloads, the whole tree
sceneManager.Close();                                     // unloads every root scene
```

The public API is `LoadAndActivateScene`, `UnloadScene`, `Close` and the frame methods; loading, activation and deactivation of individual scenes are private. Each loaded scene is one `LoadedScene` (scene, service scope, parent ID) in an `EntityStore` keyed by scene ID; children are found by `ParentId`, and a scene owns its scope exactly when it has no parent. The frame loop iterates an `OrderedEntityStoreValueCache` (`Olve.Engine3D/Stores/`) sorted by `LayerOrder`, then `Layer`, which rebuilds when scenes are added or removed. `SceneScopeAccessor.ActiveScopeProviders` (used by log display-name resolution) reads the active scopes from `SceneManager`.

The main game loop calls `sceneManager.Input()`, `sceneManager.Update()`, `sceneManager.Render()` each frame, which delegates to all active scenes in layer order.
