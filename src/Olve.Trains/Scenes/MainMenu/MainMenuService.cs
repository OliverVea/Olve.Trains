using Microsoft.Extensions.DependencyInjection;
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
    private bool _startGame;

    public Result Load()
    {
        MainMenu.StartGameText.Content = "Start Game";

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

    public Result Update(TimeSpan deltaTime)
    {
        if (!_startGame) return Result.Success();
        _startGame = false;

        return TransitionToGame();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!guiElementService.TryGetGuiNodeId(MainMenu.StartGameButton.Id, _registrationId, out var buttonNodeId))
        {
            return;
        }

        if (message.NodeId == buttonNodeId)
        {
            _startGame = true;
        }
    }

    private Result TransitionToGame()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateScene(SceneIds.MainMenuScene).TryPickProblems(out var problems))
        {
            return problems;
        }

        if (sceneManager.UnloadScene(SceneIds.MainMenuScene).TryPickProblems(out problems))
        {
            return problems;
        }

        Id<IScene>[] gameScenes =
        [
            SceneIds.GameLogicScene,
            SceneIds.GameRenderingScene,
            SceneIds.GameUIScene,
        ];

        foreach (var sceneId in gameScenes)
        {
            if (sceneManager.LoadScene(sceneId).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to load scene '{0}'", sceneId);
            }

            if (sceneManager.ActivateScene(sceneId).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to activate scene '{0}'", sceneId);
            }
        }

        return Result.Success();
    }
}
