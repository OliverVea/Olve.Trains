## Bugs

(none)

## Tech Debt

- [ ] Integration tests should use user-facing validated placement tools (e.g. select tool, click to place) instead of raw `place-track`/`place-building` commands. If validation logic breaks, raw commands bypass it silently — at least some tests should go through the full GUI placement flow to catch that.
- [ ] Move `BoundedContainer<TKey>` from `Olve.Engine3D.Collections` to `Olve.Utilities.Collections` (it's a general-purpose data structure, not engine-specific)

## Demo

### Core infrastructure (build first — other features depend on these)

- [ ] GUI clipping epic (Technical):
  - Description: Add clipping rectangle support to the GUI rendering system. Elements can define a clip mask so children are only visible within the parent's bounds. Enables slide-in animations (e.g. a progress bar appearing from behind another element, rising into view) and scroll containers. Needed by depot UI and info panels.
  - [ ] Add clip rectangle property to GUI elements (inherited by children, intersected hierarchically)
  - [ ] Implement scissor test or stencil-based clipping in the 2D rendering pass
  - [ ] Add slide-in animation support using clip masks (e.g. inventory bar rising into view)
- [ ] Train depot epic (Feature):
  - Description: Train depots are buildings where players build and customize trains. Place them like a station, click it to open a train builder UI. Players choose a locomotive, add/remove/reorder wagons, and deploy the train onto the depot's track. Replaces the current auto-spawn with 2 goods wagons.
  - [ ] Add train depot building type (placed with new owned track, like stations)
  - [ ] Add depot UI panel (click depot → open train builder)
  - [ ] Add 'create new train dialog' in the UI - creating a new train immediately deploys the train to the depo track
  - [ ] Allow for editing existing train in the UI - a train on the depo track can be edited until released
  - [ ] Remove automatic 2-goods-wagon attachment on train creation
- [ ] Resources epic (Feature):
  - [ ] Design resource system (discrete entities, field deposits, geometric resources; harvest range; production linking)
  - [ ] Implement resource system based on design
- [ ] Shadow epic (Visual):
  - Description: Add shadow mapping to the game. Requires shader pipeline changes (removing geometry shaders, adding SDF grid), FBO infrastructure for the shadow depth pass, and receiver shader modifications to sample the shadow map.
  - [x] Remove terrain geometry shader — compute flat normals via `dFdx`/`dFdy` in fragment shader, render grid overlay as SDF in terrain fragment shader, delete wireframe shader files
  - [x] Add shadow map generation — FBO wrapper, depth-only pixel format, depth-only shader, `ShadowMapService` that renders casters from light-space each frame
  - [x] Add shadow map sampling to receiver shaders — terrain, default, and building vertex+fragment shaders pass light-space position and sample the shadow map to attenuate diffuse lighting
  - [ ] Fit shadow map to camera frustum — compute light-space ortho bounds from camera view-projection instead of the full terrain AABB, so the shadow map resolution is spent on visible geometry
  - [ ] Add PCF shadow softening — re-add percentage-closer filtering for soft shadow edges

### Gameplay mechanics (build after core systems are validated)

- [ ] Money epic (Feature):
  - Description: The economic feedback loop. Players earn money from cargo deliveries and spend it on tracks, buildings, and trains. Without money the demo is a sandbox with no goals.
  - [ ] Add money/balance entity and service
  - [ ] Earn money from cargo deliveries (pay out when cargo reaches a consuming industry or city)
  - [ ] Charge money for placing tracks
  - [ ] Charge money for placing buildings and trains
  - [ ] Add money display to UI (balance in the info bar)
- [ ] Cities epic (Feature):
  - Description: Cities are simple, transparent demand targets for the demo. A city has a level (1-4) and each level requires specific goods at specific rates. Delivering goods earns money and progresses the city. Higher levels unlock new demand tiers requiring cross-network transport. Cities are hardcoded on the demo map — procedural city placement is a v1.0 concern.
  - [ ] Create city entity with name, level, and demand table (level → required goods and rates)
  - [ ] Cities consume goods delivered to nearby stations and pay out money
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
  - [ ] Add industry info panel showing production status and inventory when clicking an industry
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
- [ ] Terrain epic (Visual):
  - [ ] Add bumpy terrain using low-res noise texture for low-poly smooth height variation
- [ ] Toolbar icons epic (Visual):
  - Description: Current toolbar icons are placeholder quality. Generate icons from the underlying 3D models (render model to texture) for a consistent, polished look.
  - [ ] Investigate rendering models to texture for icon generation
  - [ ] Generate toolbar icons from 3D models
- [ ] Main menu improvements (Visual):
  - [x] Add FPS counter to the info bar (right subsection, left of main menu button)
  - [x] Make main menu into burger button, trigger menu in middle of game with main menu button as only option for now
  - [ ] Make a visually appealing main menu
- [ ] GUI layout previewer (Tooling):
  - [ ] Standalone viewer that renders XML layouts via the existing GUI system
  - [ ] File watcher for live reload on XML changes

---

Other milestones: [v1.0](docs/milestones/v1.0.md) · [v1.1](docs/milestones/v1.1.md) · [Done](docs/milestones/done.md)