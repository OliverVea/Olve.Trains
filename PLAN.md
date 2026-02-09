# Plan: Generic Texture System

## Overview

Unify the parallel RGBA/Float texture paths into a single generic `TextureData<T>` system with compile-time type safety from asset loading through OpenGL upload. Replace MemoryPack with a swappable `IAssetSerializer` (JSON for now).

## Step 1: Serialization Abstraction

Replace MemoryPack with an `IAssetSerializer` interface and JSON implementation.

### Files to create:
- `src/Olve.Engine3D/Assets/IAssetSerializer.cs`
  ```csharp
  public interface IAssetSerializer
  {
      byte[] Serialize<T>(T value);
      T? Deserialize<T>(byte[] data);
  }
  ```
- `src/Olve.Engine3D/Assets/JsonAssetSerializer.cs` — implementation using `System.Text.Json`
  - Register a custom `JsonConverter` for `Vector4D<byte>`, `Vector3D<float>`, `Vector2D<float>`, `Matrix4X4<float>` etc. (Silk.NET types that System.Text.Json won't handle by default)
  - Register converter for `RGBA` struct

### Files to modify:
- `src/Olve.Engine3D/Assets/AssetLoader.cs` — inject `IAssetSerializer`, replace `MemoryPackSerializer.Deserialize<T>(bytes)` with `serializer.Deserialize<T>(bytes)`
- `src/Olve.Trains.AssetPipeline/Assets/AssetWriter.cs` — inject `IAssetSerializer`, replace `MemoryPackSerializer.SerializeAsync` with `serializer.Serialize<T>`
- DI registrations in both game and pipeline projects

### Files to clean up:
- Remove `[MemoryPackable]` from: `TextureData`, `FloatTextureData`, `TerrainData`, `HeightmapData`, `MeshData`, `LineStripData`, `TriangleIndex`
- Remove MemoryPack NuGet references if no longer used

**Note:** Re-run the asset pipeline after this step to re-serialize all assets as JSON. Existing binary `.mesh`/`.texture`/`.terrain` files will need to be regenerated.

## Step 2: Generic TextureData<T>

Replace `TextureData` and `FloatTextureData` with a single generic `TextureData<T>`.

### Files to create:
- None (modify existing)

### Files to modify:
- `src/Olve.Engine3D/Assets/Entities/TextureData.cs` — replace with:
  ```csharp
  public class TextureData<T> where T : unmanaged
  {
      public required T[] Pixels { get; init; }
      public required int Width { get; init; }
      public required int Height { get; init; }

      public Result Validate() { ... } // Width * Height == Pixels.Length
  }
  ```

### Files to delete:
- `src/Olve.Engine3D/Assets/Entities/FloatTextureData.cs`

### Files to update (type references):
- `src/Olve.Trains.AssetPipeline/Assets/TextureFileReader.cs` — `TextureData` → `TextureData<RGBA>`, change `Vector4D<byte>` pixel creation to `new RGBA(r/255f, g/255f, b/255f, a/255f)`
- `src/Olve.Trains.AssetPipeline/Assets/ProcessTextureAssets.cs` — update `Asset<TextureData>` → `Asset<TextureData<RGBA>>`
- `src/Olve.Trains.AssetPipeline/templates/AssetClass.scriban` — the `AssetType` for textures becomes `TextureData<RGBA>`
- `src/Olve.Trains/Scenes/Rendering/TerrainRenderingService.cs` — `FloatTextureData` → `TextureData<float>`

## Step 3: Texture<T> Marker Type and Id<Texture<T>>

Replace the non-generic `Texture` domain type with a generic `Texture<T>` marker.

### Files to modify:
- `src/Olve.Engine3D/Rendering/Textures/Texture.cs` — replace with:
  ```csharp
  /// Marker type for typed texture IDs.
  public readonly record struct Texture<T> where T : unmanaged;
  ```
  The old `Texture(TextureData Data, AssetPath)` record is removed — the `TextureManager` stores `ITextureData` internally.

- `src/Olve.Engine3D/Rendering/Textures/TextureManager.cs`:
  - Internal storage: `Dictionary<Id<Texture<T>>, ...>` won't work for mixed T. Instead, use a non-generic `TextureId` wrapper internally, or store as `Dictionary<Id, ITextureData>` and provide generic accessor methods.
  - Actually: use `Id` (untyped) internally for storage, but expose typed `Id<Texture<T>>` at the API boundary:
    ```csharp
    public Id<Texture<T>> Register<T>(TextureData<T> data, string? name = null) where T : unmanaged
    public Id<Texture<T>> EnsureTextureLoaded<T>(AssetPath<TextureData<T>> assetPath) where T : unmanaged
    public bool TryGetTextureData<T>(Id<Texture<T>> id, out TextureData<T> data) where T : unmanaged
    ```
  - Remove `ReserveExternalTextureId` (no longer needed — float textures go through the same path)

- `src/Olve.Engine3D/Rendering/Textures/TextureRenderingManager.cs`:
  - `EnsureTextureLoaded<T>(Id<Texture<T>>)` → `RenderingId<Texture<T>>`
  - Remove `RegisterFloatTexture` method
  - Callers provide `TextureUploadOptions` when registering for rendering

- `src/Olve.Engine3D/Rendering/Parameters/RenderingParameter.cs`:
  - `Texture` class uses `Id<Textures.Texture>` — this needs to become type-erased at this boundary since `AnyRenderingParameter` can't be generic. Keep `RenderingParameter.Texture(string name, Id value)` using untyped `Id` internally, with implicit conversion from `Id<Texture<T>>`.

- `src/Olve.Engine3D/Rendering/Parameters/IShaderParameters.cs`:
  - `GetTextureIds()` returns `IReadOnlyList<Id>` (untyped) since the slot manager just needs handles at bind time.

- `src/Olve.Engine3D/Rendering/OpenGL/TextureSlotManager.cs`:
  - Works with untyped `Id` internally (from `RenderingParameter.Texture.Value`), resolves to GL handle. No change to binding logic.

## Step 4: IPixelFormat<T> and TextureUploadOptions

Add the type-safe pixel format system in the OpenGL layer.

### Files to create:
- `src/Olve.Engine3D/Rendering/OpenGL/IPixelFormat.cs`:
  ```csharp
  public interface IPixelFormat<T> where T : unmanaged
  {
      static abstract InternalFormat InternalFormat { get; }
      static abstract PixelFormat PixelFormat { get; }
      static abstract PixelType PixelType { get; }
      static virtual int BytesPerPixel => Unsafe.SizeOf<T>();
      static virtual void WriteBytes(ReadOnlySpan<T> pixels, Span<byte> destination)
      {
          MemoryMarshal.AsBytes(pixels).CopyTo(destination);
      }
  }
  ```
- `src/Olve.Engine3D/Rendering/OpenGL/PixelFormats/RgbaPixelFormat.cs`:
  ```csharp
  public readonly struct RgbaPixelFormat : IPixelFormat<RGBA>
  {
      // InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte
      // BytesPerPixel = 4
      // WriteBytes packs RGBA floats → 4 bytes via RGBA.ToBytes()
  }
  ```
- `src/Olve.Engine3D/Rendering/OpenGL/PixelFormats/R32fPixelFormat.cs`:
  ```csharp
  public readonly struct R32fPixelFormat : IPixelFormat<float>
  {
      // InternalFormat.R32f, PixelFormat.Red, PixelType.Float
      // Uses default BytesPerPixel (4) and default WriteBytes (memcopy)
  }
  ```
- `src/Olve.Engine3D/Rendering/OpenGL/TextureUploadOptions.cs`:
  ```csharp
  public record TextureUploadOptions(
      GLEnum Wrap = GLEnum.Repeat,
      GLEnum Filter = GLEnum.Nearest,
      bool GenerateMipmaps = false
  );
  ```

### Files to modify:
- `src/Olve.Engine3D/Rendering/OpenGL/OpenGLTextureManager.cs`:
  - Replace `Register(TextureData)` and `RegisterFloat(FloatTextureData)` with single:
    ```csharp
    public Result<Texture2D> Register<T, TFormat>(TextureData<T> data, TextureUploadOptions options)
        where T : unmanaged
        where TFormat : IPixelFormat<T>
    ```
  - Uses `TFormat.WriteBytes`, `TFormat.InternalFormat`, etc.
  - Applies `options.Wrap`, `options.Filter`, `options.GenerateMipmaps`
  - Remove `BytesPerPixel` constant and `Nearest`/`Repeat` statics

- `src/Olve.Engine3D/Rendering/EntityManagers/TextureEntityManager.cs`:
  - Replace `RegisterInOpenGL(Texture)` and `RegisterFloat(FloatTextureData)` with:
    ```csharp
    public Result<RenderingId<Texture<T>>> Register<T, TFormat>(TextureData<T> data, TextureUploadOptions options)
        where T : unmanaged
        where TFormat : IPixelFormat<T>
    ```
  - May need to adjust base class usage since `RenderingEntityManagerBase<Texture, Registration>` is typed to non-generic `Texture`.

## Step 5: Shader Annotation and Code Generation

Add `@pixelType` annotation support to the asset pipeline.

### Files to modify:
- `src/Olve.Trains.AssetPipeline/Shaders/Uniform.cs` — add:
  ```csharp
  public string? PixelType { get; set; } // e.g. "RGBA", "float"
  ```

- `src/Olve.Trains.AssetPipeline/Shaders/ShaderHelper.cs` — in `GetUniforms()`:
  - After parsing the uniform name, scan the rest of the line for `// @pixelType(...)`
  - Extract the type string and set `uniform.PixelType`
  - Same pattern as existing `@instanced` annotation parsing

- `src/Olve.Trains.AssetPipeline/Shaders/UniformTypeExtensions.cs` — change `GetDataType()`:
  - `Sampler2D` now needs the pixel type context. Change signature or add overload:
    ```csharp
    public static string GetDataType(this Uniform uniform)
    ```
  - For `Sampler2D`: returns `"Id<Texture<{pixelType}>>"` (e.g. `"Id<Texture<RGBA>>"`, `"Id<Texture<float>>"`)
  - Error/warning if `Sampler2D` uniform has no `@pixelType` annotation

- `src/Olve.Trains.AssetPipeline/Shaders/ProcessShaders.cs` — in `MapToScriptObject()`:
  - Pass `uniform` object (not just `uniform.Type`) to `GetDataType`
  - Pass pixel type info to template

- `src/Olve.Trains.AssetPipeline/templates/ShaderClass.scriban`:
  - `GetTextureIds()` return type becomes `IReadOnlyList<Id>` (untyped) since mixed texture types can't share a single generic list
  - Add `using Olve.Engine3D;` for `RGBA` type access
  - The texture ID extraction uses `.Value` to get untyped `Id` from `Id<Texture<T>>`

### Shader files to annotate:
- `src/Olve.Trains/resources/shaders/texturedRectangle.frag.glsl`: `uniform sampler2D uTexture; // @pixelType(RGBA)`
- `src/Olve.Trains/resources/shaders/terrain.vert.glsl`: `uniform sampler2D heightMap; // @pixelType(float)`
- `src/Olve.Trains/resources/shaders/terrainWireframe.vert.glsl`: `uniform sampler2D heightMap; // @pixelType(float)`
- `src/Olve.Trains/resources/shaders/default.frag.glsl`: `uniform sampler2D textureSampler; // @pixelType(RGBA)`
- `src/Olve.Trains/resources/shaders/msdfText.frag.glsl`: `uniform sampler2D uFontAtlas; // @pixelType(RGBA)`

## Step 6: Update Callers

Update all texture usage sites to the new generic API.

### Files to modify:
- `src/Olve.Engine3D/Assets/TextureLoadingService.cs`:
  - `LoadTexture(AssetPath<TextureData<RGBA>>)` → returns `Id<Texture<RGBA>>`
  - Fallback white pixel uses `TextureData<RGBA>` with `new RGBA(1f, 1f, 1f, 1f)`
  - Registration includes `TextureUploadOptions` (Repeat, Nearest, mipmaps=true)

- `src/Olve.Trains/Scenes/Rendering/TerrainRenderingService.cs`:
  - `TextureData<float>` instead of `FloatTextureData`
  - Register through unified path with `TextureUploadOptions(ClampToEdge, Nearest, mipmaps=false)`
  - `_terrainShader.HeightMap` is now `Id<Texture<float>>`

- `src/Olve.Trains/Scenes/Rendering/VehicleRenderingService.cs` — `Id<Texture<RGBA>>`
- `src/Olve.Trains/Scenes/Rendering/JunctionSignalRenderingService.cs` — `Id<Texture<RGBA>>`
- `src/Olve.Trains/Scenes/UI/Indicators/TrackArrowIndicatorService.cs` — `Id<Texture<RGBA>>`
- `src/Olve.Trains/Scenes/UI/GUI/GuiRectangleRenderingService.cs` — `Id<Texture<RGBA>>`
- `src/Olve.Trains/Scenes/UI/GUI/GuiRectangleUpdateService.cs` — update texture loading calls
- `src/Olve.Trains/Scenes/UI/GUI/GuiTextRenderingService.cs` — `Id<Texture<RGBA>>` for font atlas
- `src/Olve.Trains/Scenes/UI/GUI/GuiTextUpdateService.cs` — update texture loading calls
- `src/Olve.Engine3D/GUI/Elements/Image.cs` — `AssetPath<TextureData<RGBA>>`
- `src/Olve.Engine3D/GUI/Elements/IRenderableAsRectangle.cs` — `AssetPath<TextureData<RGBA>>`
- `src/Olve.Engine3D/Assets/Entities/FontAtlasData.cs` — `AssetPath<TextureData<RGBA>>`
- `src/Olve.Engine3D/Rendering/RenderingServiceHelper.cs` — update `LoadTexture` signature
- `src/Olve.Trains/assets/Textures/Textures.cs` — regenerated by pipeline (becomes `AssetPath<TextureData<RGBA>>`)
- `src/Olve.Trains.AssetPipeline/templates/FontClass.scriban` — `AssetPath<TextureData<RGBA>>`

## Step 7: Clean Up

- Delete proof of concept: `src/Olve.Trains/TextureProofOfConcept.cs`
- Delete old PoC location if still exists: `src/Olve.Engine3D/Rendering/OpenGL/TextureProofOfConcept.cs`
- Remove `TextureUploadOptions2`, `TextureData2`, etc. from proof of concept
- Run asset pipeline to regenerate all shader classes and texture assets
- Build and verify compilation
- Run the application to verify textures render correctly

## Key Design Decisions

1. **`Id<Texture<T>>`** carries pixel type for compile-time safety at registration and shader parameter assignment
2. **Type erasure at rendering parameter boundary**: `RenderingParameter.Texture` stores untyped `Id` internally since `AnyRenderingParameter` union can't be generic. `GetTextureIds()` returns `IReadOnlyList<Id>`.
3. **`IPixelFormat<T>`** lives in the OpenGL layer with default `MemoryMarshal` implementation. Only `RgbaPixelFormat` overrides (float→byte packing).
4. **`TextureUploadOptions`** is non-format (wrap, filter, mipmaps only). Format comes from `TFormat` type parameter.
5. **`IAssetSerializer`** interface with JSON implementation replaces MemoryPack. Swappable via DI.
6. **`@pixelType(T)` shader annotation** parsed by asset pipeline, emitted as `Id<Texture<T>>` in generated code.
