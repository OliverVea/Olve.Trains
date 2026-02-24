

## Platform Support

| Platform | Status |
|---|---|
| Windows | Supported |
| Linux | Supported |
| macOS | Planned |
| Nintendo Switch | Planned |
| Xbox Series | Planned |
| PlayStation 5 | Planned |
| iOS / Android | Planned |

See [docs/platform-strategy.md](docs/platform-strategy.md) for priority analysis and porting notes.

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline


## Validation

A build+screenshot validation script verifies the full pipeline end-to-end: asset compilation, build, headless game launch, entity placement, and screenshot capture.

```bash
# Full validation (asset pipeline + build + headless screenshot)
./scripts/validate-build.sh

# Skip build steps (for CI, when binary already exists)
./scripts/validate-build.sh --skip-build
```

This runs automatically in CI as the `validate-screenshot` job after the build completes. The screenshot is uploaded as a build artifact for visual inspection.


## TODO

(updated 02/17/2026 [mm/dd/yyyy])

- [x] Track creation epic:
  - [x] Allow ghost preview during track creation
  - [x] Disallow tracks with collisions with geometry, other tracks, and extreme curvature
  - [x] Display ghost previews of disallowed tracks in red
- Terrain epic:
  - [ ] Add water rendering for y < 0
- Configuration epic:
  - [ ] Create engine-side system for managing configuration
    - all kinds of configuration: graphics, interface, audio, game options, bindings
    - bindings should allow key/mouse/other bindings
    - categorization and sub categorization. E.g. 'channel balance' should be allowed to have categorization 'audio' and sub category 'mix'. Key binds can be 'bindings' and sub category 'GUI' for 'select place track'.
  - [ ] Implement game-side configuration
    - create the specific configuration that can be set/referenced
    - create defaults for e.g. binding 
  - [ ] Allow UI source generated elements to reference key bind keys. E.g. 'SELECT_PLACE_TRACK' is looked up in config and then used to activate PlaceTrackButton.
- Building epic:
  - [x] Add basic support for buildings
  - [x] Add building validation and validation failure rendering
  - [x] Validate collisions
  - [ ] Add station with station track
  - [x] BUG: don't show building ghost when there's no terrain intersection with mouse
  - [ ] Design building aspect system — buildings can have multiple aspects (residential, station, industry) as supplementary systems that are notified on building add/remove
- Collision system epic:
  - Description: Centralized collision system (e.g. Id<Collider>) queryable when placing tracks, buildings, or future obstacles like trees. Replaces per-type validation with a unified approach.
  - [ ] Design generalized collision system with typed collider IDs
  - [ ] Migrate track collision checks to central system
  - [ ] Migrate building collision checks to central system
  - [ ] Add terrain as a collision source
- [x] Deletion epic:
  - [x] Add deletion tool
  - [x] Allow deleting tracks
  - [x] Allow deleting trains
  - [x] Allow deleting vehicles
- Industry epic:
  - [ ] Add basic support for industries
  - [ ] Add building for industries
  - [ ] Add 'harvest range' for buildings and register entities within range
  - [ ] Create game-side logic for supporting cargo and train inventories
  - [ ] Create game-side logic for generating cargo in stations next to industries
  - [ ] Create game-side logic for consuming cargo delivered to stations
- Residential buildings epic:
  - [ ] Add residential building type, blueprint (1x1), and placement tool
  - [ ] Add variable-sized residential blueprints (bias toward medium)
  - [ ] Add 'buildings' layer to ORA map format (grey color)
  - [ ] Add deterministic algorithm to split ORA building blobs into variable-sized houses
- Random generation epic:
  - [ ] Add random terrain generation
  - [ ] Add random residential building generation
- Cities epic:
  - Description: Cities form automatically from clusters of residential buildings. On building placement, unassigned houses are checked against a density map — if a density spike exceeds a threshold, a new city is created. Houses near existing cities are assigned to them. Cities have a level that can be upgraded by delivering cargo. When a city upgrades, buildings in the city gradually upgrade to match the city level. A designated town hall upgrades immediately. Cities can also merge when they grow into each other.
  - [ ] Create city entity with name, level, and assigned buildings
  - [ ] Listen to building added/removed events to trigger city evaluation
  - [ ] Calculate density map from unassigned residential buildings
  - [ ] Create new city when density spike exceeds threshold
  - [ ] Assign nearby buildings to existing cities
  - [ ] Add city merging when cities grow into each other
  - [ ] Add city level and cargo-based upgrading
  - [ ] Add town hall designation (upgrades immediately with city level)
  - [ ] Gradually upgrade city buildings to match city level
- Building upgrading epic:
  - [ ] Add basic support for building upgrades
  - [ ] Require resources for building upgrades
- Shadow epic:
  - Description: Add shadow mapping to the game. Requires shader pipeline changes (removing geometry shaders, adding SDF grid), FBO infrastructure for the shadow depth pass, and receiver shader modifications to sample the shadow map.
  - [x] Remove terrain geometry shader — compute flat normals via `dFdx`/`dFdy` in fragment shader, render grid overlay as SDF in terrain fragment shader, delete wireframe shader files
  - [ ] Add shadow map generation — FBO wrapper, depth-only pixel format, depth-only shader, `ShadowMapService` that renders casters from light-space each frame
  - [ ] Add shadow map sampling to receiver shaders — terrain, default, and building fragment shaders sample the shadow map to attenuate diffuse lighting
- Track rendering epic:
  - [ ] Improve rendering of tracks by procedurally generating the track mesh from the underlying spline
  - [ ] Create and render procedurally generated station meshes
  - [ ] Improve positioning of track signals based on track positioning
- Unit geometry:
  - [x] Extract unit cube to `UnitCube` data class in `Olve.Engine3D/Rendering/Primitives/` with `RegisterUnitCube` extension on `RenderingManager3D`
  - [ ] Extract unit quad to `UnitQuad` in `Olve.Engine3D/Rendering/Primitives/` with `RegisterUnitQuad` extension
  - [ ] Add `UnitLineSamples` for GPU-side spline tessellation (unit t-value vertex buffer for instanced spline rendering)
- Detached mode:
  - Description: For e.g. Agentic AI access to the game for debugging, we want to be able to run with --detached or -d. Then, we should be able to use the cli to interact with the game, enumerating options (for main menu it could be clicking buttons, for the game it could be listing and placing tracks, buildings, trains). We should also be able to take screenshots of the screen as .pngs.
  - [x] Launch game in detached mode
  - [x] Add argument to send command to detached game by id
  - [x] Add a simple echo command
  - [x] Add commands for navigating the main menu
  - [x] Add support for taking screenshots of the current state of the game
  - [x] Add predefined command handler argument parsers (e.g. TilePosition, Vector3, CardinalDirection)
- OpenTelemetry metrics epic:
  - [x] Add engine-level OTel metrics integration (frame time, render time, update time, entity counts)
  - [ ] Add per-scene metrics breakdown (tagged by human-readable scene service identifier, possibly sampled)
  - [ ] Add game-level OTel metrics (track count, vehicle count, building count, command throughput)
- Replay & scripting epic:
  - Description: Enable deterministic replay of game sessions for bug investigation and integration testing. An LLM should be able to take a game log and generate a replay script. The game should support a script file syntax that can be passed as a replay, and manual simulation stepping (disabling auto-step) via a command.
  - [ ] Define script file syntax for replay sequences (commands + timing/step triggers)
  - [ ] Add `--replay <script>` CLI argument to load and execute a script file
  - [ ] Add manual simulation stepping command (pause auto-step, advance frame-by-frame)
  - [ ] Add game state logging sufficient for LLM-based replay script generation
- Test coverage epic:
  - [ ] Add tests for core engine services (SceneManager lifecycle, GameManager)
  - [ ] Add tests for command pipe communication (CommandPipeServer/Client)
  - [ ] Add tests for game logic services (tracks, vehicles, buildings)
  - [ ] Add test execution step to CI pipeline
- Localization epic:
  - Description: A system for managing translated strings across multiple languages. GUI elements and other user-facing text reference string IDs instead of hardcoded text. The active locale determines which translation is returned at lookup time.
  - [ ] Design localization system with string ID lookup (e.g. `ILocalizer.Get(StringId)`)
  - [ ] Define a storage format for translation files (e.g. JSON/TOML per locale)
  - [ ] Integrate with UI system so GUI elements reference string IDs instead of hardcoded text
  - [ ] Add fallback behavior (missing translation falls back to default locale)
  - [ ] Add initial English locale as the default/baseline
  - [ ] Add tooling or workflow for adding new locales
- Main menu improvements:
  - [x] Add FPS counter to the info bar (right subsection, left of main menu button)
  - [ ] Make main menu into burger button, trigger menu in middle of game with main menu button as only option for now