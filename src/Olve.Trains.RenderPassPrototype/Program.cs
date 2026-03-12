// ════════════════════════════════════════════════════════════════════════════
// Render Pass Prototype — Scene ownership demonstration
// Must compile. Does not need to run.
// ════════════════════════════════════════════════════════════════════════════

using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Results;
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

if (fbManager.CreateWithDepth<IDefaultFrameFormat, RGBA>(1920, 1080)
    .TryPickProblems(out var problems, out var mainFbResult))
{
    throw new Exception($"Failed to create main framebuffer: {problems}");
}

var (mainFb, mainColor, mainDepth) = mainFbResult;

screen.SetSource(mainColor); // wire color output → screen

// On window resize: fbManager.Resize(mainFb, newW, newH);
// TextureIds (mainColor, mainDepth) remain stable — GPU textures are recreated.

// ════════════════════════════════════════════════════════════════════════════
// GameSceneRenderService — owns shadow map FB + 3D render passes
// Depends on SharedRenderingService (uses mainFb), not on GUI.
// ════════════════════════════════════════════════════════════════════════════

// Shadow map — depth-only framebuffer, game-owned
if (fbManager.CreateWithDepth<IDepthFrameFormat>(4096, 4096)
    .TryPickProblems(out problems, out var shadowFbResult))
{
    throw new Exception($"Failed to create shadow framebuffer: {problems}");
}

var (shadowFb, shadowMap) = shadowFbResult;

var shadowPass = passManager.Create(shadowFb, priority: 0, ClearFlags.Depth);

// Main 3D pass — renders into the shared framebuffer
var mainPass = passManager.Create(mainFb, priority: 10, ClearFlags.ColorDepth);

// Wire shadow map output → lit shader input
var defaultShader = new DefaultShader { ShadowMap = shadowMap };

// Register groups — same geometry, different shaders, different passes
groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDepthFrameFormat>(
    meshGeometry, new DepthOnlyShader(), shadowPass, RenderState.Opaque);

if (groupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDefaultFrameFormat>(
    meshGeometry, defaultShader, mainPass, RenderState.Opaque)
    .TryPickProblems(out problems, out var mainMeshGroup))
{
    throw new Exception($"Failed to register main mesh group: {problems}");
}

// ════════════════════════════════════════════════════════════════════════════
// GuiSceneRenderService — owns GUI render pass
// Depends on SharedRenderingService (uses mainFb), NOT on GameScene.
// Loaded alongside GameScene or MainMenuScene — doesn't matter which.
// ════════════════════════════════════════════════════════════════════════════

var guiPass = passManager.Create(mainFb, priority: 20, ClearFlags.None);

if (groupManager.Register<Shaders.TexturedRectangle.Vertex, Shaders.TexturedRectangle.Instance, IDefaultFrameFormat>(
    quadGeometry, new GuiShader(), guiPass, RenderState.AlphaBlend)
    .TryPickProblems(out problems, out var guiGroup))
{
    throw new Exception($"Failed to register gui group: {problems}");
}

// ════════════════════════════════════════════════════════════════════════════
// Render loop — iterate ordered passes, get groups per pass
// ════════════════════════════════════════════════════════════════════════════

foreach (var pass in passManager.GetOrderedPasses())
{
    // In real engine: bind framebuffer, apply clear flags
    Console.WriteLine($"Pass priority={pass.Priority} clear={pass.Clear} fb={pass.FramebufferId}");

    foreach (var group in groupManager.GetGroupsForPass(pass.PassId))
    {
        // In real engine: apply render state, load shader, bind geometry, draw instances
        Console.WriteLine($"  Group shader={group.Shader.ShaderData.Name} sortKey={group.SortKey} primitive={group.PrimitiveType}");
    }
}

// Blit to screen
Console.WriteLine($"Screen blit source={screen.Source}");

// ════════════════════════════════════════════════════════════════════════════
// Cleanup — destroy groups, passes, framebuffers (reverse order of creation)
// ════════════════════════════════════════════════════════════════════════════

Console.WriteLine($"\nDestroy gui group: {groupManager.Destroy(guiGroup)}");
Console.WriteLine($"Destroy gui pass: {passManager.Destroy(guiPass)}");

Console.WriteLine($"Destroy main mesh group: {groupManager.Destroy(mainMeshGroup)}");
Console.WriteLine($"Destroy main pass: {passManager.Destroy(mainPass)}");
Console.WriteLine($"Destroy shadow pass: {passManager.Destroy(shadowPass)}");

Console.WriteLine($"Destroy shadow fb: {fbManager.Destroy(shadowFb)}");
Console.WriteLine($"Destroy main fb: {fbManager.Destroy(mainFb)}");

// Double-destroy should return NotFound
Console.WriteLine($"Double-destroy main fb: {fbManager.Destroy(mainFb)}");

// ════════════════════════════════════════════════════════════════════════════
// Validation — negative dimensions should fail
// ════════════════════════════════════════════════════════════════════════════

var invalidResult = fbManager.CreateWithDepth<IDefaultFrameFormat, RGBA>(-1, 0);
Console.WriteLine($"\nInvalid dimensions result: failed={invalidResult.Failed}");

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
