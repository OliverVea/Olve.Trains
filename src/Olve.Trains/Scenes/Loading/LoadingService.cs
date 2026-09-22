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
    private Task<Result<PreparedScene>>? _loadingTask;

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

        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        _loadingTask = Task.Run(() =>
        {
            // Background thread: warm CPU asset caches, then build the game scene arguments and run the
            // GameLogicScene's DI scope creation + parameter service + service Load() calls. All of this is
            // CPU-only and touches no GPU/OpenGL state. GPU work (rendering/UI scene loads) is finalized on
            // the main thread in TransitionToGame once this completes.
            assetPrewarmService.PrewarmAll();

            if (BuildGameSceneArguments(parameterService.Arguments).TryPickProblems(out var problems, out var arguments))
            {
                return problems;
            }

            return sceneManager.PrepareScene(SceneIds.GameLogicScene.Id, SceneIds.GameLogicScene.With(arguments));
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

            if (result.TryPickProblems(out var problems, out var prepared))
            {
                logger.LogError("Loading failed: {Problems}", problems);
                return TransitionToMainMenu();
            }

            return TransitionToGame(prepared);
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

    private Result TransitionToGame(PreparedScene preparedGameLogic)
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.LoadingScene.Id)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        // Register the GameLogicScene loaded on the background thread, then load + activate the remaining
        // scenes on the main thread. GameRenderingScene/GameUIScene reuse the GameLogicScene scope and perform
        // their GPU work (shader/framebuffer/buffer creation) here, where the GL context lives.
        if (sceneManager.CommitPreparedScene(preparedGameLogic).TryPickProblems(out problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.GameUIScene);
    }

    private Result TransitionToMainMenu()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.LoadingScene.Id)
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
