using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Input;

public class GuiFocusService
{
    public Id<GuiNode>? FocusedNode { get; private set; }
    public Event<GuiFocusChanged> OnFocusChanged { get; } = new();

    public void SetFocus(Id<GuiNode>? nodeId)
    {
        if (FocusedNode == nodeId) return;

        var previous = FocusedNode;
        FocusedNode = nodeId;
        OnFocusChanged.Invoke(new GuiFocusChanged(previous, nodeId));
    }
}