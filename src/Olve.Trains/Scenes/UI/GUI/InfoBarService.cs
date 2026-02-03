using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiAnchorService guiAnchorService) : SceneService(loggingManager)
{

    public override int Priority => 100;

    private static readonly Layouts.InfoBar InfoBar = Layouts.BuildInfoBar();

    private Id<GuiElementRegistrations> _registrationId;

    protected override Result OnLoad()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight)
            .TryPickProblems(out var problems, out var anchorId))
        {
            return problems;
        }

        return guiElementService
            .RegisterElementAndChildren(anchorId, InfoBar.BarBackground)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }
}