using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class InfoBarService(
    DayTimeManager dayTimeManager,
    GuiElementService guiElementService,
    GuiAnchorService guiAnchorService) : ISceneService
{

    public int Priority => 100;

    private static readonly Layouts.InfoBar InfoBar = Layouts.BuildInfoBar();

    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        return guiElementService
            .RegisterElementAndChildren(_anchorId, InfoBar.BarBackground)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiAnchorService.UnregisterAnchor(_anchorId);

        // Todo: Unregister element and children
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        var dayTime = dayTimeManager.CurrentTime;
        InfoBar.Clock.Content = $"{dayTime.Hours:D2} : {dayTime.Minutes:D2}";
        return Result.Success();
    }
}