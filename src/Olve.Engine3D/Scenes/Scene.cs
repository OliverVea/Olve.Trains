using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene<TProvider> : IScene where TProvider : class, ISceneServicesProvider
{
    private readonly string _name =  $"Scene<{typeof(TProvider).Name}>";
    
    private readonly ILoggingManager _loggingManager;
    private readonly SceneService[] _sceneServices;
    private readonly Result[] _serviceResults;

    public Scene(ILoggingManager loggingManager, TProvider provider, Id<IScene> sceneId, int layerOrder = 0)
    {
        Id = sceneId;
        LayerOrder = layerOrder;

        _loggingManager = loggingManager;
        _sceneServices = provider.GetSceneServices().OrderBy(x => x.Priority).ToArray();
        _serviceResults = new Result[_sceneServices.Length];
    }
    
    public Id<IScene> Id { get; }
    public int LayerOrder { get; }
    
    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;


    public Result Load()
    {
        _loggingManager.Log(LogLevel.Debug, $"Loading scene: {_name}");
        
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Load();
        }
        
        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }
        
        _loggingManager.Log(LogLevel.Debug, $"Finished loading scene: {_name}");
        
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