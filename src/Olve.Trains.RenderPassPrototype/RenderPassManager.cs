using Olve.Results;
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
/// Info about a render pass, returned by <see cref="RenderPassManager.GetOrderedPasses"/>.
/// Uses untyped Id since the render loop iterates all passes regardless of format.
/// </summary>
public record RenderPassInfo(Id PassId, Id FramebufferId, int Priority, ClearFlags Clear);

/// <summary>
/// Central registry for render passes. Owns per-frame execution order.
/// </summary>
public class RenderPassManager
{
    private readonly Dictionary<Id, RenderPassEntry> _passes = new();
    private List<RenderPassInfo>? _orderedCache;

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
        _orderedCache = null;

        return passId;
    }

    // ── Destroy ──

    public DeletionResult Destroy<TFormat>(Id<RenderPass<TFormat>> pass)
        where TFormat : IFrameFormat
    {
        if (!_passes.Remove(pass.Value))
            return DeletionResult.NotFound();

        _orderedCache = null;
        return DeletionResult.Success();
    }

    // ── Query ──

    /// <summary>
    /// Returns all passes sorted by priority (ascending). This is the per-frame execution order.
    /// </summary>
    public IReadOnlyList<RenderPassInfo> GetOrderedPasses()
    {
        if (_orderedCache is not null)
            return _orderedCache;

        _orderedCache = _passes
            .Select(kvp => new RenderPassInfo(kvp.Key, kvp.Value.FramebufferId, kvp.Value.Priority, kvp.Value.Clear))
            .OrderBy(p => p.Priority)
            .ToList();

        return _orderedCache;
    }

    public bool Exists<TFormat>(Id<RenderPass<TFormat>> pass)
        where TFormat : IFrameFormat
        => _passes.ContainsKey(pass.Value);
}
