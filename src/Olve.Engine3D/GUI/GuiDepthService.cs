using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiDepthService(ILogger<GuiDepthService> logger, GuiNodeService guiNodeService) : BaseEntityAuxiliaryService<GuiNode>(guiNodeService)
{
    private readonly Dictionary<Id<GuiNode>, int> _depths = [];

    protected override void OnAdded(Id<GuiNode> nodeId)
    {
        if (guiNodeService.TryGetParent(nodeId, out var parent))
        {
            if (parent.TryGetT2(out var guiNodeParent, out _))
            {
                var depth = GetDepth(guiNodeParent) + 1;
                _depths[nodeId] = depth;
                return;
            }
        }
        else
        {
            logger.LogError("Could not get parent for node '{NodeId}'. Setting depth for node to 0.", nodeId);
        }

        _depths[nodeId] = 0;
    }

    protected override void OnRemoved(Id<GuiNode> nodeId) => _depths.Remove(nodeId);

    public int GetDepth(Id<GuiNode> nodeId)
    {
        if (!_depths.TryGetValue(nodeId, out var depth))
        {
            logger.LogError("Could not get depth for node '{NodeId}'. Returning int.MinValue", nodeId);
            return int.MinValue;
        }

        return depth;
    }
}
