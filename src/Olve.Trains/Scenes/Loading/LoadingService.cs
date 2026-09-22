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
    AssetPrewarmService assetPrewarmService,
    GameLoadService gameLoadService,
    GuiElementService guiElementService,
    GuiAnchorService guiAnchorService,
    ILogger<LoadingService> logger) : ISceneService
{
    public int Priority => 100;

    private static readonly Layouts.LoadingScreen LoadingScreen = Layouts.BuildLoadingScreen();

    private Id<GuiAnchor> _anchorId;
    private Id<GuiElementRegistrations> _registrationId;
    private Task<Result<GameSceneArguments>>? _loadingTask;

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

        _loadingTask = Task.Run(() =>
        {
            // Background thread: warm the CPU asset caches and build the game scene arguments (including reading
            // a save). The game scenes themselves load on the main thread in TransitionToGame.
            assetPrewarmService.PrewarmAll();

            return BuildGameSceneArguments(parameterService.Arguments);
        });

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
            var result = _loadingTask.Result;
            _loadingTask = null;

            if (result.TryPickProblems(out var problems, out var arguments))
            {
                logger.LogError("Loading failed: {Problems}", problems);
                return TransitionToMainMenu();
            }

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

        if (sceneManager.UnloadScene(SceneIds.LoadingScene.Id)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.GameUIScene, SceneIds.GameLogicScene.With(arguments));
    }

    private Result TransitionToMainMenu()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.UnloadScene(SceneIds.LoadingScene.Id)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.MainMenuScene);
    }

    private Result<GameSceneArguments> BuildGameSceneArguments(LoadingSceneArguments args)
    {
        // No save path: start a fresh game from the default arguments.
        if (string.IsNullOrWhiteSpace(args.SaveFilePath))
        {
            return new GameSceneArguments();
        }

        return gameLoadService.BuildGameSceneArguments(Path.Create(args.SaveFilePath));
    }
}
