using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiAnchorService(ILogger<GuiAnchorService> logger)
{
    private readonly Dictionary<Id<GuiAnchor>, GuiAnchor> _anchors = new();

    public Result<Id<GuiAnchor>> RegisterAnchor(AnchorPosition position, GrowthDirection growth, int depth = 0)
    {
        var anchorId = Id.New<GuiAnchor>();
        var anchor = new GuiAnchor(anchorId, position, growth, depth);
        _anchors[anchorId] = anchor;
        logger.LogDebug(
            "Registered anchor '{AnchorId}' at position ({HPos}, {VPos}) with growth ({HGrowth}, {VGrowth}). Total anchors: {Count}",
            anchorId, position.Horizontal, position.Vertical, growth.Horizontal, growth.Vertical, _anchors.Count);
        return anchorId;
    }

    public Result<Id<GuiAnchor>> RegisterAnchor(
        Id<GuiNode> referenceNode,
        AnchorPosition position,
        GrowthDirection growth,
        int depth = 0)
    {
        var anchorId = Id.New<GuiAnchor>();
        var anchor = new GuiAnchor(anchorId, position, growth, depth, referenceNode);
        _anchors[anchorId] = anchor;
        logger.LogDebug(
            "Registered relative anchor '{AnchorId}' referencing node '{ReferenceNode}' at position ({HPos}, {VPos}) with growth ({HGrowth}, {VGrowth}). Total anchors: {Count}",
            anchorId, referenceNode, position.Horizontal, position.Vertical, growth.Horizontal, growth.Vertical, _anchors.Count);
        return anchorId;
    }

    public bool TryGetAnchor(Id<GuiAnchor> anchorId, out GuiAnchor anchor)
        => _anchors.TryGetValue(anchorId, out anchor);

    public Result UpdateAnchor(Id<GuiAnchor> anchorId, GuiAnchor anchor)
    {
        if (anchor.Id != anchorId)
        {
            return new ResultProblem("Anchor ID mismatch: expected '{0}', got '{1}'", anchorId, anchor.Id);
        }

        if (!_anchors.ContainsKey(anchorId))
        {
            return new ResultProblem("Anchor '{0}' not found", anchorId);
        }

        _anchors[anchorId] = anchor;
        logger.LogDebug("Updated anchor '{AnchorId}'. Total anchors: {Count}", anchorId, _anchors.Count);
        return Result.Success();
    }

    public void UnregisterAnchor(Id<GuiAnchor> anchorId)
    {
        if (_anchors.Remove(anchorId))
        {
            logger.LogDebug("Unregistered anchor '{AnchorId}'. Total anchors: {Count}", anchorId, _anchors.Count);
        }
        else
        {
            logger.LogWarning("Failed to unregister anchor '{AnchorId}': anchor not found. Total anchors: {Count}", anchorId, _anchors.Count);
        }
    }
}
