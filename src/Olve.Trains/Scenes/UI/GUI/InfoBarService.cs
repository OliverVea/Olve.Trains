using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Logging;
using Olve.Trains.Scenes.UI.Tools;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(
    ILoggingManager loggingManager,
    ToolManagementService toolManagementService,
    GuiElementService guiElementService,
    GuiElementStateService stateService,
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

        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);

        return guiElementService
            .RegisterElementAndChildren(anchorId, ToolBar.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    private void OnActiveToolChanged(ToolManagementService.ActiveToolChangedMessage message)
    {
        // Clear focus from previous tool
        if (GetToolElement(message.CurrentTool) is { } currentElement
            && guiElementService.TryGetGuiNodeId(currentElement.Id, _registrationId, out var currentNodeId))
        {
            stateService.UpdateState(currentNodeId, s => s & ~GuiElementState.Focused);
        }

        // Set focus on new tool
        if (GetToolElement(message.NewTool) is { } newElement
            && guiElementService.TryGetGuiNodeId(newElement.Id, _registrationId, out var newNodeId))
        {
            stateService.UpdateState(newNodeId, s => s | GuiElementState.Focused);
        }
    }

    private Box? GetToolElement(Id<Tool>? toolId)
    {
        if (toolId == TrackPlacingToolService.ToolId) return ToolBar.PlaceTrack;
        if (toolId == TrainPlacingToolService.ToolId) return ToolBar.PlaceTrain;

        return null;
    }
}