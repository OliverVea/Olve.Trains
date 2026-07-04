---
name: asset-pipeline
description: Reference for the asset pipeline — build targets, shader/layout/mesh/texture/font processing, source-art configuration, path layout, and generated code. Use when modifying shaders, layouts, adding new assets, troubleshooting pipeline issues, or when builds fail due to missing generated types or asset references.
user-invocable: false
---

# Asset Pipeline

The asset pipeline (`src/Olve.Trains.AssetPipeline/`) compiles source resources into generated C# code and binary assets consumed by the game at runtime.

## Pipeline Execution Order

```
1. ProcessLayouts ────────────────► assets/Layouts/*.cs
2. LoadLocalAssets ───────────────► reads source art from resources/assets/
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

| Target | Flag | Needs source art | Description |
|---|---|---|---|
| `Shaders` | `1 << 0` | No | GLSL shaders → generated C# classes |
| `Meshes` | `1 << 1` | Yes | 3D model files → binary mesh assets |
| `Textures` | `1 << 2` | Yes | Image files → binary texture assets |
| `Terrains` | `1 << 3` | Yes | Heightmap/terrain data → binary assets |
| `Layouts` | `1 << 4` | No | XML UI layouts → generated C# classes |
| `Fonts` | `1 << 5` | Yes | Font files → MSDF atlas + binary assets |
| `TextureAtlases` | `1 << 6` | Yes | Texture atlas definitions → packed atlases |

Configured via `Build:Targets` in appsettings (list of target name strings).

Targets in the "Needs source art" column read source files from the committed `Asset:SourceDirectory` (see [Asset Source Configuration](#asset-source-configuration)). `BuildTargets.RequiresSourceAssets()` is the flag check.

## Pipeline Flow (`RunAssetPipeline.cs`)

1. **Layouts** — process XML layout files (if `Layouts` target enabled)
2. **Source assets** — load committed source art from `Asset:SourceDirectory` (if any source-art target enabled)
3. **Shaders** — compile GLSL → C# (if `Shaders` target enabled)
4. **Assets** — process meshes, textures, terrains, fonts, texture atlases (delegates to sub-processors)

## Directory Structure

### Source directories (input)
- `src/Olve.Trains/resources/shaders/` — GLSL shader source files (`*.glsl`)
- `src/Olve.Trains/resources/layouts/` — XML layout definitions (`*.xml`)
- `src/Olve.Trains/resources/assets/` — source art: meshes (`*.fbx`), textures (`*.png`/`*.tga`), fonts (`*.ttf`), terrain (`*.ora`), atlas defs (`*.json`). Binaries are tracked via git LFS.

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

<a name="asset-source-configuration"></a>
## Asset Source Configuration (`AssetOptions`)

Source art is committed to the repo under `src/Olve.Trains/resources/assets/` and tracked via git LFS (binaries are pinned to the commit, so a checkout always has exactly the art that matches the code). `LoadLocalAssets` reads every file in that directory and hands the flat file list to the sub-processors, which filter by extension.

| Option | Default | Description |
|---|---|---|
| `SourceDirectory` | — | Directory of committed source art. Required when any source-art target is enabled. |

Configured via `Asset:SourceDirectory` — set as an absolute path in `appsettings.local.json` for local runs, and via the `Asset__SourceDirectory` env var in the CI build/test scripts (`.pipelines/scripts/`).

**LFS note:** the source binaries are LFS objects, so a plain GitHub tarball fetch yields pointer files, not real art. Anything that runs the pipeline must obtain the working tree via `git clone` + `git lfs pull` (both `build.sh` and `test.sh` do this).

## Asset Processing Sub-Processors

Each sub-processor follows the same pattern: `Request` → `ExecuteAsync` → `Result<Response>`.

| Processor | Input | Output |
|---|---|---|
| `ProcessMeshAssets` | source mesh files | Binary mesh assets + C# class |
| `ProcessTextureAssets` | source texture files | Binary texture assets + C# class |
| `ProcessTerrainAssets` | source terrain files (OpenRaster) | Binary terrain assets + C# class |
| `ProcessFonts` | source font files | MSDF atlas generation + binary assets + C# class |
| `ProcessTextureAtlasAssets` | source texture files | Packed texture atlases + C# class |

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

## Adding a New Source Asset

1. Drop the source file into `src/Olve.Trains/resources/assets/` (meshes `*.fbx`, textures `*.png`/`*.tga`, fonts `*.ttf`, terrain `*.ora`, atlas defs `*.json`).
2. Binary types are already matched by the LFS rules in `.gitattributes` (`resources/assets/**/*.{fbx,png,tga,ora,ttf}`) — `git add` stores them as LFS objects automatically. Verify with `git lfs ls-files`.
3. Run the pipeline: `cd src/Olve.Trains.AssetPipeline && dotnet run`.
4. Commit the source file **and** the regenerated `src/Olve.Trains/assets/` outputs together, so the committed art and its generated references stay in lockstep.

Because the source art is committed and pinned to the commit, a checkout always has exactly the art the code expects — there is no separate download/sync step and no stale-cache class of bugs.

## Troubleshooting

- **Build fails with missing generated types** → Run the asset pipeline first
- **Pipeline fails with "Asset source directory does not exist"** → `Asset:SourceDirectory` is unset or wrong; set it in `appsettings.local.json` (see [Asset Source Configuration](#asset-source-configuration))
- **Source art shows up as tiny text pointer files** → the working tree was fetched without LFS; run `git lfs pull`
- **Shader compile error** → Check GLSL file naming (`<name>.<frag|vert|geom>.glsl`) and uniform syntax
- **Layout parse error** → Validate XML; attribute values must be valid C# expressions
