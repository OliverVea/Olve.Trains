using Olve.Engine3D;
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
    Provider<LayoutContext> layoutContextProvider,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => 100;
    private double _t;

    private static readonly Layouts.InfoBar InfoBar = Layouts.BuildInfoBar();

    private Id<GuiElementRegistrations> _registrationId;

    protected override Result OnLoad()
    {
        var anchorId = Id.New<GuiAnchor>();

        return guiElementService
            .RegisterElementAndChildren(anchorId, InfoBar.BarBackground)
            .TryPickProblems(out var problems, out _registrationId) ? problems : Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        _t += deltaTime.TotalSeconds;
        var dx = new Dp((float) (1 - Math.Cos(_t)) * 50f);
        var left = dx;

        if (Result.Concat(
                UpdateElementLayoutBox(InfoBar.BarBackground, b => b with {Size = b.Size with {PreferredWidth = layoutContextProvider.Value.DesignSize.X}}),
                UpdateElementLayoutBox(InfoBar.HelloText, b => b with { Margin = b.Margin with { Left = left } }))
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        //TODO: Optimize this :)
        return guiLayoutService.ComputeLayout();
    }

    private Result UpdateElementLayoutBox(GuiElement guiElement, Func<LayoutBox, LayoutBox> update)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, _registrationId, out var nodeId))
        {
            return new ResultProblem("Could not get node id for element");
        }

        var newLayoutBox = update(guiElement.LayoutBox!.Value);
        guiLayoutService.CreateOrSetNodeBox(nodeId, newLayoutBox);

        return Result.Success();
    }
}