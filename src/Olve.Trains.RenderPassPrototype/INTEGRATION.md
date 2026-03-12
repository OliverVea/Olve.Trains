# Integration Guide — Render Pass Prototype → Real Engine

## Prototype → Engine File Mapping

| Prototype file | Engine target | Action |
|---|---|---|
| `FrameFormats.cs` | `Olve.Engine3D/Rendering/FrameFormats.cs` | New file |
| `PixelFormats.cs` | `Olve.Engine3D/Rendering/Textures/PixelFormats.cs` | New file (`Depth`, `DepthStencil`) |
| `ITypedShader.cs` | `Olve.Engine3D/Rendering/Shaders/IShader.cs` | Extend existing `IShader` with `IShader<TFormat>` |
| `FramebufferManager.cs` | `Olve.Engine3D/Rendering/FramebufferManager.cs` | New file — add GL FBO/texture allocation |
| `RenderPassManager.cs` | `Olve.Engine3D/Rendering/RenderPassManager.cs` | New file |
| `ScreenPass.cs` | `Olve.Engine3D/Rendering/ScreenPass.cs` | New file — add fullscreen quad blit |
| `RenderingGroupManager.cs` | `Olve.Engine3D/Rendering/Instancing/RenderingGroupManager.cs` | Modify — add `pass` param to Register |
| `RenderingManagerSketch.cs` | `Olve.Engine3D/Rendering/RenderingManager.cs` | Modify — nested pass→group loop |
| `RenderStateExtensions.cs` | `Olve.Engine3D/Rendering/RenderStateExtensions.cs` | New file (extract from current RenderingManager) |

## Key Decisions Made During Prototyping

- **Type safety is at registration only.** Storage uses untyped `Id`/`IShader` — the render loop iterates heterogeneous passes.
- **No cascading deletes in managers.** Real engine should use events: `FramebufferDestroyed → passes destroyed → groups destroyed → instances destroyed`.
- **`RenderPassInfo`/`GroupInfo` are prototype-only.** Real `RenderingManager` has internal access to `GroupData` — no public DTOs needed.
- **Create methods return `Result<T>`.** Follows codebase convention. Dimensions validated.
- **No stencil variants.** Only `Create` and `CreateWithDepth`, up to 3 color attachments.

## Migration Steps

### 1. Add new types to Olve.Engine3D
Add `IFrameFormat` hierarchy, `Depth`/`DepthStencil` pixel types, `IShader<TFormat>`. These have no dependencies — safe to add without breaking anything.

### 2. Update shader source generator
Every generated shader must implement `IShader<TFormat>` instead of `IShader`. The GLSL source generator needs a `@frameFormat` annotation (or convention) to determine which `TFormat` each shader targets. This is the biggest piece of work.

### 3. Add FramebufferManager with GL backing
Prototype is pure bookkeeping. Real impl needs:
- `Id → GLuint FBO` mapping
- `glGenFramebuffers`, `glFramebufferTexture2D` for color/depth attachments
- `glGenTextures` with appropriate internal formats per pixel type
- `Resize` must `glDeleteTextures` + `glGenTextures` at new size while keeping `TextureId` stable

### 4. Add RenderPassManager + ScreenPass
These are close to production-ready from the prototype. ScreenPass needs a fullscreen quad shader + blit implementation.

### 5. Modify RenderingGroupManager.Register
Add `Id<RenderPass<TFormat>> pass` parameter. Every existing call site must pass a render pass. Maintain `_groupsByPass` index.

### 6. Modify RenderingManager.RenderAll
Flat `foreach (group in SortedGroups)` becomes:
```csharp
foreach (var pass in passManager.GetOrderedPasses())
{
    BindFramebuffer(pass.FramebufferId);
    pass.Clear.Apply(gl);
    foreach (var group in groupManager.GetGroupsForPass(pass.PassId))
    {
        // identical inner loop
    }
}
screenPass.Blit(gl);
```

### 7. Update scene services
Each scene service that calls `Register` needs a render pass:
- `SharedRenderingService` (new) — creates main FB, wires `ScreenPass.SetSource`
- `MeshRenderingService` — registers into a 3D pass from `SharedRenderingService.MainFb`
- `TerrainRenderingService` — same
- GUI services — register into a GUI pass (higher priority, `ClearFlags.None`)
- Shadow-casting services — create their own depth-only FB + pass

### 8. Register new managers in DI
`FramebufferManager`, `RenderPassManager`, `ScreenPass` as singletons in `SceneServiceRegistration`.
