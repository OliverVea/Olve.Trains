using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Scenes.GameUI.Tools;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class ToolBarService(
    ToolManagementService toolManagementService,
    GuiElementService guiElementService,
    GuiNodeStateService stateService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService) : ISceneService
{
    public int Priority => 100;

    private static readonly Layouts.ToolBar ToolBar = Layouts.BuildToolBar();

    private Id<GuiAnchor> _anchorId;
    private Id<GuiElementRegistrations> _registrationId;

    private readonly (Box, Id<Tool>)[] _toolElements = [
        (ToolBar.PlaceTrack, TrackPlacingToolService.ToolId),
        (ToolBar.PlaceStation, StationPlacingToolService.ToolId),
        (ToolBar.PlaceTrain, TrainPlacingToolService.ToolId),
        (ToolBar.Delete, DeletionToolService.ToolId)
    ];

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.BottomCenter, GrowthDirection.Up)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);
        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);

        return guiElementService
            .RegisterElementAndChildren(_anchorId, ToolBar.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiActivationService.GuiElementActivated.Unsubscribe(OnGuiElementActivated);
        toolManagementService.ActiveToolChanged.Unsubscribe(OnActiveToolChanged);

        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        foreach (var (box, toolId) in _toolElements)
        {
            if (NodeIdMatches(box, message.NodeId))
            {
                toolManagementService.ToggleActiveTool(toolId);
            }
        }
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, _registrationId, out var guiElementNodeId))
        {
            return false;
        }

        return nodeId == guiElementNodeId;
    }

    private void OnActiveToolChanged(ToolManagementService.ActiveToolChangedMessage message)
    {
        if (GetNode(message.CurrentTool) is { } currentNodeIds)
        {
            stateService.UpdateState(currentNodeIds, s => s & ~GuiNodeState.Active);
        }

        if (GetNode(message.NewTool) is { } newNodeIds)
        {
            stateService.UpdateState(newNodeIds, s => s | GuiNodeState.Active);
        }
    }

    private Id<GuiNode>? GetNode(Id<Tool>? toolId)
    {
        if (GetToolElement(toolId) is { } currentElement && guiElementService.TryGetGuiNodeId(currentElement.Id, _registrationId, out var currentNodeId))
        {
            return currentNodeId;
        }

        return null;
    }

    private Box? GetToolElement(Id<Tool>? toolId)
    {
        foreach (var (box, boxToolId) in _toolElements)
        {
            if (boxToolId == toolId)
            {
                return box;
            }
        }

        return null;
    }
}