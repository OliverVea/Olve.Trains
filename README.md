

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

(updated 02/16/2026 [mm/dd/yyyy])

- [x] Track creation epic:
  - [x] Allow ghost preview during track creation
  - [x] Disallow tracks with collisions with geometry, other tracks, and extreme curvature
  - [x] Display ghost previews of disallowed tracks in red
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
  - [ ] Add building validation and validation failure rendering
  - [ ] Validate collisions
  - [ ] Add station with station track
  - [x] BUG: don't show building ghost when there's no terrain intersection with mouse
  - [ ] Add residence buildings from ORA data
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
- Building upgrading epic:
  - [ ] Add basic support for building upgrades
  - [ ] Require resources for building upgrades
- Track rendering epic:
  - [ ] Improve rendering of tracks by procedurally generating the track mesh from the underlying spline
  - [ ] Create and render procedurally generated station meshes
  - [ ] Improve positioning of track signals based on track positioning
- Detached mode:
  - Description: For e.g. Agentic AI access to the game for debugging, we want to be able to run with --detached or -d. Then, we should be able to use the cli to interact with the game, enumerating options (for main menu it could be clicking buttons, for the game it could be listing and placing tracks, buildings, trains). We should also be able to take screenshots of the screen as .pngs.
  - [x] Launch game in detached mode
  - [x] Add argument to send command to detached game by id
  - [x] Add a simple echo command
  - [x] Add commands for navigating the main menu
  - [x] Add support for taking screenshots of the current state of the game
- Main menu improvements:
  - [x] Add FPS counter to the info bar (right subsection, left of main menu button)
  - [ ] Make main menu into burger button, trigger menu in middle of game with main menu button as only option for now