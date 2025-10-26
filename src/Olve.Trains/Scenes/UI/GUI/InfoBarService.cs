using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(ILoggingManager loggingManager, GuiElementService guiElementService) : SceneService(loggingManager)
{
    public override int Priority => 100;

    private static readonly Layouts.InfoBar InfoBar = Layouts.BuildInfoBar();

    private Id<GuiElementRegistrations> _registrationId;

    protected override Result OnLoad()
    {
        LoggingManager.Log(LogLevel.Warning, "InfoBarService.OnLoad() - Starting element registration");
        var anchorId = Id.New<GuiAnchor>();

        if (guiElementService
            .RegisterElementAndChildren(anchorId, InfoBar.BarBackground)
            .TryPickProblems(out var problems, out _registrationId))
        {
            return problems;
        }

        LoggingManager.Log(LogLevel.Warning, "InfoBarService.OnLoad() - Element registration complete");
        return Result.Success();
    }
}