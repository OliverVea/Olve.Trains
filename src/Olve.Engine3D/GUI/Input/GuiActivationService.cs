using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Input;

public class GuiActivationService
{
    public readonly record struct GuiElementActivatedMessage(Id<GuiNode> NodeId);
    public Event<GuiElementActivatedMessage> GuiElementActivated { get; } = new();

    public void Activate(Id<GuiNode> pressedNode)
    {
        GuiElementActivated.Invoke(new GuiElementActivatedMessage(pressedNode));
    }
}