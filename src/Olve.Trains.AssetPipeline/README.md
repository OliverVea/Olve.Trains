
## Usage

Create a `.env` file at [`<repo root>/src/Olve.Engine3D.AssetPipeline`](./.env) in the root of the project with the following content:

```env
S3_URL=http://minio.lan:9000
S3_BUCKET=olve.trains
S3_KEY=FcAifn12IemoMatmJkJ0
S3_SECRET=PichNjFNXZfWc6zmCwpNr74b09DQTL1IMELbePA2
```

Run [`build.sh`](./build.sh) to build the assets.

Run `dotnet build` in `Olve.Trains` to build the game.

## Todo

- [ ] Implement asset pipeline
  - [x] Load assets from minio S3 bucket
  - [ ] Process shaders with shader slang
  - [ ] Process assets with Silk.NET.Assimp
  - [ ] Write metadata source files
- [ ] github actions
  - [ ] Connect to tailnet + set end node
  - [ ] Run Asset Pipeline
  - [ ] (run tests)
  - [ ] Build project
  - [ ] Push assets to minio S3 bucket
