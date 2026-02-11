using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene(
    ILoggingManager loggingManager,
    IEnumerable<SceneService> sceneServices,
    Id<IScene> sceneId,
    string name,
    int layerOrder = 0) : IScene
{
    private readonly SceneService[] _sceneServices = sceneServices.OrderBy(x => x.Priority).ToArray();
    private readonly Result[] _serviceResults = new Result[sceneServices.Count()];

    public Id<IScene> Id { get; } = sceneId;
    public int LayerOrder { get; } = layerOrder;

    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;


    public Result Load()
    {
        loggingManager.Log(LogLevel.Debug, $"Loading scene: {name}");

        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Load();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        loggingManager.Log(LogLevel.Debug, $"Finished loading scene: {name}");

        return Result.Success();
    }

    public Result Unload()
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Unload();
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        foreach (var sceneService in _sceneServices)
        {
            var result = sceneService.Input(deltaTime);
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

    public Result Update(TimeSpan deltaTime)
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Update(deltaTime);
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Render(deltaTime);
        }

        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}
