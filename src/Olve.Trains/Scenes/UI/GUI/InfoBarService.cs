using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Logging;
using Olve.Trains.Scenes.UI.Tools;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(
    ILoggingManager loggingManager,
    ToolManagementService toolManagementService,
    GuiElementService guiElementService,
    GuiNodeStateService stateService,
    GuiNodeService guiNodeService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService) : SceneService(loggingManager)
{
    public override int Priority => 100;

    private static readonly Layouts.ToolBar ToolBar = Layouts.BuildToolBar();

    private Id<GuiElementRegistrations> _registrationId;

    protected override Result OnLoad()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.BottomCenter, GrowthDirection.Up)
            .TryPickProblems(out var problems, out var anchorId))
        {
            return problems;
        }

        guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);
        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);

        return guiElementService
            .RegisterElementAndChildren(anchorId, ToolBar.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (NodeIdMatches(ToolBar.PlaceTrack, message.NodeId))
        {
            toolManagementService.ToggleActiveTool(TrackPlacingToolService.ToolId);
        }

        if (NodeIdMatches(ToolBar.PlaceTrain, message.NodeId))
        {
            toolManagementService.ToggleActiveTool(TrainPlacingToolService.ToolId);
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
        if (GetNodeAndDescendants(message.CurrentTool) is { } currentNodeIds)
        {
            stateService.UpdateAll(currentNodeIds, s => s & ~GuiNodeState.Active);
        }

        if (GetNodeAndDescendants(message.NewTool) is { } newNodeIds)
        {
            stateService.UpdateAll(newNodeIds, s => s | GuiNodeState.Active);
        }
    }

    private IEnumerable<Id<GuiNode>>? GetNodeAndDescendants(Id<Tool>? toolId)
    {
        if (GetToolElement(toolId) is { } currentElement && guiElementService.TryGetGuiNodeId(currentElement.Id, _registrationId, out var currentNodeId))
        {
            if (guiNodeService
                .GetNodeAndDescendants(currentNodeId)
                .TryPickProblems(out var problems, out var nodeIds))
            {
                LoggingManager.Log(problems.Prepend("Failed to get node and descendants for node with id '{0}'", currentNodeId));
                return null;
            }

            return nodeIds;

        }

        return null;
    }

    private Box? GetToolElement(Id<Tool>? toolId)
    {
        if (toolId == TrackPlacingToolService.ToolId) return ToolBar.PlaceTrack;
        if (toolId == TrainPlacingToolService.ToolId) return ToolBar.PlaceTrain;

        return null;
    }
}