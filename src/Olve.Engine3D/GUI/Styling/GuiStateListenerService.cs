using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStateListenerService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiNodeService guiNodeService,
    GuiNodeStateService stateService,
    GuiMouseInputService guiMouseInputService,
    GuiFocusService guiFocusService) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = new(guiElementService.OnRemoved);
    private readonly EventQueue<Id<GuiNode>> _nodeEnabledQueue = new(guiNodeService.OnEnabled);
    private readonly EventQueue<Id<GuiNode>> _nodeDisabledQueue = new(guiNodeService.OnDisabled);
    private readonly EventQueue<GuiFocusChanged> _focusChangedQueue = new(guiFocusService.OnFocusChanged);
    private readonly EventQueue<Id<GuiNode>> _pressedQueue = new(guiMouseInputService.OnPressedNode);
    private readonly EventQueue<Id<GuiNode>> _releasedQueue = new(guiMouseInputService.OnReleasedNode);

    protected override Result OnLoad()
    {
        _elementAddedQueue.SetHandler(OnElementAdded).Init();
        _elementRemovedQueue.SetHandler(OnElementRemoved).Init();
        _nodeEnabledQueue.SetHandler(OnNodeEnabled).Init();
        _nodeDisabledQueue.SetHandler(OnNodeDisabled).Init();
        _focusChangedQueue.SetHandler(OnFocusChanged).Init();
        _pressedQueue.SetHandler(OnNodePressed).Init();
        _releasedQueue.SetHandler(OnNodeReleased).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return Result.Chain(
            _elementAddedQueue.Update,
            _elementRemovedQueue.Update,
            _nodeEnabledQueue.Update,
            _nodeDisabledQueue.Update,
            _focusChangedQueue.Update,
            _pressedQueue.Update,
            _releasedQueue.Update);
    }

    private Result OnElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return new ResultProblem("Could not get GUI element with id '{0}'.", args.NodeId);
        }

        return AddState(args.NodeId, GuiNodeState.Show | GuiNodeState.Enabled);
    }

    private Result OnElementRemoved(GuiElementArgs args)
    {
        return RemoveState(args.NodeId, GuiNodeState.All);
    }

    private Result OnNodeEnabled(Id<GuiNode> nodeId) => AddState(nodeId, GuiNodeState.Enabled);
    private Result OnNodeDisabled(Id<GuiNode> nodeId) => RemoveState(nodeId, GuiNodeState.Enabled);

    private Result OnFocusChanged(GuiFocusChanged change)
    {
        if (change.Previous.HasValue) OnFocusRemoved(change.Previous.Value);
        if (change.Current.HasValue) OnFocusSet(change.Current.Value);
        return Result.Success();
    }

    private Result OnFocusSet(Id<GuiNode> nodeId) => AddState(nodeId, GuiNodeState.Focused);
    private Result OnFocusRemoved(Id<GuiNode> nodeId) => RemoveState(nodeId, GuiNodeState.Focused);

    private Result OnNodePressed(Id<GuiNode> nodeId) => AddState(nodeId, GuiNodeState.Pressed);
    private Result OnNodeReleased(Id<GuiNode> nodeId) => RemoveState(nodeId, GuiNodeState.Pressed);

    private Result AddState(Id<GuiNode> nodeId, GuiNodeState guiNodeState)
    {
        stateService.UpdateState(nodeId, s => s | guiNodeState);
        var inheritingChildren = GetChildrenInheritingState(nodeId);
        foreach (var childId in inheritingChildren)
        {
            AddState(childId, guiNodeState);
        }

        return Result.Success();
    }

    private Result RemoveState(Id<GuiNode> nodeId, GuiNodeState guiNodeState)
    {
        stateService.UpdateState(nodeId, s => s & ~guiNodeState);
        var inheritingChildren = GetChildrenInheritingState(nodeId);
        foreach (var childId in inheritingChildren)
        {
            RemoveState(childId, guiNodeState);
        }
        return Result.Success();
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
