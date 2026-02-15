

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline


## TODO

(updated 07/02/2026 [mm/dd/yyyy])

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
  - [ ] BUG: don't show building ghost when there's no terrain intersection with mouse
  - [ ] Add residence buildings from ORA data
- Deletion epic:
  - [ ] Add deletion tool
  - [ ] Allow deleting tracks
  - [ ] Allow deleting trains
  - [ ] Allow deleting vehicles
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

