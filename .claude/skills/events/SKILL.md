---
name: events
description: Reference for the event system — Event<T>, EventQueue, immediate vs deferred event scene services, registration patterns, ordering, and prefill. Use when working with events, wiring up new event handlers, or understanding event flow.
user-invocable: false
---

# Event System

This skill is a reference for the event system. Read this when you need to wire up event handlers, understand event flow, or debug event ordering issues.

Event handlers return `Olve.Results.Result` — see the `/olve-results` skill for the full API. Key points: `Result.Success()` for success, `ResultProblem` for failure, `.ToEmptyResult()` to discard a `Result<T>`'s value.

## Core Types

### Event<T> (`Olve.Utilities.Stores`, from the Olve.Utilities package)

Synchronous multicast event: all handlers run inline, in subscription order, when `Invoke` is called. `Olve.Utilities.Stores` is a global using in the engine and game projects.

```csharp
public class Event<T>
{
    public void Invoke(T message);   // never throws
    public void Subscribe(Action<T> handler);
    public void Unsubscribe(Action<T> handler);
}
```

There is also a non-generic `Event` (no parameter).

Handlers are isolated: a handler that throws does not stop the others, and the exception does not reach the code that called `Invoke`. It goes to `EventDispatch.OnHandlerException`, which `Program.cs` routes to an error-level log (category `EventDispatch`), so integration tests' no-errors check still catches it.

### EventQueue<T> (`src/Olve.Engine3D/Systems/EventQueue.cs`)

Subscribes to an `Event<T>` and enqueues items into a `ConcurrentQueue`. On `Update()`, drains the queue and calls the handler for each item. Used by `EventSceneService` for deferred processing.

- `Init(prefill?)` — enqueues prefill items, then subscribes to the event
- `Update()` — drains and processes the queue; stops on first handler failure
- `Cleanup()` — unsubscribes from the event

## Event Scene Services

Two wrappers that integrate events with the scene lifecycle (see `/scenes` skill). Both implement `ISceneService`.

### ImmediateEventSceneService<T> (`src/Olve.Engine3D/Systems/ImmediateEventSceneService.cs`)

Handler runs **synchronously when the event fires** — inside the `Invoke()` call chain. Use for state that must be consistent immediately (collision registration, entity store indexes).

- `Load()`: processes prefill items, then subscribes handler to the event
- `Update()`: no-op (events already handled inline)
- `Unload()`: unsubscribes

### EventSceneService<T> (`src/Olve.Engine3D/Systems/EventSceneService.cs`)

Handler runs **deferred during the scene's Update() phase**. Events are enqueued and processed in batch. Use for derived state that can wait until the next frame (rendering, UI updates).

- `Load()`: initializes the queue with prefill, subscribes to the event
- `Update()`: drains queue and calls handler for each item
- `Unload()`: unsubscribes

### When to use which

| Scenario | Use |
|---|---|
| New entity needs a collider immediately so subsequent code can query it | Immediate |
| New entity needs a rendering instance | Deferred |
| Derived state that other immediate handlers depend on | Immediate |
| Side effects with no same-frame dependents | Deferred |

## Registration

Extension methods in `SceneServiceRegistration.cs` (see `/scenes` skill for the broader scene service system). All have overloads for 0, 1, or 2 handler dependencies.

### AddImmediateEventSceneService

```csharp
services.AddImmediateEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingAdded,              // event source
    (BuildingCollisionService bcs, EntityAdded<Building, Id<Building>> added) => bcs.Register(added.Id),  // handler
    prefill: bs => bs.Buildings.AsAdded());                  // optional prefill
```

### AddEventSceneService

```csharp
services.AddEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingRemoved,            // event source
    (StationService ss, EntityDeleted<Building, Id<Building>> deleted) => ss.DeleteStationForBuilding(deleted.Id));  // handler
```

### Parameters

- **`eventSelector`**: Extracts the `Event<T>` from the source service (e.g., `bs => bs.OnBuildingAdded`)
- **`handler`**: Callback receiving the event payload. Must return `Olve.Results.Result` (see `/olve-results` skill).
- **`prefill`**: Optional. Returns existing items to process on scene load, as event payloads. Use this to initialize dependent state for entities that already exist (e.g., register colliders for all buildings that were loaded before this service started). For store events, `entities.AsAdded()` (`Olve.Engine3D.Systems.EntityEvents`) turns entities into `EntityAdded` payloads.
- **`before`**: Array of `ISceneServiceType`. This event service runs before the named services. Sets priority to `min(before_priorities) - 1024`.
- **`after`**: Array of `ISceneServiceType`. This event service runs after the named services. Sets priority to `max(after_priorities) + 1024`.
- **`propagateFailedUpdate`**: (Deferred only) If true, a failed handler propagates the error up the scene update. Default false.

### Ordering with before/after

```csharp
// Ensure this runs after TrainMovementService but before TrainJunctionCrossingService
services.AddEventSceneService(sceneId,
    (TrainMovementService vms) => vms.OnTrainReachedTrackEnd,
    (TrainJunctionCrossingService vjcs, Id<Train> id) => vjcs.OnTrainReachedEnd(id),
    after: [new SceneServiceType<TrainMovementService>()],
    before: [new SceneServiceType<TrainJunctionCrossingService>()]);
```

Priority resolution:
- `after` only: `max(after_priorities) + 1024`
- `before` only: `min(before_priorities) - 1024`
- Both: midpoint `(max_after + min_before) / 2` (logs a warning if constraints conflict)
- Neither: priority 0

## Common Patterns

### Paired add/remove

Always register both sides to keep state symmetric:

```csharp
// Add
services.AddImmediateEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingAdded,
    (BuildingCollisionService bcs, EntityAdded<Building, Id<Building>> added) => bcs.Register(added.Id),
    prefill: bs => bs.Buildings.AsAdded());

// Remove
services.AddEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingRemoved,
    (BuildingCollisionService bcs, EntityDeleted<Building, Id<Building>> deleted) => bcs.Unregister(deleted.Id));
```

Note: add handlers typically use Immediate + prefill. Remove handlers typically use deferred (no prefill needed).

### Multiple handlers on same event

Multiple services can subscribe to the same event. Each gets its own registration:

```csharp
// All fire when a building is added:
services.AddImmediateEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingAdded,
    (StationService ss, EntityAdded<Building, Id<Building>> added) => ss.CreateStationForBuilding(added.Id).ToEmptyResult(),
    prefill: bs => bs.Buildings.AsAdded());

services.AddImmediateEventSceneService(sceneId,
    (BuildingService bs) => bs.OnBuildingAdded,
    (DepotService ds, EntityAdded<Building, Id<Building>> added) => ds.CreateDepotForBuilding(added.Id).ToEmptyResult(),
    prefill: bs => bs.Buildings.AsAdded());
```

### Cascade deletion via blueprint events

```csharp
// When a blueprint is removed, delete all entities using that blueprint
services.AddEventSceneService(sceneId,
    (BuildingBlueprintService bbs) => bbs.OnBlueprintRemoved,
    (BuildingService bs, EntityDeleted<BuildingBlueprint, Id<BuildingBlueprint>> deleted) => bs.DeleteBuildingsWithBlueprint(deleted.Id));
```

### Event sources

Events originate from `EntityStore<T>` (Olve.Utilities package) and carry the committed entity:
- `OnAdded` — `EntityAdded<T, Id<T>>(Id, Entity)`, fires after `TryAdd()` (or `Set()` of a new id) succeeds
- `OnUpdated` — `EntityUpdated<T, Id<T>>(Id, Before, After)`, fires after `Set()` replaces a different value
- `OnDeleted` — `EntityDeleted<T, Id<T>>(Id, Entity)`, fires after `Delete()` succeeds

Services expose these as public properties with domain names (e.g., `BuildingService.OnBuildingAdded`, `BuildingService.OnBuildingRemoved` for `OnDeleted`).

Removal events fire **after** the entity has left the store, so a handler can't look the entity up by id any more. Read what you need from the payload's `Entity` instead (e.g., `JunctionService.OnTrackRemoved` takes the removed track's endpoints from it). Entities are immutable records: never mutate a payload.

## Where events are registered

- **Game logic**: `src/Olve.Trains/Scenes/GameLogic/GameLogicSceneServiceRegistration.cs`
- **Rendering**: `src/Olve.Trains/Scenes/GameRendering/GameRenderingSceneServiceRegistration.cs`
