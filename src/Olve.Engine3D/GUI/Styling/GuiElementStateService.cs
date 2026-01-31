using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Styling;

public class GuiElementStateService
{
    public readonly record struct GuiElementStateChanged(
        Id<GuiNode> NodeId,
        GuiElementState Before,
        GuiElementState After)
    {
        public (bool Changed, bool Enabled) GetChangeFor(GuiElementState state)
        {
            var changed = Before.HasFlag(state) != After.HasFlag(state);
            var enabled = After.HasFlag(state);

            return (changed, enabled);
        }
    }

    private readonly Dictionary<Id<GuiNode>, GuiElementState> _stateRegistry = new();

    public Event<GuiElementStateChanged> OnStateChanged { get; } = new();

    public bool TryGetState(Id<GuiNode> nodeId, out GuiElementState state)
    {
        return _stateRegistry.TryGetValue(nodeId, out state);
    }

    public void SetState(Id<GuiNode> nodeId, GuiElementState state)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiElementState.None);
        _stateRegistry[nodeId] = state;
        OnStateChanged.Invoke(new GuiElementStateChanged(nodeId, existingState, state));
    }

    public void UpdateState(Id<GuiNode> nodeId, Func<GuiElementState, GuiElementState> update)
    {
        var existingState = _stateRegistry.GetValueOrDefault(nodeId, GuiElementState.None);
        var newState = update(existingState);

        if (existingState == newState)
        {
            return;
        }

        _stateRegistry[nodeId] = newState;
        OnStateChanged.Invoke(new GuiElementStateChanged(nodeId, existingState, newState));
    }
}
