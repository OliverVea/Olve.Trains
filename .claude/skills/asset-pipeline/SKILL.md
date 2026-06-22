---
name: asset-pipeline
description: Reference for the asset pipeline — build targets, shader/layout/mesh/texture/font processing, S3 configuration, path layout, and generated code. Use when modifying shaders, layouts, adding new assets, troubleshooting pipeline issues, or when builds fail due to missing generated types or asset references.
user-invocable: false
---

# Asset Pipeline

The asset pipeline (`src/Olve.Trains.AssetPipeline/`) compiles source resources into generated C# code and binary assets consumed by the game at runtime.

## Pipeline Execution Order

```
1. ProcessLayouts ────────────────► assets/Layouts/*.cs
2. DownloadAssets / LoadLocalAssets ► temp build dir
3. ProcessShaders ────────────────► assets/Shaders/*.cs
4. ProcessAssets
   ├─ ProcessMeshAssets ──────────► assets/Meshes/*.mesh + .cs
   ├─ ProcessTextureAssets ───────► assets/Textures/*.texture + .cs
   ├─ ProcessTerrainAssets ───────► assets/Terrains/*.terrain + .cs
   ├─ ProcessTextureAtlasAssets ──► assets/TextureAtlases/*.cs
   └─ ProcessFonts ──────────────► assets/fonts/*.texture + .cs
```

## Running the Pipeline

```bash
cd src/Olve.Trains.AssetPipeline && dotnet run
```

After modifying shaders or layouts, you **must** run the pipeline before building the game.

Configuration is loaded from (in order, later overrides earlier):
- `Properties/appsettings.json`
- `Properties/appsettings.local.json`
- User secrets
- Command line args

## Build Targets (`BuildTargets.cs`)

Flags enum controlling which asset types to process:

| Target | Flag | Requires S3 | Description |
|---|---|---|---|
| `Shaders` | `1 << 0` | No | GLSL shaders → generated C# classes |
| `Meshes` | `1 << 1` | Yes | 3D model files → binary mesh assets |
| `Textures` | `1 << 2` | Yes | Image files → binary texture assets |
| `Terrains` | `1 << 3` | Yes | Heightmap/terrain data → binary assets |
| `Layouts` | `1 << 4` | No | XML UI layouts → generated C# classes |
| `Fonts` | `1 << 5` | Yes | Font files → MSDF atlas + binary assets |
| `TextureAtlases` | `1 << 6` | Yes | Texture atlas definitions → packed atlases |

Configured via `Build:Targets` in appsettings (list of target name strings).

Targets requiring S3 will either download from S3 or load from local cache, depending on `S3:UseLocalAssets`.

## Pipeline Flow (`RunAssetPipeline.cs`)

1. **Layouts** — process XML layout files (if `Layouts` target enabled)
2. **S3 assets** — download from S3 or load local cache (if any S3-requiring target enabled)
3. **Shaders** — compile GLSL → C# (if `Shaders` target enabled)
4. **Assets** — process meshes, textures, terrains, fonts, texture atlases (delegates to sub-processors)

## Directory Structure

### Source directories (input)
- `src/Olve.Trains/resources/shaders/` — GLSL shader source files (`*.glsl`)
- `src/Olve.Trains/resources/layouts/` — XML layout definitions (`*.xml`)

### Output directories (generated)
- `src/Olve.Trains/assets/Shaders/` — generated shader C# classes
- `src/Olve.Trains/assets/Layouts/` — generated layout C# classes
- `src/Olve.Trains/assets/Meshes/` — binary mesh files + generated C# code
- `src/Olve.Trains/assets/Textures/` — binary texture files + generated C# code
- `src/Olve.Trains/assets/fonts/` — font atlas textures

Paths are configured via `PathProvider` using options from `BuildOptions`, `ShaderOptions`, `MeshOptions`, etc.

### Generated namespaces
Base namespace from `Build:BaseNamespace` (default: `"Generated"`), with per-asset suffixes:
- Shaders: `Generated.Shaders`
- Layouts: `Generated.Layouts`
- Meshes: `Generated.Meshes`
- Textures: `Generated.Textures`
- Terrains: `Generated.Terrains`
- Fonts: `Generated.Fonts`
- TextureAtlases: `Generated.TextureAtlases`

The `Meshes` and `Textures` catalogs (generated from `templates/AssetClass.scriban`) expose one `static readonly AssetPath<T>` per asset plus an `All` list (`IReadOnlyList<AssetPath<T>>`) of every entry, used for bulk operations like asset cache pre-warming.

## Shader Processing (`Shaders/ProcessShaders.cs`)

Reads GLSL files, extracts uniforms and vertex attributes, generates C# wrapper classes via Scriban templates.

### Shader file naming convention
```
<program-name>.<type>.glsl
```
- `type`: `frag` (fragment), `vert` (vertex), `geom` (geometry)
- Example: `terrain.frag.glsl`, `terrain.vert.glsl`

A shader **program** groups one fragment + one vertex + optionally one geometry shader by matching `<program-name>`.

### What gets generated per shader program
- C# class with:
  - Embedded shader source code (fragment, vertex, geometry)
  - Typed uniform properties with layout locations
  - Vertex attribute structs with stride/offset calculations
  - Instance attribute structs (for instanced rendering)
  - Interface implementations from `@implements` annotations

### Uniform parsing
Uniforms are extracted from GLSL `uniform` declarations. The pipeline reads type, name, and layout location.

### Vertex attribute parsing
Vertex attributes are extracted from vertex shader `in` declarations. Attributes annotated with `@instanced` are separated into an instance data struct.

### Template
Uses `templates/ShaderClass.scriban` to generate the C# source.

## Layout Processing (`Layouts/ProcessLayouts.cs`)

Reads XML layout files and generates C# classes that construct the UI element tree.

### XML structure
- Each XML element becomes a UI node
- `id` attribute → fixed ID (stable across runs via `Id.FromName`)
- `style` attribute → style key reference
- Other attributes → property assignments (emitted as-is, must be valid C# expressions)
- Nested elements → parent-child relationships

### Template
Uses `templates/LayoutClass.scriban` to generate the C# source.

## S3 Configuration (`S3Options`)

| Option | Default | Description |
|---|---|---|
| `Bucket` | — | S3 bucket name |
| `Prefix` | `"/"` | Object key prefix |
| `Key` | — | Access key |
| `Secret` | — | Secret key |
| `TimeoutMs` | `20000` | Download timeout (ms) |
| `AllowFailure` | `false` | Continue on S3 errors |
| `UseLocalAssets` | `false` | Skip S3, use local build dir |

Set `UseLocalAssets: true` for offline development (uses whatever is cached in the build directory). Set to `false` for full builds (required for CI and fresh environments).

## Asset Processing Sub-Processors

Each sub-processor follows the same pattern: `Request` → `ExecuteAsync` → `Result<Response>`.

| Processor | Input | Output |
|---|---|---|
| `ProcessMeshAssets` | S3 mesh files | Binary mesh assets + C# class |
| `ProcessTextureAssets` | S3 texture files | Binary texture assets + C# class |
| `ProcessTerrainAssets` | S3 terrain files (OpenRaster) | Binary terrain assets + C# class |
| `ProcessFonts` | S3 font files | MSDF atlas generation + binary assets + C# class |
| `ProcessTextureAtlasAssets` | S3 texture files | Packed texture atlases + C# class |

## Adding a New Shader

1. Create `<name>.vert.glsl` and `<name>.frag.glsl` in `src/Olve.Trains/resources/shaders/`
2. Run the asset pipeline: `cd src/Olve.Trains.AssetPipeline && dotnet run`
3. A `Shaders.<Name>.cs` class is generated in `src/Olve.Trains/assets/Shaders/`
4. Use the generated class in rendering code (provides typed uniforms, vertex layout, embedded source)

## Adding a New Layout

1. Create `<ClassName>.xml` in `src/Olve.Trains/resources/layouts/`
2. Run the asset pipeline
3. A `<ClassName>.cs` class is generated in `src/Olve.Trains/assets/Layouts/`
4. Use the generated class to build UI trees

## Stale Local Asset Cache

Asset references (generated C# code that references binary mesh/texture/terrain files) are committed to git. When another developer adds new S3 assets on their machine, they commit the generated references. After pulling those changes, **your local asset cache is stale** — the generated code references assets that don't exist locally yet.

**Symptoms:**
- Build fails with missing asset files (e.g., `FileNotFoundException` for `.mesh`, `.texture` files)
- Runtime errors about missing meshes/textures that exist in the generated C# code
- Tests fail because assets referenced in code aren't present

**Fix:** Re-run the asset pipeline with S3 download enabled:
```bash
# Ensure UseLocalAssets is false in appsettings.local.json
cd src/Olve.Trains.AssetPipeline && dotnet run
```

This downloads the new assets from S3 and regenerates any stale references. Then rebuild:
```bash
dotnet build src/Olve.Trains/Olve.Trains.csproj --configuration Release
```

**Rule of thumb:** After pulling changes that touch `src/Olve.Trains/assets/`, always re-run the asset pipeline with `UseLocalAssets: false`.

## Troubleshooting

- **Build fails with missing generated types** → Run the asset pipeline first
- **Build fails with missing asset files after `git pull`** → Local asset cache is stale; re-run pipeline with `S3:UseLocalAssets: false` (see above)
- **Missing meshes/textures** → Ensure `S3:UseLocalAssets` is `false` and S3 credentials are configured
- **Shader compile error** → Check GLSL file naming (`<name>.<frag|vert|geom>.glsl`) and uniform syntax
- **Layout parse error** → Validate XML; attribute values must be valid C# expressions
