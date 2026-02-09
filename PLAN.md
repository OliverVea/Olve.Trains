# Superplan: Generic Texture System

## Overview

Two-phase plan:
- **Phase 1**: Replace MemoryPack with a swappable `IAssetSerializer` (JSON for now). This unblocks generic type serialization and is a standalone improvement.
- **Phase 2**: Unify the parallel RGBA/Float texture paths into a single generic `TextureData<T>` system with compile-time type safety from asset loading through OpenGL upload.

Each phase should be completed, committed, and verified independently before starting the next.

---

# Phase 1: Serialization Abstraction

## Step 1: Serialization Abstraction

(skipped since it's not required)

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
  - Register custom `JsonConverter`s for `Vector4D<byte>`, `Vector3D<float>`, `Vector2D<float>`, `Matrix4X4<float>` etc. (Silk.NET types that System.Text.Json won't handle by default)
  - Register converter for `RGBA` struct

### Files to modify:
- `src/Olve.Engine3D/Assets/AssetLoader.cs` — inject `IAssetSerializer`, replace `MemoryPackSerializer.Deserialize<T>(bytes)` with `serializer.Deserialize<T>(bytes)`
- `src/Olve.Trains.AssetPipeline/Assets/AssetWriter.cs` — inject `IAssetSerializer`, replace `MemoryPackSerializer.SerializeAsync` with `serializer.Serialize<T>`
- DI registrations in both game and pipeline projects

### Files to clean up:
- Remove `[MemoryPackable]` from: `TextureData`, `FloatTextureData`, `TerrainData`, `HeightmapData`, `MeshData`, `LineStripData`, `TriangleIndex`
- Remove MemoryPack NuGet references if no longer used

**Note:** Re-run the asset pipeline after this step to re-serialize all assets as JSON. Existing binary `.mesh`/`.texture`/`.terrain` files will need to be regenerated.

---

# Phase 2: Generic Texture System

## Step 2: Generic TextureData<T>

Replace `TextureData` and `FloatTextureData` with a single generic `TextureData<T>`.

### Files to modify:
- `src/Olve.Engine3D/Assets/Entities/TextureData.cs` — replace with:
  ```csharp
  public class TextureData<T> : ITextureData where T : unmanaged
  {
      public required T[] Pixels { get; init; }
      public required int Width { get; init; }
      public required int Height { get; init; }

      public Result Validate() { ... } // Width * Height == Pixels.Length
  }

  /// Non-generic interface for type-erased storage in TextureManager.
  public interface ITextureData
  {
      int Width { get; }
      int Height { get; }
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
  The old `Texture(TextureData Data, AssetPath)` record is removed.

- `src/Olve.Engine3D/Rendering/Textures/TextureManager.cs`:
  - Domain-level only. No type tracking — just stores data and hands out IDs.
  - Internal storage: `Dictionary<Id, ITextureData>` — fully type-erased.
  - Public API uses typed `Id<Texture<T>>`:
    ```csharp
    public Id<Texture<T>> Register<T>(TextureData<T> data, string? name = null) where T : unmanaged
    public Id<Texture<T>> EnsureTextureLoaded<T>(AssetPath<TextureData<T>> assetPath) where T : unmanaged
    public bool TryGetTextureData<T>(Id<Texture<T>> id, out TextureData<T> data) where T : unmanaged
    ```
  - `TryGetTextureData<T>` attempts to cast `ITextureData` to `TextureData<T>`. Logs a warning and returns false if the cast fails. In practice the typed IDs guarantee the cast always succeeds.
  - Remove `ReserveExternalTextureId` (no longer needed — float textures go through the same path)

- `src/Olve.Engine3D/Rendering/Textures/TextureRenderingManager.cs`:
  - No more `RenderingId` in the public API. Callers only deal with `Id<Texture<T>>`.
  - Owns type verification: stores `Dictionary<Id, (Type PixelType, Texture2D Handle)>`.
  - Single unified registration method:
    ```csharp
    public Result Register<T, TFormat>(Id<Texture<T>> textureId, TextureUploadOptions options)
        where T : unmanaged
        where TFormat : IPixelFormat<T>
    ```
  - Stores `(typeof(T), texture2D)` by `textureId.Value`.
  - Remove `RegisterFloatTexture`, `RegisterExternalTexture`, `AdoptExternalTexture`, `EnsureTextureLoaded` — replaced by the single `Register` method.
  - Provides `TryGetTexture2D(Id id, Type expectedPixelType, out Texture2D texture)` for use by `TextureSlotManager`. Verifies that `expectedPixelType` matches the stored `Type` and logs/returns false on mismatch.

- `src/Olve.Engine3D/Rendering/Parameters/RenderingParameter.cs`:
  - Include the pixel type in the rendering parameter so the rendering manager can verify at render time:
    ```csharp
    public class Texture(string name, Id value, Type pixelType) : Base<Id>(name, value)
    {
        public Type PixelType { get; } = pixelType;
    }
    ```
  - Generated shader code passes `typeof(T)` when creating the parameter.

- `src/Olve.Engine3D/Rendering/Parameters/IShaderParameters.cs`:
  - `GetTextureIds()` returns `IReadOnlyList<Id>` (untyped) since mixed texture types can't share a single generic list.

- `src/Olve.Engine3D/Rendering/OpenGL/TextureSlotManager.cs`:
  - Receives `RenderingParameter.Texture` (which contains both `Id` and `PixelType`).
  - Passes both to `TextureRenderingManager.TryGetTexture2D(id, pixelType, out texture)` for type-verified lookup.
  - Binds the returned `Texture2D` handle to the GL texture unit.

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
  - No longer implements `IOpenGLEntityManager` (not needed since TextureEntityManager is bespoke now)

- `src/Olve.Engine3D/Rendering/EntityManagers/TextureEntityManager.cs`:
  - **Replace entirely** with a bespoke sealed class in a single file. No base class inheritance.
  - Sealed class that wraps `OpenGLTextureManager` directly:
    ```csharp
    public sealed class TextureEntityManager(OpenGLTextureManager openGLTextureManager)
    {
        private readonly Dictionary<Id, Texture2D> _textures = new();

        public Result Register<T, TFormat>(Id<Texture<T>> textureId, TextureData<T> data, TextureUploadOptions options)
            where T : unmanaged
            where TFormat : IPixelFormat<T>
        {
            if (openGLTextureManager.Register<T, TFormat>(data, options)
                .TryPickProblems(out var problems, out var texture))
                return problems;
            _textures[textureId.Value] = texture;
            return Result.Success();
        }

        public bool TryGetTexture2D(Id id, out Texture2D texture)
            => _textures.TryGetValue(id, out texture);

        public Result Unregister(Id id) { ... }
    }
    ```

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

- `src/Olve.Trains.AssetPipeline/Shaders/UniformTypeExtensions.cs`:
  - `GetDataType` changes to accept the `Uniform` (not just `UniformType`) so it can access `PixelType`:
    ```csharp
    public static string GetDataType(this Uniform uniform)
    ```
  - For `Sampler2D` with `PixelType = "RGBA"`: returns `"Id<Texture<RGBA>>"`
  - For `Sampler2D` with `PixelType = "float"`: returns `"Id<Texture<float>>"`
  - Error if `Sampler2D` uniform has no `@pixelType` annotation

- `src/Olve.Trains.AssetPipeline/Shaders/ProcessShaders.cs` — in `MapToScriptObject()`:
  - Pass `uniform` object to `GetDataType` instead of `uniform.Type`
  - Pass pixel type info to template for `RenderingParameter.Texture` construction (needs `typeof(T)`)

- `src/Olve.Trains.AssetPipeline/templates/ShaderClass.scriban`:
  - `GetTextureIds()` return type becomes `IReadOnlyList<Id>` (untyped)
  - Texture ID extraction uses `.Value` to get untyped `Id` from `Id<Texture<T>>`
  - `RenderingParameter.Texture` construction passes `typeof({pixelType})` as third argument
  - Add `using Olve.Engine3D;` for `RGBA` type access

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
  - Registration: `textureManager.Register(data)` then `textureRenderingManager.Register<RGBA, RgbaPixelFormat>(id, options)`

- `src/Olve.Trains/Scenes/Rendering/TerrainRenderingService.cs`:
  - `TextureData<float>` instead of `FloatTextureData`
  - Register through unified path:
    ```csharp
    var id = textureManager.Register(floatTextureData, "terrain_heightmap");
    textureRenderingManager.Register<float, R32fPixelFormat>(id, new TextureUploadOptions(Wrap: GLEnum.ClampToEdge));
    ```
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
- Run asset pipeline to regenerate all shader classes and texture assets
- Build and verify compilation
- Run the application to verify textures render correctly

## Key Design Decisions

1. **`Id<Texture<T>>`** carries pixel type for compile-time safety at registration and shader parameter assignment.
2. **`TextureManager` is domain-only, fully type-erased internally**: stores `Dictionary<Id, ITextureData>`. `TryGetTextureData<T>` just attempts a cast — no type tracking needed.
3. **`TextureRenderingManager` owns type verification**: stores `(Type, Texture2D)` by `Id`. Verifies pixel type matches at render time when `TextureSlotManager` resolves textures.
4. **No `RenderingId` in public API**: callers only deal with `Id<Texture<T>>`. The mapping to OpenGL handles is internal.
5. **`RenderingParameter.Texture` includes `Type pixelType`**: passed from generated shader code (`typeof(T)`), used by `TextureRenderingManager` for render-time verification.
5. **Bespoke `TextureEntityManager`**: sealed class, no base class inheritance. Single file, simple dictionary-based management.
6. **`IPixelFormat<T>`** lives in the OpenGL layer with default `MemoryMarshal` implementation. Only `RgbaPixelFormat` overrides (float→byte packing).
7. **`TextureUploadOptions`** is non-format (wrap, filter, mipmaps only). Format comes from `TFormat` type parameter.
8. **`IAssetSerializer`** interface with JSON implementation replaces MemoryPack. Swappable via DI.
9. **`@pixelType(T)` shader annotation** parsed by asset pipeline, emitted as `Id<Texture<T>>` in generated code.
