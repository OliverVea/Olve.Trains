using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Generated.Textures;
using Olve.Logging;
using Olve.Trains.Scenes.UI.Tools;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(
    ILoggingManager loggingManager,
    ToolManagementService toolManagementService,
    GuiElementService guiElementService,
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
        var currentBox = GetToolElement(message.CurrentTool);
        currentBox?.BorderColor = (0.2f, 0.2f, 0.2f, 0.3f);

        var newBox = GetToolElement(message.NewTool);
        newBox?.BorderColor = (1, 1, 1, 1);
    }

    private Box? GetToolElement(Id<Tool>? toolId)
    {
        if (toolId == TrackPlacingToolService.ToolId) return ToolBar.PlaceTrack;
        if (toolId == TrainPlacingToolService.ToolId) return ToolBar.PlaceTrain;

        return null;
    }
}