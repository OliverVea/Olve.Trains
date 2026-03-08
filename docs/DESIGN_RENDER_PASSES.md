# Render Pass System

## Problem

`RenderingManager.RenderAll()` iterates all groups in a single flat sorted list and renders them to the default framebuffer. There is no support for custom framebuffer objects (FBOs) or multi-pass rendering. Features like shadow mapping require rendering the scene multiple times to different targets with different shaders.

## Design

### Core concepts

- **Framebuffer** — a render target with color and/or depth attachments. The engine manages their lifecycle.
- **Render pass** — a named rendering step targeting a framebuffer. Game code defines which passes exist and in what order.
- **Color output type** — a game-defined interface describing the color attachments of a framebuffer. Shaders annotate which output type they write. The compiler enforces that shaders and passes are compatible.

Depth and stencil are framebuffer-level configuration, not part of the color output type — they are written implicitly by the rasterizer, not declared in shader outputs.

### Color output types

Game code defines interfaces describing what a framebuffer's color attachments look like. Shaders annotate their `out` declarations to declare compatibility.

```csharp
// Game defines these — engine has no built-in output types
public interface IMainColor
{
    RGBA Color { get; }
}

// Future: MRT for deferred rendering
public interface IGBufferColor
{
    RGBA Albedo { get; }
    RGB Normal { get; }
    float Roughness { get; }
}
```

Shaders declare compatibility via annotations on their fragment outputs:

```glsl
// default.frag
// @output(IMainColor.Color)
layout(location = 0) out vec4 fragColor;

// building.frag — same annotation, compatible with same passes
// @output(IMainColor.Color)
layout(location = 0) out vec4 fragColor;
```

The pipeline generates `IShader<IMainColor>` on the shader class. Multiple shaders implementing the same output type can coexist in the same pass.

Depth-only shaders (e.g. shadow map) have no `@output` annotations and no color output type — they implement `IShader` (non-generic).

### FramebufferManager (engine)

Manages FBO lifecycle. The engine pre-registers the default framebuffer (GL framebuffer 0) at startup.

```csharp
public class FramebufferManager
{
    // Create a custom FBO with color attachments typed by TColor
    Result<FramebufferRegistration<TColor>> Create<TColor>(FramebufferOptions options);

    // Create a depth-only FBO (no color attachments)
    Result<FramebufferRegistration> CreateDepthOnly(FramebufferOptions options);

    // Resize an existing FBO (e.g. shadow quality change)
    Result Resize(Id<Framebuffer> id, int width, int height);

    DeletionResult Destroy(Id<Framebuffer> id);
}

public class FramebufferOptions
{
    // Size mode: fixed or window-tracking
    FramebufferSizeMode SizeMode { get; init; } = FramebufferSizeMode.Fixed;

    // Required when SizeMode = Fixed, ignored when MatchWindow
    int? Width { get; init; }
    int? Height { get; init; }

    // Depth/stencil — framebuffer-level concerns, not shader concerns
    bool Depth { get; init; }
    bool Stencil { get; init; }
}

public enum FramebufferSizeMode
{
    Fixed,        // explicit width/height, does not auto-resize
    MatchWindow,  // tracks window size, auto-resizes on window resize
}
```

`FramebufferRegistration<TColor>` returns typed texture IDs for sampling in later passes:

```csharp
public class FramebufferRegistration<TColor>
{
    Id<Framebuffer> Id { get; }
    TextureId<TColor> ColorTexture { get; }    // typed by color output
    TextureId<float>? DepthTexture { get; }    // present if Depth = true
}

// Depth-only variant (no color type parameter)
public class FramebufferRegistration
{
    Id<Framebuffer> Id { get; }
    TextureId<float> DepthTexture { get; }
}
```

#### Default framebuffer

The engine wraps GL framebuffer 0 as a well-known `Id<Framebuffer>`:

```csharp
public static class Framebuffers
{
    public static readonly Id<Framebuffer> Default = Id.FromName<Framebuffer>("default");
}
```

Pre-registered at startup. Always matches window size. Game code uses it like any other framebuffer.

#### Window resize handling

- **`MatchWindow` FBOs** — the `FramebufferManager` listens to window resize events and recreates the underlying GPU textures. The `Id<Framebuffer>` and `TextureId<T>` handles remain stable.
- **`Fixed` FBOs** — unaffected by window resize. Can be resized explicitly via `Resize()`.
- **Default framebuffer** — OpenGL handles its resize automatically.

Fullscreen, borderless, and windowed mode changes are just resize events from the FBO's perspective.

### RenderPassManager (engine)

Central registry for render passes. Owns the per-frame execution order.

```csharp
public class RenderPassManager
{
    // Typed pass — requires shaders implementing IShader<TColor>
    Result<Id<RenderPass<TColor>>> Register<TColor>(RenderPassOptions<TColor> options);

    // Untyped pass — for depth-only rendering (no color output)
    Result<Id<RenderPass>> Register(RenderPassOptions options);

    DeletionResult Deregister(Id<RenderPass> passId);
}

public class RenderPassOptions<TColor>
{
    required string Name { get; init; }
    required int Order { get; init; }             // lower = runs first
    required Id<Framebuffer> Framebuffer { get; init; }
    ClearBufferMask? ClearFlags { get; init; }    // null = no clear
    Color? ClearColor { get; init; }
}

public class RenderPassOptions
{
    required string Name { get; init; }
    required int Order { get; init; }
    required Id<Framebuffer> Framebuffer { get; init; }
    ClearBufferMask? ClearFlags { get; init; }
}
```

### Group registration

`RenderingGroupManager.Register` requires a pass. No implicit default — all groups must specify which pass they belong to.

```csharp
// Typed pass — shader must implement IShader<TColor>
public Result<GroupId<TInstance>> Register<TVertex, TInstance, TColor>(
    GeometryId<TVertex> geometryId,
    IShader<TColor> shader,
    RenderState renderState,
    Id<RenderPass<TColor>> pass,
    PrimitiveType primitiveType = PrimitiveType.Triangles,
    int sortKey = 0,
    IShaderParameters? groupParameters = null)
    where TVertex : IVertexData
    where TInstance : IInstanceData<TVertex>;

// Untyped pass (depth-only) — any shader works
public Result<GroupId<TInstance>> Register<TVertex, TInstance>(
    GeometryId<TVertex> geometryId,
    IShader shader,
    RenderState renderState,
    Id<RenderPass> pass,
    PrimitiveType primitiveType = PrimitiveType.Triangles,
    int sortKey = 0,
    IShaderParameters? groupParameters = null)
    where TVertex : IVertexData
    where TInstance : IInstanceData<TVertex>;
```

The compiler enforces: a shader implementing `IShader<IMainColor>` can only be registered into an `Id<RenderPass<IMainColor>>` pass.

### RenderingManager changes

`RenderAll()` iterates passes in `Order`, then groups within each pass in `SortKey` order:

```csharp
public Result RenderAll()
{
    foreach (var pass in renderPassManager.SortedPasses)
    {
        BindFramebuffer(pass.Framebuffer);
        SetViewport(pass.ViewportSize);
        Clear(pass.ClearFlags, pass.ClearColor);

        foreach (var group in GroupsForPass(pass.Id))
        {
            // ... existing draw logic unchanged ...
        }
    }
}
```

## Consumer example: shadow mapping

```csharp
public class ShadowService : ISceneService
{
    public void Initialize()
    {
        // 1. Create depth-only FBO for shadow map
        var shadowFbo = framebufferManager.CreateDepthOnly(new FramebufferOptions
        {
            SizeMode = FramebufferSizeMode.Fixed,
            Width = 2048, Height = 2048,
            Depth = true,
        });

        // 2. Create main FBO (or use Framebuffers.Default to render directly to screen)
        var mainFramebuffer = Framebuffers.Default;

        // 3. Register passes
        var shadowPass = renderPassManager.Register(new RenderPassOptions
        {
            Name = "Shadow",
            Order = 0,
            Framebuffer = shadowFbo.Id,
            ClearFlags = ClearBufferMask.DepthBufferBit,
        });

        var mainPass = renderPassManager.Register<IMainColor>(new RenderPassOptions<IMainColor>
        {
            Name = "Main",
            Order = 100,
            Framebuffer = mainFramebuffer,
            ClearFlags = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit,
            ClearColor = Color.CornflowerBlue,
        });

        // 4. Register groups into passes
        // Shadow pass: depth-only shader, no color output
        groupManager.Register<Vertex, Instance>(
            buildingGeometry, depthOnlyShader, RenderState.Opaque,
            pass: shadowPass);

        // Main pass: lit shader implementing IShader<IMainColor>
        groupManager.Register<Vertex, Instance, IMainColor>(
            buildingGeometry, litShader, RenderState.Opaque,
            pass: mainPass);

        // 5. Wire shadow map into lit shaders
        litShader.ShadowMap = shadowFbo.DepthTexture;  // TextureId<float>
    }
}
```

## Multi-pass groups

A mesh that appears in multiple passes needs one group registration per pass with appropriate shaders:

```csharp
// Shadow pass: depth-only shader
groupManager.Register<Vertex, Instance>(
    geometry, depthShader, RenderState.Opaque,
    pass: shadowPass);

// Main pass: full lit shader
groupManager.Register<Vertex, Instance, IMainColor>(
    geometry, litShader, RenderState.Opaque,
    pass: mainPass);
```

Same geometry, different shaders, different groups. Instance data is managed independently per group.

## Type safety chain

```
Shader @output annotations
    → pipeline generates IShader<TColor> implementation
        → group registration requires IShader<TColor> to match Id<RenderPass<TColor>>
            → pass creation requires Id<Framebuffer> with matching color type
                → framebuffer creation defines the actual GPU attachments
```

Compiler catches mismatches. A shader annotated with `@output(IMainColor.Color)` cannot be registered into a pass typed as `IGBufferColor`.

Depth-only shaders have no `@output` annotations, implement non-generic `IShader`, and register into non-generic `Id<RenderPass>` passes. Any shader can go into a depth-only pass.

## What the engine provides vs. the game

| Concern | Engine | Game |
|---------|--------|------|
| FBO lifecycle (create, bind, resize, destroy) | x | |
| Default framebuffer (`Framebuffers.Default`) | x | |
| Pass registration and ordering | x | |
| Per-pass clear, viewport, framebuffer bind | x | |
| Window resize → MatchWindow FBO resize | x | |
| Attachment → TextureId bridging | x | |
| Color output type definitions | | x |
| Which passes exist and their order | | x |
| Which groups go in which pass | | x |
| Which shaders to use per pass | | x |
| Shader @output annotations | | x |

## Scope

This design covers the infrastructure needed by the shadow epic. It does not include:
- Post-processing chains (ping-pong buffers, fullscreen quad blit) — not needed yet, but the design supports it (render to custom FBO, then sample in a later pass targeting `Framebuffers.Default`)
- Compute shaders — orthogonal concern
- Render pass dependencies — handled implicitly by `Order` and `TextureId` wiring
- MRT (multiple render targets) — the color output interface pattern supports it (multiple fields = multiple color attachments), but no immediate consumer
