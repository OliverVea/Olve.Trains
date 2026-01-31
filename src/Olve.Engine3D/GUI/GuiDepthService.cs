using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiDepthService(ILoggingManager loggingManager, GuiNodeService guiNodeService) : BaseEntityAuxiliaryService<GuiNode>(loggingManager, guiNodeService)
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
            LoggingManager.Log(LogLevel.Error, $"Could not get parent for node '{nodeId}'. Setting depth for node to 0.");
        }

        _depths[nodeId] = 0;
    }

    protected override void OnRemoved(Id<GuiNode> nodeId) => _depths.Remove(nodeId);

    public int GetDepth(Id<GuiNode> nodeId)
    {
        if (!_depths.TryGetValue(nodeId, out var depth))
        {
            LoggingManager.Log(LogLevel.Error, $"Could not get depth for node '{nodeId}'. Returning int.MinValue");
            return int.MinValue;
        }

        return depth;
    }
}
