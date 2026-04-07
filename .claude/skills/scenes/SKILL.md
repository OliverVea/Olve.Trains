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

GameLogicScene (root, LayerOrder 0)
  └── GameRenderingScene (child, LayerOrder 1)
      └── GameUIScene (child, LayerOrder 2)
```

Defined in `src/Olve.Trains/GameServiceRegistration.cs`:

```csharp
new SceneDefinition(SceneIds.GameLogicScene, "GameScene", LayerOrder: 0),
new SceneDefinition(SceneIds.GameRenderingScene, "RenderingScene", LayerOrder: 1,
    ParentId: SceneIds.GameLogicScene),
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

```csharp
// Receives typed parameters before Load() runs on any scene service
services.AddSceneParameterService<GameSceneParameterService>(sceneId);
```

Registers the type as scoped and keys it as `ISceneParameterService` for the scene. During scene loading, `LoadParameters(args)` is called on all parameter services **before** any `ISceneService.Load()` runs.

```csharp
public interface ISceneParameterService<in T> : ISceneParameterService
{
    Result LoadParameters(T parameters);
}
```

Pass parameters when loading a scene:

```csharp
sceneManager.LoadAndActivateScene(SceneIds.GameUIScene, SceneIds.GameLogicScene, new GameSceneArguments());
```

The second argument is the target scene ID where the parameter service is registered. The parameter service distributes values to other services (e.g., `MoneyService.Balance`), keeping those services decoupled from the parameter system.

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
    public static readonly Id<IScene> GameLogicScene = Id.New<IScene>();
    public static readonly Id<IScene> GameUIScene = Id.New<IScene>();
    public static readonly Id<IScene> GameRenderingScene = Id.New<IScene>();
}
```

## Where services are registered

| Scene | Registration file | Purpose |
|---|---|---|
| MainMenu | `Scenes/MainMenu/MainMenuSceneServiceRegistration.cs` | Menu UI, rendering core |
| GameLogic | `Scenes/GameLogic/GameLogicSceneServiceRegistration.cs` | Game state, commands, entity services, event wiring |
| GameRendering | `Scenes/GameRendering/GameRenderingSceneServiceRegistration.cs` | 3D/2D rendering, shadow maps, entity renderers |
| GameUI | `Scenes/GameUI/UISceneServiceRegistration.cs` | Tools, panels, GUI, screenshots |

## SceneManager (`src/Olve.Engine3D/Scenes/SceneManager.cs`)

Manages scene loading and the main loop:

```csharp
sceneManager.LoadAndActivateScene(SceneIds.GameLogicScene);  // loads full hierarchy
sceneManager.DeactivateAndUnloadScene(SceneIds.GameLogicScene);  // cascades to children
```

The main game loop calls `sceneManager.Input()`, `sceneManager.Update()`, `sceneManager.Render()` each frame, which delegates to all active scenes in layer order.
