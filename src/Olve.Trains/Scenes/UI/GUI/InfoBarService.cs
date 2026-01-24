using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => 100;
    private double _t;

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

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (!guiElementService.TryGetGuiNodeId(InfoBar.HelloText.Id, _registrationId, out var nodeId))
        {
            return new ResultProblem("Could not get HelloText id");
        }

        _t += deltaTime.TotalSeconds;

        var dx = new Dp((float) (1 - Math.Cos(_t)) * 50f);

        var layoutBox = InfoBar.HelloText.LayoutBox!.Value;

        var left = dx;
        var newMargin = layoutBox.Margin with { Left = left };
        var newLayoutBox = layoutBox with { Margin = newMargin };

        guiLayoutService.CreateOrSetNodeBox(nodeId, newLayoutBox);

        //TODO: Optimize this :)
        return guiLayoutService.ComputeLayout();
    }
}