# Render Pass Prototype — Build Instructions

## Goal

Create a compilable prototype in this project (`Olve.Trains.RenderPassPrototype`) that validates the render pass & framebuffer design against the real engine/game types. It references `Olve.Engine3D` and `Olve.Trains` — reuse their types directly wherever unchanged. Only write prototype versions for new or changed types.

**It must build. It does not need to run.**

## Reference Files

- **Design doc**: `~/sandbox/frame-format-prototype/DESIGN.md` — full design with scene ownership, render loop, resize, etc.
- **Toy prototype (compiles & runs)**: `~/sandbox/frame-format-prototype/Types.cs` + `Program.cs` — standalone proof of the type system.

## What to Build

### New types (write these in the prototype)

1. **Pixel format types**: `IPixelFormat`, `Depth`, `Stencil`, `DepthStencil` (RGBA already exists in engine)
2. **Frame format interfaces**: `IFrameFormat`, `IFrameFormat<T0>`, ..., `IFrameFormat<T0..T7>` — up to 8 color attachments
3. **Game-defined formats**: `IDefaultFrameFormat : IFrameFormat<RGBA>`, `IDepthFrameFormat : IFrameFormat`
4. **`TextureId<TPixel>`** — unified GPU texture handle (typed by pixel format)
5. **`FramebufferId<TFormat>`** — phantom-typed framebuffer handle
6. **`FramebufferManager`** — creates framebuffers + backing textures. Methods:
   - `Create` / `CreateWithDepth` / `CreateWithStencil` / `CreateWithDepthStencil`
   - Each has overloads for 0–8 color attachments (mechanical, all the same pattern)
   - Returns `(FramebufferId<TFormat>, TextureId<T0>, ..., TextureId<Depth|Stencil|DepthStencil>)` tuples
   - `Resize<TFormat>(FramebufferId<TFormat>, int w, int h)` — recreates GPU textures, TextureIds stay stable
7. **`RenderPassId<TFormat>`** — phantom-typed pass handle
8. **`RenderPassManager`** — `Create(FramebufferId<TFormat>, int priority, ClearFlags)`
9. **`ClearFlags`** — `[Flags] enum { None, Color, Depth, Stencil, ColorDepth, All }`
10. **`ScreenPass`** — `SetSource<TPixel>(TextureId<TPixel>)` — blits to FBO 0
11. **`IShader<TFormat>`** — shader interface parameterized by frame format
12. **Concrete shaders** — `DefaultShader : IShader<IDefaultFrameFormat>`, `DepthOnlyShader : IShader<IDepthFrameFormat>` — hardcode shader data, no asset pipeline
13. **Updated `GroupManager.Register`** — single method with `TFormat` shared between `IShader<TFormat>` and `RenderPassId<TFormat>`

### Reuse from engine/game (do NOT rewrite)

- `Id<T>` from `Olve.Utilities`
- `Result`, `ResultProblem` from `Olve.Results`
- Existing scene infrastructure, DI
- `RGBA` pixel type (if it exists in engine)
- Any geometry/vertex types

### Scene services (write as demonstration code in Program.cs)

Show the scene ownership pattern with placeholder bodies:

```csharp
// SharedRenderingService — owns output framebuffer + screen blit
//   Load(): create main FB, wire screen.SetSource(mainColor)
//   Expose MainFb for other scenes

// GameSceneRenderService — depends on SharedRenderingService
//   Load(): create shadow map FB + depth pass, create main pass into shared.MainFb
//   Register geometry groups

// GuiSceneRenderService — depends on SharedRenderingService (NOT game scene)
//   Load(): create GUI pass into shared.MainFb at higher priority
//   Register UI groups
```

### Compile-time safety checks

Include commented-out lines that would fail to compile:
```csharp
// groupManager.Register(geometry, new DepthOnlyShader(), mainPass);  // ✗ format mismatch
```

## Key Design Decisions

- **No backwards compatibility** — old untyped Register goes away
- **Game never calls render** — services do Load() and Update() only, engine owns render loop
- **Passes are per-scene** — created on Load, torn down on Unload
- **SharedRenderingService owns the output FB** — game and GUI are siblings, neither depends on the other
- **TextureId is the unified handle** — both asset textures and framebuffer attachments use the same type
- **FramebufferManager methods are mechanical** — 4 depth modes × 9 arities = 36 methods, all the same pattern

## Build Command

```bash
cd ~/sandbox/olve-trains-v2-prototype
dotnet build src/Olve.Trains.RenderPassPrototype/Olve.Trains.RenderPassPrototype.csproj
```
