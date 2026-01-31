using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStateListenerService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiNodeService guiNodeService,
    GuiElementStateService stateService) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = new(guiElementService.OnRemoved);
    private readonly EventQueue<Id<GuiNode>> _nodeEnabledQueue = new(guiNodeService.OnEnabled);
    private readonly EventQueue<Id<GuiNode>> _nodeDisabledQueue = new(guiNodeService.OnDisabled);

    protected override Result OnLoad()
    {
        _elementAddedQueue.SetHandler(OnElementAdded).Init();
        _elementRemovedQueue.SetHandler(OnElementRemoved).Init();
        _nodeEnabledQueue.SetHandler(OnNodeEnabled).Init();
        _nodeDisabledQueue.SetHandler(OnNodeDisabled).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return Result.Chain(
            _elementAddedQueue.Update,
            _elementRemovedQueue.Update,
            _nodeEnabledQueue.Update,
            _nodeDisabledQueue.Update);
    }

    private Result OnElementAdded(GuiElementArgs args)
    {
        stateService.SetState(args.NodeId, GuiElementState.Show | GuiElementState.Enabled);
        return Result.Success();
    }

    private Result OnElementRemoved(GuiElementArgs args)
    {
        stateService.SetState(args.NodeId, GuiElementState.None);
        return Result.Success();
    }

    private Result OnNodeEnabled(Id<GuiNode> nodeId)
    {
        stateService.UpdateState(nodeId, s => s | GuiElementState.Enabled);
        return Result.Success();
    }

    private Result OnNodeDisabled(Id<GuiNode> nodeId)
    {
        stateService.UpdateState(nodeId, s => s & ~GuiElementState.Enabled);
        return Result.Success();
    }

    // TODO: Subscribe to hover/focus/input events and update state accordingly
    // e.g., stateService.UpdateState(nodeId, s => s | GuiElementState.Hovered);
    // e.g., stateService.UpdateState(nodeId, s => s & ~GuiElementState.Hovered);
}
