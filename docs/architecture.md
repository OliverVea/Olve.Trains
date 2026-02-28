# Architecture Reference

Detailed reference for the Olve.Trains codebase. See [CLAUDE.md](../CLAUDE.md) for build/run instructions and workflow conventions.

## Project Structure

```
src/
  Olve.Engine3D/           # Core engine library (rendering, scenes, GUI, commands)
  Olve.Trains/             # Main game application ("On Track To Grow")
  Olve.Trains.AssetPipeline/  # Asset compiler (shaders + layouts → C# classes)
tests/
  Olve.Engine3D.Tests/     # Unit tests (TUnit framework)
scripts/
  integration-test.sh      # Integration test with station, tracks, trains, time-of-day screenshots
```

**Dependency graph:**
- `Olve.Trains` → `Olve.Engine3D`
- `Olve.Trains.AssetPipeline` → `Olve.Engine3D`
- `Olve.Engine3D.Tests` → `Olve.Engine3D`

## Rendering Pipeline

### Shader Inventory

6 shader pairs in `src/Olve.Trains/resources/shaders/`:

| Shader | Instanced Attributes | Purpose |
|--------|---------------------|---------|
| default | `iWorld` (mat4) | Standard mesh with texture + normals + diffuse lighting (buildings, 3D objects). Uses white pixel texture for untextured meshes. |
| track | `iP0`, `iP1`, `iT0`, `iT1` (vec3s) | Hermite spline track rendering |
| lineStrip | none | Lines with per-vertex color |
| msdfText | `iPosPx`, `iSizePx`, `iTint`, `iUvMin`, `iUvMax` | MSDF text glyph rendering |
| texturedRectangle | `iPosPx`, `iSizePx`, `iTint`, `iBorderWidthPx`, `iBorderColor`, `iBorderRadiusPx` | GUI rectangles with borders/rounded corners |
| terrain | none (uses `gl_VertexID`) | Procedural heightmap terrain + grid overlay (flat normals via `dFdx`/`dFdy`) |

Note: The `building` shader was merged into `default`. The `terrainWireframe` shader was removed (grid overlay is now SDF-based in the terrain fragment shader).

### Asset Pipeline Code Generation

The pipeline reads GLSL shaders and generates C# classes via Scriban templates (`src/Olve.Trains.AssetPipeline/templates/ShaderClass.scriban`). Each shader gets:

1. **Uniform properties** — nullable, settable (e.g., `public Vector3D<float>? UColor { get; set; }`)
2. **`EntityParameters` record** — implements `IShaderParameters` for per-group overrides
3. **`Vertex` record** — implements `IVertexData` + composable interfaces (e.g., `IWithPosition3D<Vertex>`, `IWithNormal3D<Vertex>`): `FloatCount`, `WriteTo(Span<float>)`, `ConfigureAttributes(GL)`
4. **`Instance` record** — implements `IInstanceData<Vertex>` + composable interfaces (e.g., `IWithWorldMatrix<Instance>`): same API as Vertex but with `gl.VertexAttribDivisor(location, 1)`. All shaders are always-instanced.

Layout XML files generate C# builder classes via `LayoutClass.scriban`. Each layout produces a record with named element properties and a flat `IReadOnlyList<GuiElement>` of all descendants.

### Unified RenderingManager

All rendering (2D and 3D) goes through a single `RenderingManager` backed by three sub-managers. File: `src/Olve.Engine3D/Rendering/RenderingManager.cs`

**Sub-managers:**
- **`GeometryManager`** — registers/updates/deregisters mesh geometry
- **`RenderingGroupManager`** — groups = geometry + shader + render state + instances
- **`RenderingInstanceManager`** — add/update/remove instances within groups

**Geometry registration** (once per shape):
```csharp
// Indexed geometry
Result<GeometryId<TVertex>> Register<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<uint> indices,
    PrimitiveType primitiveType = Triangles, BufferUsageARB usage = StaticDraw) where TVertex : IVertexData

// Non-indexed (e.g., terrain uses gl_VertexID)
Result<UntypedGeometryId> RegisterDrawArrays(uint vertexCount, PrimitiveType primitiveType = Triangles)

Result UpdateVertices<TVertex>(GeometryId<TVertex>, ReadOnlySpan<TVertex> vertices, BufferUsageARB usage = DynamicDraw)
Result Deregister(UntypedGeometryId)
```

**Group registration** (geometry + shader + render state):
```csharp
Result<GroupId<TInstance>> Register<TVertex, TInstance>(
    GeometryId<TVertex>, IShader, RenderState,
    PrimitiveType primitiveType = Triangles, int sortKey = 0, IShaderParameters? groupParameters = null)
    where TVertex : IVertexData where TInstance : IInstanceData<TVertex>
```

**Instance management** (many per group):
```csharp
Result<Id<TInstance>> Add<TInstance>(GroupId<TInstance>, TInstance data) where TInstance : IInstanceData
Result Update<TInstance>(GroupId<TInstance>, Id<TInstance>, TInstance data)
Result Remove<TInstance>(GroupId<TInstance>, Id<TInstance>)
```

**Rendering flow:** `RenderAll()` iterates groups sorted by `sortKey`, rebuilds dirty instance buffers, applies render state (`RenderState`: blend mode, depth write, depth test), loads shaders, and executes instanced draw calls.

**Type-safe IDs:** `GeometryId<TVertex>` and `GroupId<TInstance>` prevent mixing geometry/instance types at compile time.

### Primitives

`src/Olve.Engine3D/Rendering/Primitives/`:

- **`UnitCube`** — 24 vertices (4/face), 36 indices. `Populate<T>()` fills any vertex type implementing `IWithPosition3D<T>` + `IWithNormal3D<T>`.
- **`UnitQuad`** — 6 vertices (2 triangles), no indices. `Populate<T>()` fills any vertex type implementing `IWithPosition2D<T>`. Covers [0,0]→[1,1] for 2D GUI rendering.
- `UnitLineSamples` does not exist yet (TODO item).

### Key Types

- **`GeometryId<TVertex>`** — type-safe geometry ID, inherits `UntypedGeometryId`
- **`GroupId<TInstance>`** — type-safe group ID, inherits `UntypedGroupId`
- **`Id<TInstance>`** — instance ID within a group (from `Olve.Utilities`)
- **`IVertexData`** — vertex data interface: `FloatCount`, `WriteTo(Span<float>)`, `ConfigureAttributes(GL)`
- **`IInstanceData<TVertex>`** — instance data interface (same API), typed to its vertex type
- **Composable attribute interfaces** — `IWithPosition3D<T>`, `IWithNormal3D<T>`, `IWithTexCoords2D<T>`, `IWithColor3D<T>`, `IWithPosition2D<T>`, `IWithWorldMatrix<T>`. Enable generic mesh construction (e.g., `UnitCube.Populate<T>()` works with any vertex type that has position + normal).
- **`RenderState`** — `record struct(BlendMode, DepthWrite, DepthTest)` with presets: `Opaque`, `AlphaBlend`, `AlphaBlendNoDepthWrite`, etc.
- **`PrimitiveType`** — from Silk.NET: `Triangles`, `LineStrip`, `LineLoop`, etc.

### Typical Usage Pattern

```csharp
// 1. Register geometry (once)
var geometryId = geometryManager.Register<Shaders.Default.Vertex>(vertices, indices);

// 2. Register group (geometry + shader + render state)
var groupId = renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance>(
    geometryId, shader, RenderState.Opaque);

// 3. Add/update/remove instances
var instanceId = renderingInstanceManager.Add(groupId, new Shaders.Default.Instance(worldMatrix));
renderingInstanceManager.Update(groupId, instanceId, new Shaders.Default.Instance(newMatrix));
renderingInstanceManager.Remove(groupId, instanceId);
```

## Entity Management

**`EntityStore<T>`** (composition pattern): wraps `IdDictionary<T>` with `TryAdd`, `Set`, `Remove`, `TryGet`, `Exists`, `Where`. Exposes `OnAdded`/`OnRemoved` events. Services compose this directly and expose domain-specific event names (e.g., `OnTrackAdded`).

## Scene System

4 scenes with parent-child hierarchy:

| Scene | Layer | Parent | Role |
|-------|-------|--------|------|
| MainMenuScene | 0 | (root) | Main menu UI |
| GameLogicScene | 0 | (root) | Game state, tracks, vehicles, lighting |
| GameRenderingScene | 1 | GameLogicScene | 3D rendering |
| GameUIScene | 2 | GameRenderingScene | In-game UI overlays |

**Key concepts:**
- **`SceneDefinition`**: `Id<IScene>`, `Name`, `LayerOrder`, optional `ParentId`
- **DI scoping**: root scenes create a new `IServiceScope`; child scenes share parent's scope
- **Lifecycle**: `Unloaded → Inactive → Active → Inactive → Unloaded`
- **Service ordering**: by `Priority` (ascending). `SceneServicePriority.FromDependencies(...)` / `FromDependents(...)`
- **State guard**: `Update()` and `Render()` check `State == Active` before each service — if a service unloads the scene mid-iteration, remaining services are skipped
- **`EventSceneService<T>`**: bridges events to scene lifecycle — subscribes on load, queues, processes on update, unsubscribes on unload

**Service registration**: `AddSceneService<T>(sceneId)` / `AddEventSceneService(...)`

**Loading**: `LoadAndActivateScene(sceneId)` walks parent chain, loads parent-first. Child scenes loaded via parent automatically get the parent's scope.

## GUI System

### Layout Files

XML in `src/Olve.Trains/resources/layouts/` (MainMenu.xml, ToolBar.xml, InfoBar.xml).

```xml
<Box id="Root" Justify="Justify.Center" Align="Align.Center" Vertical="true" Gap="4">
    <Box id="StartGameButton" Style="MainMenuButtonStyle" Interactive="true">
        <Text Content='"Start Game"' Style="MainMenuButtonTextStyle" InheritParentState="true" />
    </Box>
</Box>
```

Generated to `src/Olve.Trains/assets/Layouts/` as C# records with named properties. Element IDs use `Id.FromName<GuiElement>("Layout/Type/Name")` for deterministic stable IDs.

### Element Types

In `src/Olve.Engine3D/GUI/Elements/`:

- **Box** — flexbox-like container: width/height/weight/gap/margin/padding, Justify (Start/Center/End/SpaceBetween/Around/Evenly), Align, Vertical, background/border/radius. Implements `IRenderableAsRectangle`.
- **Text** — string content, font, size, color, alignment. Implements `IRenderableAsText` + `IRenderableAsRectangle`.
- **Image** — texture reference. Implements `IRenderableAsRectangle`.

### Core Services

In `src/Olve.Engine3D/GUI/`:

- **Element management**: `GuiElementService`, `GuiNodeService`, `GuiElementRegistrations`
- **Layout**: `GuiLayoutService` / `GuiLayoutUpdateService` — computes positions and sizes
- **Rendering**: `GuiDepthService`, `GuiAnchorService`
- **Input**: `GuiMouseInputService` (priority -100), `GuiFocusService`, `GuiActivationService`, `GuiCollisionService` (AABB hit testing)
- **Styling**: `GuiStyleApplierService`, `GuiAnimationService`, `GuiNodeStateService` (hover/focus/active)
- **Game styles**: `GameStyleService` + `Styles.cs` define color schemes, fonts, borders

## Command System (Detached Mode)

Infrastructure in `src/Olve.Engine3D/Commands/`:

- **`ICommandHandler`**: `Verb`, `HelpString`, `Arguments`, `Handle(CommandContext) → Result<CommandOutput>`
- **`CommandRunner`**: resolves handlers from `CommandHandlerServiceCollection`, returns `Result<CommandOutput>`
- **Named pipe IPC**: `CommandPipeServer` (background thread) + `CommandPipeClient` (static). Protocol: 4-byte length prefix + MemoryPack payload (`CommandRequest`/`CommandResponse`)
- **`CommandQueue`**: `ConcurrentQueue<PendingCommand>` with `TaskCompletionSource<CommandResponse>`. Pipe thread enqueues, main thread dequeues.
- **`CommandProcessingService`**: scene service (priority -1000, registered in MainMenu + GameLogic) that drains the queue each update tick
- **`GameInstanceId`**: determines pipe name `olve-trains-{id}`
- **CLI args**: `--listen` / `--send "cmd"` / `--instance "id"`

## Building System

- **`Building`** (record struct): `Id<Building>` + `Id<BuildingBlueprint>` + `BuildingPosition` (TilePosition + CardinalDirection)
- **`BuildingBlueprint`** (record struct): `Id<BuildingBlueprint>` + description + `TileFootprint` (W/H/D)
- **`BuildingBlueprintLibraryService`**: predefined blueprints (Station: 4x2x2)
- **`StationPlacingToolService`**: raycast to terrain, ghost preview at 50% opacity, rotate with 'R', click to place
- **`BuildingRenderingService`**: unit cubes scaled/rotated via world matrices through `RenderingManager` (default shader with white pixel texture)
- Files: logic in `Scenes/GameLogic/Industries/`, rendering in `Scenes/GameRendering/`, UI in `Scenes/GameUI/Tools/`

## CI/CD

GitHub Actions workflow: `.github/workflows/push_master.yml` (triggers on push/PR to master).

3 jobs:
1. **build-assets** (Ubuntu) — runs asset pipeline, uploads built assets artifact
2. **build-dotnet** (matrix: linux-x64 + win-x64) — downloads assets, builds Release
3. **validate-screenshot** (Ubuntu headless) — Xvfb + Mesa software OpenGL, runs `scripts/integration-test.sh --skip-build --windowing xvfb`, uploads screenshot artifact

Headless rendering uses `LIBGL_ALWAYS_SOFTWARE=1` + Xvfb virtual framebuffer.

## Testing

### Unit Tests

`tests/Olve.Engine3D.Tests/` using **TUnit** framework. Run with `dotnet run` (TUnit Exe runner). Tests cover GUI layout computation (`GuiLayoutServiceTests`, `GuiLayoutServicePositioningTests`) and spline sandboxes.

### Integration Tests

**`scripts/integration-test.sh`** — thin wrapper around pytest (`tests/integration/`). Test scenarios live in `tests/integration/scenarios/` and use a game instance pool with per-instance Xvfb displays for parallel execution. Screenshots are compared against reference images in `tests/integration/reference/`.

Options: `--s3` (upload screenshots to S3), `--windowing native|xvfb` (default: xvfb; Windows must use native), `--skip-build`, `--update-references` (save current screenshots as new references), `--resolution WxH`.

Tests use the named pipe command system to control the game.