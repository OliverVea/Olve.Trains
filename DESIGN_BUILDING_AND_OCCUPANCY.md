# Building & Occupancy System Design

## Building System

### Core Model (game logic layer, mesh-unaware)

```
Building(Id<Building>, BuildingClassification, Position, Footprint)
BuildingClassification = Station | Industry | Residency
```

The core model knows what a building *is* and where it *sits*. Nothing about how it looks.

### Spatial Services (game logic layer)

- **`BuildingService : BaseEntityService<Building>`** — owns buildings, fires events.
- **`BuildingNameService`** (later) — separate, type-aware auto-naming via `IBuildingNamingStrategy` per classification. Each classification defines its own naming scheme (station name pools, industry-type-derived names, procedural residency names). Names can be player-overridden.

### Visual Mapping (rendering layer, configurable)

- **`BuildingAppearanceProvider`** — maps `BuildingClassification` (and optionally sub-keys) to a `BuildingAppearance(MeshId, TextureId)`. This is system-level configuration, not carried by the `Building` record.
- **`BuildingRenderingService : BaseEntityListeningService<Building>`** — listens to `BuildingService`, resolves appearance from provider, registers mesh instances via `RenderingManager3D`.

### Boundary

```
Game Logic (mesh-unaware)          Rendering (mesh-aware)
─────────────────────────          ──────────────────────
Building                           BuildingAppearanceProvider
  - Classification                   - Classification → Appearance
  - Position + Footprint
                                   BuildingRenderingService
BuildingService                      - listens to BuildingService
BuildingNameService (later)          - resolves appearance from provider
                                     - registers mesh instances
```

The core never references meshes. The rendering layer never decides *what* a building is.

---

## Occupancy System (Geometry-Based)

A shared spatial authority. All systems (buildings, tracks, trees, terrain) register their real geometry here. No tile quantization — everything uses actual shapes.

### Core Model

```
Occupancy(Id<Occupancy>, Id ObjectId, Id<OccupancyType>, IOccupancyShape Shape)
```

- `ObjectId` — the domain entity's own ID (e.g. `Id<Building>`, `Id<Track>`), opaque to the occupancy system.
- `Id<OccupancyType>` — registered per-system, not an enum. Extensible without modifying the occupancy system.
- `IOccupancyShape` — the real geometry.

### Shapes

```
IOccupancyShape
├── RectangleShape(Vector3 center, Vector2 size, float rotation)   // buildings
├── SplineCorridorShape(Spline spline, float width)                // tracks
├── CircleShape(Vector3 center, float radius)                      // trees
└── HeightfieldShape(...)                                          // terrain (future)
```

Each shape implements:

```
IOccupancyShape.GetBounds() → AxisAlignedBoundingBox     // broad-phase
IOccupancyShape.Intersects(IOccupancyShape other) → bool // narrow-phase
```

Intersection is double-dispatched — each shape pair has a specific test (rect-rect, rect-spline, circle-spline, etc.). Only needed pairs get implemented; others default to AABB overlap as a conservative fallback.

### Service

**`OccupancyService`**

```
Register(Id objectId, Id<OccupancyType> type, IOccupancyShape shape) → Id<Occupancy>
Unregister(Id<Occupancy>)
Update(Id<Occupancy>, IOccupancyShape newShape)  // for moving entities

Query:
  Intersects(IOccupancyShape proposed) → IReadOnlyList<Occupancy>
  IntersectsAny(IOccupancyShape proposed) → bool
  GetByObject(Id objectId) → Occupancy?
  GetByArea(AABB region) → IReadOnlyList<Occupancy>
```

Internally uses a spatial index (quadtree or spatial hash on XZ) with `GetBounds()` for broad-phase, then `Intersects()` for narrow-phase. Can start with brute force and optimize later.

### Coexistence Rules

```
OccupancyService.AllowCoexistence(Id<OccupancyType> a, Id<OccupancyType> b)
```

Registered as explicit exceptions. Collisions between allowed-to-coexist types are not reported as conflicts. Default: no coexistence.

### Per-System Usage

Each domain service talks to `OccupancyService` the same way:

```csharp
// BuildingService placing a building:
var shape = new RectangleShape(origin, footprintSize, rotation);
if (occupancyService.IntersectsAny(shape)) // reject placement
var occupancyId = occupancyService.Register(buildingId, buildingOccupancyType, shape);

// TrackService placing a track:
var shape = new SplineCorridorShape(spline, trackWidth);
if (occupancyService.IntersectsAny(shape)) // reject placement
var occupancyId = occupancyService.Register(trackId, trackOccupancyType, shape);
```

### What This Buys

- No tile quantization artifacts — a diagonal track doesn't block tiles it barely touches.
- Rotation and odd-shaped buildings work naturally.
- Adding a new entity type = register an `OccupancyType`, pick a shape, done.
- The occupancy system has zero knowledge of what buildings, tracks, or trees *are*.
