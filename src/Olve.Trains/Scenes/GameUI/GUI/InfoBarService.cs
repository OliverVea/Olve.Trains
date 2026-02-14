using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class InfoBarService(
    IServiceProvider serviceProvider,
    DayTimeManager dayTimeManager,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
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
        return Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (NodeIdMatches(InfoBar.MenuButton, message.NodeId))
        {
            TransitionToMainMenu();
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

    private Result TransitionToMainMenu()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.GameLogicScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.MainMenuScene);
    }
}