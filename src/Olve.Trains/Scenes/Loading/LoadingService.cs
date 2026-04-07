using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Scenes.GameLogic;

namespace Olve.Trains.Scenes.Loading;

public class LoadingService(
    IServiceProvider serviceProvider,
    LoadingSceneParameterService parameterService,
    GuiElementService guiElementService,
    GuiAnchorService guiAnchorService,
    ILogger<LoadingService> logger) : ISceneService
{
    public int Priority => 100;

    private static readonly Layouts.LoadingScreen LoadingScreen = Layouts.BuildLoadingScreen();

    private Id<GuiAnchor> _anchorId;
    private Id<GuiElementRegistrations> _registrationId;
    private Task<GameSceneArguments>? _loadingTask;

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.Center, GrowthDirection.Center)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        if (guiElementService.RegisterElementAndChildren(_anchorId, LoadingScreen.Root)
            .TryPickProblems(out problems, out _registrationId))
        {
            return problems;
        }

        _loadingTask = Task.Run(() => BuildGameSceneArguments(parameterService.Arguments));

        return Result.Success();
    }

    public Result Update()
    {
        if (_loadingTask is null)
        {
            return Result.Success();
        }

        if (_loadingTask.IsCompletedSuccessfully)
        {
            var arguments = _loadingTask.Result;
            _loadingTask = null;

            return TransitionToGame(arguments);
        }

        if (_loadingTask.IsFaulted)
        {
            logger.LogError(_loadingTask.Exception, "Loading failed");
            _loadingTask = null;

            return TransitionToMainMenu();
        }

        return Result.Success();
    }

    public Result Unload()
    {
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    private Result TransitionToGame(GameSceneArguments arguments)
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.LoadingScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(
            SceneIds.GameUIScene,
            SceneIds.GameLogicScene,
            arguments);
    }

    private Result TransitionToMainMenu()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.LoadingScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.MainMenuScene);
    }

    private static GameSceneArguments BuildGameSceneArguments(LoadingSceneArguments args)
    {
        return new GameSceneArguments();
    }
}
