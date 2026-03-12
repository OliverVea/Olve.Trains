// ════════════════════════════════════════════════════════════════════════════
// Render Pass Prototype — Scene ownership demonstration
// Must compile. Does not need to run.
// ════════════════════════════════════════════════════════════════════════════

using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Trains.RenderPassPrototype;
using Olve.Utilities.Ids;
using Olve.Generated.Shaders;

// ── Placeholder geometry IDs (would come from GeometryManager in the real engine) ──
var meshGeometry = new GeometryId<Shaders.Default.Vertex>(Id.New());
var quadGeometry = new GeometryId<Shaders.TexturedRectangle.Vertex>(Id.New());

// ── Managers ──
var fbManager = new FramebufferManager();
var passManager = new RenderPassManager();
var groupManager = new RenderingGroupManager();
var screen = new ScreenPass();

// ════════════════════════════════════════════════════════════════════════════
// SharedRenderingService — owns the output framebuffer + screen blit
// Always loaded. Both game and GUI scenes depend on this, not on each other.
// ════════════════════════════════════════════════════════════════════════════

var (mainFb, mainColor, mainDepth) =
    fbManager.CreateWithDepth<IDefaultFrameFormat, RGBA>(1920, 1080);

screen.SetSource(mainColor); // wire color output → screen

// On window resize: fbManager.Resize(mainFb, newW, newH);
// TextureIds (mainColor, mainDepth) remain stable — GPU textures are recreated.

// ════════════════════════════════════════════════════════════════════════════
// GameSceneRenderService — owns shadow map FB + 3D render passes
// Depends on SharedRenderingService (uses mainFb), not on GUI.
// ════════════════════════════════════════════════════════════════════════════

// Shadow map — depth-only framebuffer, game-owned
var (shadowFb, shadowMap) =
    fbManager.CreateWithDepth<IDepthFrameFormat>(4096, 4096);

var shadowPass = passManager.Create(shadowFb, priority: 0, ClearFlags.Depth);

// Main 3D pass — renders into the shared framebuffer
var mainPass = passManager.Create(mainFb, priority: 10, ClearFlags.ColorDepth);

// Wire shadow map output → lit shader input
var defaultShader = new DefaultShader { ShadowMap = shadowMap };

// Register groups — same geometry, different shaders, different passes
groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDepthFrameFormat>(
    meshGeometry, new DepthOnlyShader(), shadowPass, RenderState.Opaque);

groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDefaultFrameFormat>(
    meshGeometry, defaultShader, mainPass, RenderState.Opaque);

// ════════════════════════════════════════════════════════════════════════════
// GuiSceneRenderService — owns GUI render pass
// Depends on SharedRenderingService (uses mainFb), NOT on GameScene.
// Loaded alongside GameScene or MainMenuScene — doesn't matter which.
// ════════════════════════════════════════════════════════════════════════════

var guiPass = passManager.Create(mainFb, priority: 20, ClearFlags.None);

groupManager.Register<Shaders.TexturedRectangle.Vertex, Shaders.TexturedRectangle.Instance, IDefaultFrameFormat>(
    quadGeometry, new GuiShader(), guiPass, RenderState.AlphaBlend);

// ════════════════════════════════════════════════════════════════════════════
// Compile-time safety checks — uncomment any line to see a compile error
// ════════════════════════════════════════════════════════════════════════════

// ✗ Format mismatch: DepthOnlyShader (IDepthFrameFormat) vs mainPass (IDefaultFrameFormat)
// groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDefaultFrameFormat>(
//     meshGeometry, new DepthOnlyShader(), mainPass, RenderState.Opaque);

// ✗ Format mismatch: DefaultShader (IDefaultFrameFormat) vs shadowPass (IDepthFrameFormat)
// groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDepthFrameFormat>(
//     meshGeometry, defaultShader, shadowPass, RenderState.Opaque);

// ✗ Format mismatch: GuiShader (IDefaultFrameFormat) vs shadowPass (IDepthFrameFormat)
// groupManager.Register<Shaders.TexturedRectangle.Vertex, Shaders.TexturedRectangle.Instance, IDepthFrameFormat>(
//     quadGeometry, new GuiShader(), shadowPass, RenderState.AlphaBlend);
