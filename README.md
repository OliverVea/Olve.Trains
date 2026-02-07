

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline
- bun for api gen


## TODO

(updated 07/02/2026 [mm/dd/yyyy])

- Track creation epic:
  - [x] Allow ghost preview during track creation
  - [ ] Disallow tracks with collisions with geometry, other tracks, and extreme curvature
  - Display ghost previews of disallowed tracks in red
- Configuration epic:
  - [ ] Create engine-side system for managing configuration
    - all kinds of configuration: graphics, interface, audio, game options, bindings
    - bindings should allow key/mouse/other bindings
    - categorization and sub categorization. E.g. 'channel balance' should be allowed to have categorization 'audio' and sub category 'mix'. Key binds can be 'bindings' and sub category 'GUI' for 'select place track'.
  - [ ] Implement game-side configuration
    - create the specific configuration that can be set/referenced
    - create defaults for e.g. binding 
  - [ ] Allow UI source generated elements to reference key bind keys. E.g. 'SELECT_PLACE_TRACK' is looked up in config and then used to activate PlaceTrackButton.
- Industry epic:
  - [ ] Create game-side logic supporting industries
  - [ ] Create game-side logic for supporting cargo and train inventories
  - [ ] Create game-side logic for generating cargo in stations next to industries
  - [ ] Create game-side logic for consuming cargo delivered to stations
- Track rendering epic:
  - [ ] Improve rendering of tracks by procedurally generating the track mesh from the underlying spline
  - [ ] Create and render procedurally generated station meshes
  - [ ] Improve positioning of track signals based on track positioning

## FAQ

```
C:\Users\olive\.nuget\packages\microsoft.extensions.apidescription.server\9.0.8\build\Microsoft.Extensions.ApiDescription.Server.targets(68,5): error : System.AggregateException: One or more errors occurred. (Could not load the embedded file manifest 'Microsoft.Extensions.FileProviders.Embedded.Manifest.xml' for assembly 'Olve.Engine3D.DebugServer'.)
```

apigen + building the debug server kind of blocks eachother.

The solution is to create a SvelteDist directory in Olve.Engine3D.DebugServer and then put some file (e.g. `index.html`) inside.

This will allow for building Olve.Engine3D.DebugServer.Host which is required to run api generation.