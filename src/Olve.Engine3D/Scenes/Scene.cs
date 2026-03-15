using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene(
    ILogger<Scene> logger,
    IEnumerable<ISceneService> sceneServices,
    Id<IScene> sceneId,
    string name,
    int layerOrder = 0) : IScene
{
    private readonly ISceneService[] _sceneServices = sceneServices.OrderBy(x => x.Priority).ToArray();
    private readonly Result[] _serviceResults = new Result[sceneServices.Count()];

    public Id<IScene> Id { get; } = sceneId;
    public int LayerOrder { get; } = layerOrder;

    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;


    public Result Load()
    {
        logger.LogDebug("Loading scene: {SceneName}", name);

        for (var i = 0; i < _sceneServices.Length; i++)
        {
            logger.LogDebug("Loading service {Service}", _sceneServices[i].GetType().Name);
            _serviceResults[i] = _sceneServices[i].Load();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        logger.LogDebug("Finished loading scene: {SceneName}", name);

        return Result.Success();
    }

    public Result Unload()
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            logger.LogDebug("Unloading service {Service}", _sceneServices[i].GetType().Name);
            _serviceResults[i] = _sceneServices[i].Unload();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        foreach (var sceneService in _sceneServices)
        {
            var result = sceneService.Input();
            if (result.TryPickProblems(out var problems, out var pass))
            {
                return problems;
            }

            if (pass != Pass.Pass)
            {
                return Result<Pass>.Success(pass);
            }
        }

        return Result<Pass>.Success(Pass.Pass);
    }

    public Result Update()
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            if (State != SceneState.Active)
            {
                break;
            }

            _serviceResults[i] = _sceneServices[i].Update();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result Render()
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            if (State != SceneState.Active)
            {
                break;
            }

            _serviceResults[i] = _sceneServices[i].Render();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}
