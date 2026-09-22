using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Saves;
using Olve.Trains.Scenes.Loading;

namespace Olve.Trains.Scenes.MainMenu;

public class MainMenuService(
    IServiceProvider serviceProvider,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    SaveFileStore saveFileStore,
    ILogger<MainMenuService> logger) : ISceneService
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
            TransitionToGame(new LoadingSceneArguments());
        }

        if (guiElementService.IsElementNodeId(message.NodeId, MainMenu.LoadGameButton, _registrationId))
        {
            LoadQuicksave();
        }

        if (guiElementService.IsElementNodeId(message.NodeId, MainMenu.ExitGameButton, _registrationId))
        {
            var gameManager = serviceProvider.GetRequiredService<GameManager>();
            gameManager.Stop();
        }
    }

    private void LoadQuicksave()
    {
        // GameLogicScene (and its load-game command handler) is not loaded at the main menu, so resolve the
        // quicksave directly and route through the loading scene the same way Start Game does.
        if (saveFileStore.ResolvePath(SaveKind.Quicksave, "quicksave").TryPickProblems(out var problems, out var path))
        {
            logger.LogWarning("Could not resolve quicksave path: {Problems}", problems);
            return;
        }

        if (!path.Exists())
        {
            logger.LogWarning("No quicksave to load at '{Path}'.", path.Path);
            return;
        }

        TransitionToGame(new LoadingSceneArguments(path.Path));
    }

    private Result TransitionToGame(LoadingSceneArguments arguments)
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.MainMenuScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.LoadingScene.Id, SceneIds.LoadingScene.With(arguments));
    }
}
