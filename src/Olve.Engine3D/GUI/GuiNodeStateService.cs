using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiNodeStateService
{
    public readonly record struct GuiNodeStateChanged(
        Id<GuiNode> NodeId,
        GuiNodeState Before,
        GuiNodeState After);

    private readonly Dictionary<Id<GuiNode>, GuiNodeState> _stateRegistry = new();

    public Event<GuiNodeStateChanged> OnStateChanged { get; } = new();

    public bool TryGetState(Id<GuiNode> nodeId, out GuiNodeState state)
    {
        return _stateRegistry.TryGetValue(nodeId, out state);
    }

    public void SetState(Id<GuiNode> nodeId, GuiNodeState state)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiNodeState.None);
        _stateRegistry[nodeId] = state;
        OnStateChanged.Invoke(new GuiNodeStateChanged(nodeId, existingState, state));
    }

    public void UpdateState(Id<GuiNode> nodeId, Func<GuiNodeState, GuiNodeState> update)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiNodeState.None);
        var newState = update(existingState);

        if (existingState == newState)
        {
            return;
        }

        _stateRegistry[nodeId] = newState;
        OnStateChanged.Invoke(new GuiNodeStateChanged(nodeId, existingState, newState));
    }

    public void UpdateAll(IEnumerable<Id<GuiNode>> nodeIds, Func<GuiNodeState, GuiNodeState> update)
    {
        foreach (var nodeId in nodeIds)
        {
            UpdateState(nodeId, update);
        }
    }
}
