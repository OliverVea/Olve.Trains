using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class InfoBarService(
    DayTimeManager dayTimeManager,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    BurgerMenuService burgerMenuService) : ISceneService
{

    public int Priority => 100;

    private static readonly Layouts.InfoBar InfoBar = Layouts.BuildInfoBar();

    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;

    private const int FpsWindowSize = 60;
    private readonly Queue<double> _frameTimes = new(FpsWindowSize);
    private TimeSpan _fpsUpdateElapsed;

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);

        return guiElementService
            .RegisterElementAndChildren(_anchorId, InfoBar.BarBackground)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        var dayTime = dayTimeManager.CurrentTime;
        InfoBar.Clock.Content = $"{dayTime.Hours:D2} : {dayTime.Minutes:D2}";

        if (_frameTimes.Count >= FpsWindowSize)
        {
            _frameTimes.Dequeue();
        }
        _frameTimes.Enqueue(deltaTime.TotalSeconds);

        _fpsUpdateElapsed += deltaTime;
        if (_fpsUpdateElapsed.TotalSeconds >= 0.25 && _frameTimes.Count > 0)
        {
            var averageFrameTime = _frameTimes.Average();
            var fps = averageFrameTime > 0 ? 1.0 / averageFrameTime : 0;
            InfoBar.FpsCounter.Content = $"{fps:F0} FPS";
            _fpsUpdateElapsed = TimeSpan.Zero;
        }

        return Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (NodeIdMatches(InfoBar.MenuButton, message.NodeId))
        {
            burgerMenuService.ToggleMenu();
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
}