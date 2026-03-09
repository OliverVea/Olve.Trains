# Trains and Wagons

## Problem

Trains are standalone entities with no sub-structure. A train is a single locomotive with no way to attach cargo-carrying wagons.

The cargo system needs train-side inventories for loading/unloading at stations. This requires wagons with cargo capacity attached to trains.

## Design

### Wagon Entity

A wagon is attached to a train. It has no independent position or movement — its position is derived from the train's (locomotive's) position on the track spline.

```csharp
public readonly record struct Wagon(Id<Wagon> Id, Id<WagonBlueprint> BlueprintId);
```

### Wagon Blueprints

Blueprints define wagon properties. For the demo, a single "goods wagon" blueprint suffices.

```csharp
public readonly record struct WagonBlueprint(
    Id<WagonBlueprint> Id,
    string Name,
    Id<Mesh> MeshId,
    int Capacity,
    ImmutableHashSet<Id<CargoType>>? AllowedTypes, // null = accept all
    float Length);                                   // meters, for spacing
```

```csharp
public static class WagonBlueprints
{
    public static readonly WagonBlueprint GoodsWagon = new(
        Id.FromName<WagonBlueprint>("wagon/goods"),
        "Goods Wagon",
        Meshes.SM_Veh_Wagon_01,  // TBD: actual asset name
        Capacity: 10,
        AllowedTypes: null,       // accepts any cargo
        Length: 2.0f);            // TBD: measure from mesh
}
```

### Train–Wagon Relationship

Each train owns an ordered list of wagons. Managed by `TrainWagonService`:

```csharp
public class TrainWagonService
{
    IReadOnlyList<Id<Wagon>> GetWagons(Id<Train> trainId);
    Result AddWagon(Id<Train> trainId, Id<WagonBlueprint> blueprintId);
    DeletionResult RemoveWagon(Id<Train> trainId, int index);
}
```

When a train is deleted, all its wagons are removed too.

### Wagon Inventories

Each wagon gets a `CargoInventory` (reusing the existing system). Created when a wagon is added to a train, removed when the wagon is removed.

```csharp
public class WagonInventoryService
{
    // Called internally by TrainWagonService.AddWagon
    Id<CargoInventory> CreateInventory(Id<Wagon> wagonId, WagonBlueprint blueprint);
    void RemoveInventory(Id<Wagon> wagonId);

    bool TryGetInventory(Id<Wagon> wagonId, out Id<CargoInventory> inventoryId);
    IReadOnlyList<(Id<Wagon>, Id<CargoInventory>)> GetTrainInventories(Id<Train> trainId);
}
```

Transfer policies are set per wagon: output types get `TransferDirection.Out`, input types get `TransferDirection.In`, unfiltered wagons get `TransferDirection.Both` for all cargo types.

### Wagon Positioning

Wagons trail the locomotive along the track spline. Position is computed by walking backwards from the locomotive by cumulative distance:

```
Locomotive at time T on track
Gap = 0.3 (coupling gap between rolling stock)

Wagon[0] center = walk back (locomotiveLength/2 + gap + wagon[0].Length/2) from locomotive
Wagon[1] center = walk back (wagon[0].Length/2 + gap + wagon[1].Length/2) from wagon[0]
...
```

"Walking back" means decrementing the spline time parameter proportional to `distance / trackLength`. The resulting position and tangent are sampled from `TrackSplineService`, and the world matrix is computed the same way as `TrainWorldMatrix`.

#### Cross-Track Spanning

When the locomotive crosses a junction, the trailing wagons may still be on the previous track. This requires knowing which tracks the train recently traversed.

```csharp
public class TrainTrackHistoryService : ISceneService
{
    // Listens to TrainJunctionCrossingService — when a train
    // transfers to a new track, push the old track onto the history.
    void OnTrainTrackChanged(Id<Train> trainId, Id<Track> previousTrack);

    // Walk backwards through tracks starting from the train's current track.
    // Returns (trackId, entryTime) pairs for spline sampling.
    IReadOnlyList<(Id<Track> TrackId, float EntryDirection)> GetTrackHistory(Id<Train> trainId);
}
```

The wagon position algorithm walks back along the current track. If it reaches `t <= 0`, it continues onto the previous track from the history (entering at `t = 1` or `t = 0` depending on traversal direction). The history is trimmed when no wagon needs it anymore.

### Rendering

```csharp
public class WagonRenderingService : ISceneService
{
    // Per WagonBlueprint: register a mesh group in MeshRenderingService
    // Per Wagon instance: register a mesh instance in that group
    // Each frame: compute wagon positions from locomotive + offsets, update instances
}
```

Each `WagonBlueprint` maps to one mesh group. Wagon instances within that group are updated per-frame with computed world matrices.

For the demo, all wagons share the same mesh, so there is one group with N instances (one per wagon across all trains).

### Integration with Existing Systems

**Train placement** — `TrainPlacingToolService` and `PlaceTrainHandlerService` create a train. For the demo, one goods wagon is auto-attached on creation.

**Train deletion** — deleting a train also deletes all its wagons (inventories, colliders, mesh instances).

**Junction crossing** — unchanged. The train crosses junctions. Wagons follow via position derivation.

**Signal rules** — unchanged. Rules evaluate against the train. `TrainGroup` still works since it references `Id<Train>`.

**Collision** — wagon colliders are registered in `CollisionSystem` like train colliders. Updated per-frame from computed positions.

**Commands** — extend `query-train` to include wagon info. Add `list-wagons` command for test assertions.

## Data Flow

```
Train placed (UI tool or command):
    → TrainService.Create() → Id<Train>
    → TrainWagonService.AddWagon(trainId, GoodsWagon) → Id<Wagon>
        → WagonInventoryService.CreateInventory() → Id<CargoInventory>
        → WagonRenderingService registers mesh instance

Each frame:
    → TrainMovementService moves train (unchanged)
    → WagonPositionService computes wagon positions from train + history
    → WagonRenderingService updates mesh instance transforms
    → WagonCollisionService updates collider transforms

Junction crossing:
    → Train transfers to new track (unchanged)
    → TrainTrackHistoryService records previous track
    → Wagon positions now span current + previous track(s)

Train deleted:
    → Wagons removed (inventories, colliders, mesh instances cleaned up)
    → Train removed
```

## Demo Scope

- One `WagonBlueprint`: goods wagon (no filter, capacity 10)
- One wagon auto-attached per train on creation
- Wagon rendering with spline following
- Wagon inventories for use by loading/unloading (next epic step)

## Future

- Multiple wagon types with specialized meshes and cargo filters
- Player adding/removing wagons from trains
- Wagon-specific UI (click wagon to see inventory)
- Variable train lengths affecting speed/acceleration (train physics epic)
- Wagon purchase costs (money epic)

## Implementation Order

1. Add `Wagon`, `WagonBlueprint` data types
2. Add `TrainWagonService` — manage wagon lists per train
3. Add `WagonInventoryService` — create/remove cargo inventories per wagon
4. Add `TrainTrackHistoryService` — track traversal history for cross-track spanning
5. Add `WagonPositionService` — compute wagon positions from train + offsets
6. Add `WagonRenderingService` — mesh instances per wagon, updated per frame
7. Integrate with train placement — auto-attach goods wagon on train creation
8. Add wagon colliders to `CollisionSystem`
9. Extend commands — `query-train` shows wagons, add `list-wagons`