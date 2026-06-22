## Bugs

(none)

## Manual Verification

- [ ] Put a breakpoint in `LoadingService.Update()` and verify the loading scene flow works: MainMenu → LoadingScene (loading indicator visible, background task runs) → GameLogicScene

## Tech Debt

- [ ] Integration tests should use user-facing validated placement tools (e.g. select tool, click to place) instead of raw `place-track`/`place-building` commands. If validation logic breaks, raw commands bypass it silently — at least some tests should go through the full GUI placement flow to catch that.
- [ ] Move `BoundedContainer<TKey>` from `Olve.Engine3D.Collections` to `Olve.Utilities.Collections` (it's a general-purpose data structure, not engine-specific)
- [ ] Move entity counting metrics into `EntityStore` — services like `BuildingService` manually call `GameMetrics.BuildingCount.Add(1/-1)` on add/remove; this should be handled automatically by `EntityStore` so all entity types get metrics for free
- [ ] Station ghost track preview (with per-component parameters) — ghost preview for station tracks when placing stations
- [ ] Remove `TerrainHighlightSettings` shared-settings pattern — replace with a more explicit service-based approach
- [ ] GUI layout sizing — `GuiLayoutService` does not stretch cross-axis children to fill the parent (an unsized child's cross-axis size is 0), and unsized root nodes fill the entire anchor surface (1920x1080) instead of shrinking to content. The skipped tests in `GuiLayoutServiceTests`/`GuiLayoutServicePositioningTests` (`NestedWidthTest`, `TripleNested_Layout_Sizes_And_Positions`, `Positions_Nested_LeftRight_With_Fill_Weights`) specify the intended behavior; un-skip them once implemented. Also drop the obsolete `TryGetBoxPosition_Fails_Before_ComputeLayout` test (the method auto-computes layout, so its premise no longer holds).

## Infrastructure

- [ ] Migrate CD to Olve.Pipelines epic (Tooling):
  - Description: Move continuous delivery off GitHub Actions onto Olve.Pipelines (GitOps CD). Every push to master runs a single `build` step (asset pipeline + Linux and cross-compiled Windows publishes; a single step because parallel production steps deadlock the controller), gates on the screenshot test, then `publish-s3` (own bucket first) then `publish-itch`. CI is master-only — no PR checks.
  - [x] Add `.pipelines/config.yaml` + step scripts (build, test, publish-s3, publish-itch)
  - [x] Remove the three GitHub Actions workflows and update CI/CD docs (CLAUDE.md, docs/architecture.md)
  - [x] Bind the pipeline (`POST /api/pipelines/with-repo`), set secrets; `olve-trains-dist` bucket self-creates in the steps
  - [x] Verify a master push builds, tests, and publishes — confirmed end to end through `publish-s3` (versioned + `latest/` + presigned URLs)
  - [x] Set the `ITCH_API_KEY` secret (from OpenBao `external/itch.io`) — full chain green, builds publishing to itch.io `:linux`/`:windows`
  - [ ] Rotate the bootstrap `GITHUB_TOKEN` (currently the broad `gh` token) to a fine-grained read-only Olve.Trains token
  - [ ] (Later) Source screenshot references from the VR app instead of git LFS; re-add VR review-on-failure (interim: test step uploads diffs to `s3://olve-trains-dist/diffs/`)

## Demo

### Core infrastructure (build first — other features depend on these)

- [x] GUI widgets epic (Technical):
  - Description: Add standard interactive widget types to the engine-level GUI system (Olve.Engine3D). These are needed for both in-game UI (options menus, depot builder, city info panels) and debug tooling. Each widget is a GuiElement subclass composed from existing primitives (Box), with a corresponding engine service for interaction logic.
  - [x] Add Slider widget (track + thumb, value from min/max, drag-to-set via mouse polling)
  - [x] Add Checkbox widget (box + checkmark, toggle on click)
  - [x] Add Radio Button widget (group of options, single selection)
  - [x] Add Dropdown widget (collapsed box that expands to show options, single selection)
- [x] Train depot epic (Feature):
  - Description: Train depots are buildings where players build and customize trains. Place them like a station, click it to open a train builder UI. Players choose a locomotive, add/remove/reorder wagons, and deploy the train onto the depot's track. Replaces the current auto-spawn with 2 goods wagons.
  - [x] Add train depot building type (placed with new owned track, like stations)
  - [x] Add depot UI panel (click depot → open train builder)
  - [x] Add 'create new train dialog' in the UI - creating a new train immediately deploys the train to the depo track
  - [x] Allow for editing existing train in the UI - a train on the depo track can be edited until released
  - [x] Remove automatic 2-goods-wagon attachment on train creation
- [x] Observability epic (Tooling):
  - Description: Every player action must be visible in logs as a replay-able command, and the full game state must be queryable via the command interface. This enables replay files, debugging, and AI-assisted testing. This is also a design principle — all future features must log actions and expose query commands.
  - [x] Log track placements with start/end coordinates and directions (not just track ID)
  - [x] Log building placements as replay-able commands (type, position, direction)
  - [x] Log train creation, wagon add/remove, and deletion actions with full parameters
  - [x] Add `list-tracks` command — returns all tracks with their start/end endpoints and directions
  - [x] Add `query-track` command — returns full track details (endpoints, directions, connected junctions)
  - [x] Add `list-buildings` command — returns all buildings with type, position, and direction
  - [x] Audit existing commands and logs for completeness — every entity type should have list/query commands
- [x] Placement clearance epic (Feature):
  - Description: Environmental objects (trees, mushrooms) can be removed by the player and are auto-cleared when placing buildings or tracks on top of them. Other entities (buildings, tracks, trains) block placement and are never auto-cleared. The system distinguishes auto-clearable entities from blocking ones, and shows a preview of what will be deleted before the player commits to a placement.
  - [x] Add tree deletion via the delete tool (click a tree to remove it)
  - [x] Define clearance categories — auto-clearable (trees, environmental objects) vs blocking (buildings, tracks, trains) vs immovable (terrain)
  - [x] Auto-clear environmental objects when placing buildings (remove trees/etc. that overlap the building footprint)
  - [x] Auto-clear environmental objects when placing tracks (remove trees/etc. that overlap the track path)
  - [x] Show deletion preview during placement — highlight environmental objects that will be auto-cleared before the player confirms placement
- [x] Shadow epic (Visual):
  - Description: Add shadow mapping to the game. Requires shader pipeline changes (removing geometry shaders, adding SDF grid), FBO infrastructure for the shadow depth pass, and receiver shader modifications to sample the shadow map.
  - [x] Remove terrain geometry shader — compute flat normals via `dFdx`/`dFdy` in fragment shader, render grid overlay as SDF in terrain fragment shader, delete wireframe shader files
  - [x] Add shadow map generation — FBO wrapper, depth-only pixel format, depth-only shader, `ShadowMapService` that renders casters from light-space each frame
  - [x] Add shadow map sampling to receiver shaders — terrain, default, and building vertex+fragment shaders pass light-space position and sample the shadow map to attenuate diffuse lighting
  - [x] Fit shadow map to camera frustum — compute light-space ortho bounds from camera view-projection instead of the full terrain AABB, so the shadow map resolution is spent on visible geometry
  - [x] Add PCF shadow softening — re-add percentage-closer filtering for soft shadow edges
  - [x] Switch to Variance Shadow Maps (VSM) — store depth + depth² in RG32F, use Chebyshev's inequality for smooth filterable shadows, add optional Gaussian blur pass

### Gameplay mechanics (build after core systems are validated)

- [x] Money epic (Feature):
  - Description: The economic feedback loop. Players earn money from cargo deliveries and spend it on tracks, buildings, and trains. Without money the demo is a sandbox with no goals.
  - [x] Add money/balance entity and service
  - [x] Earn money from cargo deliveries (pay out when cargo reaches a consuming industry or city)
  - [x] Charge money for placing tracks
  - [x] Charge money for placing buildings and trains
  - [x] Add money display to UI (balance in the info bar)
- [ ] Save/Load epic (Feature):
  - Description: Save and load game state. A LoadingScene sits between MainMenu and Game, reading save files asynchronously and building GameSceneArguments. Saving serializes root game state to JSON — only root entities (tracks, buildings, trains, wagons, money, time, signal rules, environmental objects) since derived state (junctions, stations, depots, industries, cities, resources) is auto-created by the event system on load. GameSceneArguments is the single data contract: both new games and loaded games flow through it. Loading uses background tasks for file I/O, deserialization, and GameLogic service initialization, with GPU work finalized on the main thread. Save files carry a schema version for forward-compatible deserialization. OneOf-based types (SignalRuleTrain, SignalRuleSource, etc.) use JsonDerivedType subtype discriminators.
  - [x] Add typed scene-argument mechanism to SceneManager (pass args into LoadScene / LoadAndActivateScene, resolvable from scene services)
  - [x] Use arguments in GameLogicScene to initialize starting money/entities
  - [x] Add LoadingScene with async task infrastructure (MainMenu → LoadingScene → GameLogicScene, no save file yet — just passes default GameSceneArguments, renders a loading indicator)
  - [x] Move CPU-side asset managers (AssetLoader, MeshManager, MeshLoadingManager, TextureManager, TextureLoadingManager) from scoped to singleton so asset caches survive across scene transitions
  - [x] Pre-warm asset caches during LoadingScene — load all assets from generated catalogs on a background thread so game scene Load() hits cache instead of disk
  - [x] Background game scene initialization — run GameLogicScene DI scope creation + parameter service + logic service Load() calls on a background thread during LoadingScene, then finalize GPU work (GameRendering/GameUI scene loads) on the main thread
  - [ ] Define save file format (versioned JSON schema covering money, time, terrain args, camera state, environmental objects)
  - [ ] Add save game command — serialize current minimal state (money, time, terrain args, environmental objects) to save file
  - [ ] Add load game support in LoadingScene — read save file on background thread, deserialize, populate GameSceneArguments, transition to game
  - [ ] Expand GameSceneArguments and save format with track data
  - [ ] Expand with building data (type + position — stations, depots, industries, residences auto-created via events)
  - [ ] Expand with train data (trains, wagons, cargo, positions, motion state, cargo transfer policies, train groups)
  - [ ] Expand with signal rules (OneOf types via JsonDerivedType discriminators; TODO: persist RoundRobin counter if needed)
  - [ ] Add save/load UI (save button in burger menu, load from main menu)
- [ ] CLI output epic (Tooling):
  - Description: Command output is currently raw JSON, which wastes tokens in agentic usage and is hard to read for humans. Add a `--nice` flag to all query/list commands that produces brief, scannable plain-text output optimized for both human and AI consumption. JSON remains the default for programmatic use.
  - [ ] Add `--nice` flag support to the command system (opt-in per command)
  - [ ] Add `--nice` formatters for all query/list commands (e.g. `City 1 | 2 residences | balance: $3913`)
- [ ] Cities epic (Feature):
  - Description: Cities are simple, transparent demand targets for the demo. A city has a level (1-4) and each level requires specific goods at specific rates. Delivering goods earns money and progresses the city. Higher levels unlock new demand tiers requiring cross-network transport. Cities are hardcoded on the demo map — procedural city placement is a v1.0 concern.
  - [x] Create city entity with residences attached (name/level/demand table deferred to the leveling step)
  - [x] Cities consume goods delivered to nearby stations and pay out money
  - [ ] City leveling — deliver enough goods to reach the next level, unlocking new demand tiers
  - [ ] Design demand tiers so levels 1-2 use isolated lines, levels 3-4 require cross-network cargo
  - [ ] City UI showing current level, demand, and progress toward next level
  - [ ] Hardcode cities on the demo map with pre-placed residential clusters
- [ ] Track rendering epic (Visual):
  - [x] Improve rendering of tracks by procedurally generating the track mesh from the underlying spline
  - [ ] Add sleepers/ties to track mesh generation
  - [ ] Create and render procedurally generated station meshes
  - [ ] Improve positioning of track signals based on track positioning
  - [ ] Add train stop sign at the end of dead-end tracks
- [ ] Terrain environment epic (Visual):
  - Description: Make the terrain feel like a real landmass instead of a floating mesh. Extrude cliff/dirt sides along terrain borders down to a water plane, giving a natural island look.
  - [ ] Add water plane below terrain height (flat quad with blue/water shading)
  - [ ] Add cliff/dirt side extrusion along terrain edges (vertical quads from border down to water level)

### Content & polish (final stretch — systems are proven, fill in the content)

- [ ] Demo map epic (Feature):
  - Description: A single hand-crafted map for the demo. Geography should teach systems through natural bottlenecks — force the player to confront signal complexity, cross-line cargo transfers, and network scaling at specific points. Resource placement creates natural transport corridors. The map should support meaningful play at city levels 1-4 with different optimal strategies at each tier. Depends on all core systems being in place (industries, cities, money, resources).
  - [ ] Design the demo map (terrain shape, elevation, resource deposits, city seed locations, natural bottleneck points)
  - [ ] Implement map loading from a designed heightmap/layout format
  - [ ] Place pre-defined resource deposits and initial industries on the map
  - [ ] Place initial residential clusters / city seeds on the map

- [ ] Industry content epic (Feature):
  - Description: The cargo system infrastructure is done. This epic adds the full set of industries, recipes, and cargo types needed for the game, plus the in-game industry builder tool so players can place industries from the toolbar.
  - [ ] Design the full industry/cargo graph (what industries exist, what they consume/produce, production chains)
  - [ ] Implement all industry types, recipes, and cargo types from the design
  - [ ] Add industry builder tool (toolbar button → select industry type → click to place)
- [ ] Audio support epic (Feature):
  - [ ] Add basic support for playing audio
  - [ ] Add in-game music
  - [ ] Add GUI sound effects
  - [ ] Add in-game sound effects
  - [ ] Add spatial effects for e.g. localized sounds, wind blowing when the camera is zoomed out, and so on
- [ ] Game feel epic (Visual):
  - Description: Juice and polish to make interactions feel satisfying. Placement animations (grow/stretch/plop) for buildings and tracks, synced with audio cues. Depends on audio support.
  - [ ] Add placement animation system (scale/bounce keyframes on newly placed entities)
  - [ ] Sync placement animations with audio cues (plop/click SFX)
  - [ ] Add placement animations for buildings
  - [ ] Add placement animations for tracks
- [ ] Train perspective camera epic (Feature):
  - Description: First-person camera from the train's perspective as it drives around. Includes a skybox so the sky looks correct from the train's viewpoint. Camera position and orientation derived from the train's current spline parameter via TrackSplineService.
  - [ ] Add skybox rendering (cubemap or gradient shader, drawn behind all geometry)
  - [ ] Add perspective camera mode that follows a selected train along its track spline
  - [ ] Add UI toggle to enter/exit train perspective (keybind or button when selecting a train)
- [ ] Biome map epic (Feature):
  - Description: Drive environmental object placement from biome layers in the terrain ORA file. The ORA parsing infrastructure already exists (Olve.OpenRaster, TerrainFileReader, HeightmapLayerParser). Add new named layers (e.g. "forest") that encode density/probability per tile. Placement samples the density map with a threshold and a deterministic seed (from ORA metadata or hashed map name) so generation is reproducible. Replaces the current hardcoded random tree scattering in TerrainService.
  - [ ] Extract a seed from the ORA file (metadata field or hashed map name) and pass it through to placement
  - [ ] Add density map layer parser and extract biome layers (e.g. "forest") from the terrain ORA file alongside the heightmap
  - [ ] Add biome map service that provides per-position density lookups by layer name
  - [ ] Drive tree placement from a "forest" density layer (threshold + sampling rate)
  - [ ] Add mushroom environmental object placed in forests above a secondary density threshold
  - [ ] Remove hardcoded tree placement from TerrainService in favour of biome-driven placement
- [ ] Terrain epic (Visual):
  - [ ] Add bumpy terrain using low-res noise texture for low-poly smooth height variation
- [ ] Toolbar icons epic (Visual):
  - Description: Current toolbar icons are placeholder quality. Generate icons from the underlying 3D models (render model to texture) for a consistent, polished look.
  - [ ] Investigate rendering models to texture for icon generation
  - [ ] Generate toolbar icons from 3D models
- [ ] GUI clipping epic (Technical):
  - Description: Add clipping rectangle support to the GUI rendering system. Elements can define a clip mask so children are only visible within the parent's bounds. Enables slide-in animations (e.g. a progress bar appearing from behind another element, rising into view) and scroll containers. Needed by depot UI and info panels.
  - [ ] Add clip rectangle property to GUI elements (inherited by children, intersected hierarchically)
  - [ ] Implement scissor test or stencil-based clipping in the 2D rendering pass
  - [ ] Add slide-in animation support using clip masks (e.g. inventory bar rising into view)
- [ ] Main menu improvements (Visual):
  - [x] Add FPS counter to the info bar (right subsection, left of main menu button)
  - [x] Make main menu into burger button, trigger menu in middle of game with main menu button as only option for now
  - [ ] Make a visually appealing main menu
- [ ] GUI layout previewer (Tooling):
  - [ ] Standalone viewer that renders XML layouts via the existing GUI system
  - [ ] File watcher for live reload on XML changes

- [ ] Train collisions epic (Feature):
  - Description: Prevent trains from occupying the same track space. On collision, both trains immediately set speed to 0 (simplified — no physics knockback for now). Placement must also prevent spawning trains on top of existing ones.
  - [ ] Add collision detection — check if any two trains overlap on the same track segment each tick
  - [ ] On collision, set both trains' speed to 0
  - [ ] Prevent train placement on occupied track — reject placement if another train already occupies the target position
  - [ ] Log an error if overlapping trains are ever detected (defensive guard for edge cases)
- [ ] Train stopping epic (Feature):
  - Description: Trains stop at stations and depots instead of passing through. When approaching a stop, the train targets a speed of 1/120% of the kinematically-correct braking speed for the remaining distance — this 20% overshoot margin ensures the train always reaches the stop point even with floating-point drift. Trains decelerate smoothly and come to a full stop at the station/depot track position.
  - [ ] Add stop target system — trains identify upcoming stations/depots on their route and compute a braking curve with 1/1.2× speed margin
  - [ ] Implement smooth deceleration along the braking curve so trains come to rest at the stop point
  - [ ] Add station stop behavior — train pauses at station for a duration (loading/unloading), then departs
  - [ ] Add depot stop behavior — train stops at depot and remains until released by the player

### Pre-demo polish (final gate before demo release)

- [ ] Graphics overhaul epic (Visual):
  - Description: Comprehensive visual upgrade pass. Batch all graphics improvements together since they're interconnected (AO interacts with lighting, particles need to look right under new lighting, etc.). Build the particle/VFX system as a generalized engine feature in Olve.Engine3D for reuse in future games.
  - [ ] Add particle/VFX system to Olve.Engine3D (GPU-instanced billboards, emitter shapes, lifetime/velocity, color curves, blend modes)
  - [ ] Add locomotive smoke and steam particles
  - [ ] Add ambient occlusion (SSAO or similar)
  - [ ] Rework lighting model (soft lighting, Townscaper-inspired)
  - [ ] Improve materials (better shading, surface variation)
  - [ ] Add additional light sources beyond the single directional sun/moon
  - [ ] Add placement dust/effects using the particle system
  - [ ] General visual polish pass across all rendered elements
- [ ] GUI overhaul epic (Visual):
  - Description: Complete visual redesign of all GUI panels and elements. Final art pass with polished, responsive feel. This is the last step before demo launch — all systems and gameplay must be done first.
  - [ ] Design new GUI visual language (color palette, typography, spacing, border styles)
  - [ ] Redesign all existing panels (toolbar, info bar, depot, station info, industry info, signal rules, day time, burger menu)
  - [ ] Add responsive hover/click/transition animations across all interactive elements
  - [ ] Make a visually appealing main menu
  - [ ] Final polish pass on all GUI elements

---

Other milestones: [v1.0](docs/milestones/v1.0.md) · [v1.1](docs/milestones/v1.1.md) · [Done](docs/milestones/done.md)