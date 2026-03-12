using Olve.Utilities.Ids;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Phantom type for render pass IDs. TFormat links the pass to its framebuffer's color layout.
/// Used as <c>Id&lt;RenderPass&lt;TFormat&gt;&gt;</c>.
/// </summary>
public sealed class RenderPass<TFormat> where TFormat : IFrameFormat;

[Flags]
public enum ClearFlags
{
    None = 0,
    Color = 1,
    Depth = 2,
    ColorDepth = Color | Depth,
}

/// <summary>
/// Central registry for render passes. Owns per-frame execution order.
/// </summary>
public class RenderPassManager
{
    private readonly Dictionary<Id, RenderPassEntry> _passes = new();

    private record RenderPassEntry(Id FramebufferId, int Priority, ClearFlags Clear);

    /// <summary>
    /// Creates a render pass targeting a framebuffer.
    /// The TFormat type parameter is inferred from the framebuffer and enforces
    /// that only shaders implementing IShader&lt;TFormat&gt; can register groups into this pass.
    /// </summary>
    public Id<RenderPass<TFormat>> Create<TFormat>(
        Id<Framebuffer<TFormat>> framebuffer,
        int priority,
        ClearFlags clear = ClearFlags.ColorDepth)
        where TFormat : IFrameFormat
    {
        var passId = Id.New<RenderPass<TFormat>>();

        _passes[passId.Value] = new RenderPassEntry(framebuffer.Value, priority, clear);

        return passId;
    }
}
