using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public class RenderPassManager
{
    private readonly Dictionary<Id, RenderPassEntry> _passes = new();
    private List<RenderPassInfo>? _orderedCache;

    private record RenderPassEntry(Id FramebufferId, int Priority, ClearFlags Clear);

    public Result<Id<RenderPass<TFormat>>> Create<TFormat>(
        Id<Framebuffer<TFormat>> framebufferId,
        int priority,
        ClearFlags clear = ClearFlags.ColorDepth)
        where TFormat : IFrameFormat
    {
        var passId = Id.New<RenderPass<TFormat>>();

        _passes[passId.Value] = new RenderPassEntry(framebufferId.Value, priority, clear);
        _orderedCache = null;

        return passId;
    }

    public DeletionResult Destroy<TFormat>(Id<RenderPass<TFormat>> passId)
        where TFormat : IFrameFormat
    {
        if (!_passes.Remove(passId.Value))
            return DeletionResult.NotFound();

        _orderedCache = null;
        return DeletionResult.Success();
    }

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

    public bool Exists<TFormat>(Id<RenderPass<TFormat>> passId)
        where TFormat : IFrameFormat
        => _passes.ContainsKey(passId.Value);
}
