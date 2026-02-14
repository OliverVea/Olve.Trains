using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.MainMenu;

public class MainMenuService(
    IServiceProvider serviceProvider,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService) : ISceneService
{
    public int Priority => 100;

    private static readonly Layouts.MainMenu MainMenu = Layouts.BuildMainMenu();

    private Id<GuiAnchor> _anchorId;
    private Id<GuiElementRegistrations> _registrationId;

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.Center, GrowthDirection.Center)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);

        return guiElementService
            .RegisterElementAndChildren(_anchorId, MainMenu.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiActivationService.GuiElementActivated.Unsubscribe(OnGuiElementActivated);
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (guiElementService.IsElementNodeId(message.NodeId, MainMenu.StartGameButton, _registrationId))
        {
            TransitionToGame();
        }

        if (guiElementService.IsElementNodeId(message.NodeId, MainMenu.ExitGameButton, _registrationId))
        {
            var gameManager = serviceProvider.GetRequiredService<GameManager>();
            gameManager.Stop();
        }
    }

    private Result TransitionToGame()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.MainMenuScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.GameUIScene);
    }
}
