using Olve.Engine3D.GUI.Collision;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;
using Silk.NET.Input;

namespace Olve.Engine3D.GUI.Input;

public class GuiMouseInputService(
    MouseManager mouseManager,
    GuiCollisionService guiCollisionService,
    GuiActivationService guiActivationService,
    GuiElementService guiElementService,
    GuiDepthService guiDepthService,
    GuiFocusService guiFocusService,
    GuiNodeStateService stateService) : ISceneService
{
    private Id<GuiNode>? _pressedNode;

    public Event<Id<GuiNode>> OnPressedNode { get; } = new();
    public Event<Id<GuiNode>> OnReleasedNode { get; } = new();

    public int Priority => -100;

    public Result<Pass> Input()
    {
        var mousePos = GetMousePositionInPx();

        var topmost = guiCollisionService.GetGuiCollisions(mousePos)
            .Where(x => guiElementService.TryGetElement(x, out var element) && element.Interactive)
            .OrderByDescending(guiDepthService.GetDepth)
            .Cast<Id<GuiNode>?>()
            .FirstOrDefault();

        guiFocusService.SetFocus(topmost);

        var shouldBlock = HandleMouseButtons(topmost);

        return shouldBlock ? Pass.Block : Pass.Pass;
    }

    private Vector2D<Px> GetMousePositionInPx()
    {
        var pos = mouseManager.State.Position;
        return new Vector2D<Px>(new Px((int)pos.X), new Px((int)pos.Y));
    }

    private bool HandleMouseButtons(Id<GuiNode>? topmost)
    {
        if (mouseManager.State.IsButtonPressed(MouseButton.Left) && topmost.HasValue)
        {
            _pressedNode = topmost;
            OnPressedNode.Invoke(_pressedNode.Value);
            return true;
        }

        if (mouseManager.State.IsButtonReleased(MouseButton.Left) && _pressedNode is {} pressedNode)
        {
            if (topmost == pressedNode)
            {
                if (stateService.TryGetState(pressedNode, out var state)
                    && state.HasFlag(GuiNodeState.Enabled))
                {
                    guiActivationService.Activate(pressedNode);
                }
            }

            _pressedNode = null;
            OnReleasedNode.Invoke(pressedNode);
            return true;
        }

        return _pressedNode.HasValue;
    }
}
