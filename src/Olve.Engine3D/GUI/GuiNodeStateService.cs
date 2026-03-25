using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiNodeStateService(ILogger<GuiNodeStateService> logger, GuiNodeService guiNodeService, GuiElementService guiElementService)
{
    public readonly record struct GuiNodeStateChanged(
        Id<GuiNode> NodeId,
        GuiNodeState Before,
        GuiNodeState After);

    private readonly Dictionary<Id<GuiNode>, GuiNodeState> _stateRegistry = new();

    public Event<GuiNodeStateChanged> OnStateChanged { get; } = new();

    public void RemoveState(Id<GuiNode> nodeId)
    {
        _stateRegistry.Remove(nodeId);
    }

    public bool TryGetState(Id<GuiNode> nodeId, out GuiNodeState state)
    {
        return _stateRegistry.TryGetValue(nodeId, out state);
    }

    public void SetState(Id<GuiNode> nodeId, GuiNodeState newState)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiNodeState.None);

        SetState(nodeId, existingState, newState);
    }

    public void UpdateState(Id<GuiNode> nodeId, Func<GuiNodeState, GuiNodeState> update)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiNodeState.None);
        var newState = update(existingState);

        if (existingState == newState)
        {
            return;
        }

        SetState(nodeId, existingState, newState);
    }

    private void SetState(Id<GuiNode> nodeId, GuiNodeState before, GuiNodeState after)
    {
        List<Id<GuiNode>> toUpdate = [nodeId, ..GetChildrenInheritingState(nodeId)];

        foreach (var n in toUpdate)
        {
            _stateRegistry[n] = after;
            var source = n == nodeId ? "direct" : "inherited";
            logger.LogDebug("Node '{NodeId}' changed from '{Before}' to '{After}' ({Source})", n, before, after, source);
            OnStateChanged.Invoke(new GuiNodeStateChanged(n, before, after));
        }
    }

    private IEnumerable<Id<GuiNode>> GetChildrenInheritingState(Id<GuiNode> nodeId)
    {
        if (!guiNodeService.TryGetChildren(nodeId, out var children))
        {
            yield break;
        }

        foreach (var childId in children)
        {
            if (guiElementService.TryGetElement(childId, out var element) && element.InheritParentState)
            {
                yield return childId;
            }
        }
    }
}
