# Collision System

## Problem

Mesh data (`MeshData`) is loaded from assets, marshalled to GPU vertex buffers, and discarded. No CPU-side geometry is retained for spatial queries. Collision checking is ad-hoc and per-entity-type with no unified system. Adding new collidable entity types requires duplicating collision logic each time.

## Design

Two new engine systems:

### MeshManager

Central mesh registry analogous to `TextureManager`. Stores `MeshData` by `Id<Mesh>`, caches local-space AABB per mesh.

```csharp
public class MeshManager
{
    Event<Id<Mesh>> OnAdded;
    Event<Id<Mesh>> OnRemoved;

    Result<Id<Mesh>> Register(MeshData meshData);
    DeletionResult Unregister(Id<Mesh> meshId);
    bool TryGetMeshData(Id<Mesh> id, out MeshData data);
    bool TryGetLocalAABB(Id<Mesh> id, out AABB aabb);
}
```

`MeshLoadingManager` bridges `AssetLoader` to `MeshManager` with path-based caching (same pattern as `TextureLoadingManager`).

Eventually all mesh consumers go through `MeshManager`. Rendering maps `Id<Mesh>` to `GeometryId` internally.

### CollisionSystem

Registers colliders backed by mesh AABBs. Each collider has a group tag (`Id<ColliderGroup>`) identifying what kind of entity it represents. World-space AABBs are cached per collider and recomputed on transform updates (local AABB corners transformed by world matrix are no longer axis-aligned, so a new encompassing AABB is computed).

```csharp
public class CollisionSystem
{
    Result<Id<Collider>> RegisterMeshCollider(
        Id<Mesh> meshId, Id<ColliderGroup> group, Matrix4X4<float> worldMatrix);
    Result UpdateTransform(Id<Collider> colliderId, Matrix4X4<float> worldMatrix);
    DeletionResult Unregister(Id<Collider> colliderId);

    IReadOnlyList<RaycastHit> Raycast(Ray3D<float> ray);
    IReadOnlyList<RaycastHit> Raycast(Ray3D<float> ray, Id<ColliderGroup> group);
}
```

Broad phase: ray-AABB intersection (slab method) against cached world-space AABBs. Linear scan, sorted by distance. Sufficient for current entity counts.

Designed for future collider types (box, capsule, sphere) but only mesh-backed AABB colliders initially.

### Collider Groups

Game-defined constants using `Id.FromName<ColliderGroup>()`:

```csharp
public static class ColliderGroups
{
    public static readonly Id<ColliderGroup> Signal = Id.FromName<ColliderGroup>("Signal");
    public static readonly Id<ColliderGroup> Building = Id.FromName<ColliderGroup>("Building");
    public static readonly Id<ColliderGroup> Vehicle = Id.FromName<ColliderGroup>("Vehicle");
    public static readonly Id<ColliderGroup> Track = Id.FromName<ColliderGroup>("Track");
}
```

## AABB Caching

- **MeshManager** caches local-space AABB per mesh (computed once on registration from mesh positions, immutable).
- **CollisionSystem** caches world-space AABB per collider (recomputed on every `UpdateTransform` call). The local AABB's 8 corners are transformed by the world matrix, then a new axis-aligned box is computed around them (Arvo method). This is conservative (slightly oversized for rotated objects) but correct for broad phase.

## Data Flow

```
AssetLoader.LoadAsset(Meshes.X) → MeshData
    → meshLoadingManager.LoadMesh() → Id<Mesh>
        MeshManager retains MeshData + caches local AABB

Game logic (e.g. signal added):
    → collisionSystem.RegisterMeshCollider(meshId, group, worldMatrix) → Id<Collider>
        CollisionSystem looks up local AABB from MeshManager
        Transforms to world-space AABB, stores per collider

Mouse click:
    → camera.GetRay(mousePosition) → Ray3D
    → collisionSystem.Raycast(ray) → nearest RaycastHit (colliderId + group + distance)
```

## Rendering

Meshes stay at the rendering level. `MeshRenderingService` continues to own GPU state. No rendering changes in initial implementation.

Future step: `MeshRenderingService` accepts `Id<Mesh>` and looks up `MeshData` from `MeshManager` instead of loading directly from `AssetLoader`. This makes `MeshManager` the single source of truth for mesh data.

## Tracks

Tracks use procedural geometry with a custom shader (`Shaders.Track`) and spline control points as instance data — fundamentally different from mesh-based rendering. Track rendering stays as-is.

For collision, tracks can generate a low-poly band mesh from their spline (sample N points along the Hermite curve, extrude left/right by track half-width, triangulate) and register it through `MeshManager` + `CollisionSystem` like everything else.

## Migration Path

1. Add `MeshManager` and `MeshLoadingManager` to engine
2. Add `CollisionSystem` with mesh colliders (AABB broad phase, ray-AABB intersection)
3. Register junction signal meshes and colliders (first consumer)
4. Add raycast query and wire to mouse click (log signal clicks)
5. Migrate building meshes to `MeshManager` + register building colliders
6. Migrate vehicle meshes to `MeshManager` + register vehicle colliders
7. Migrate track collision to `CollisionSystem`
8. Add terrain as a collision source
9. Update `MeshRenderingService` to accept `Id<Mesh>` (rendering integration)
