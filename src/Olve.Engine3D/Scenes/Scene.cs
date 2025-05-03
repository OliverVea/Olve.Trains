using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene<TProvider> : IScene where TProvider : class, ISceneServicesProvider
{
    private readonly SceneService[] _sceneServices;
    private readonly Result[] _serviceResults;

    public Scene(TProvider provider, Id<IScene> sceneId, int layerOrder = 0)
    {
        Id = sceneId;
        LayerOrder = layerOrder;
        
        _sceneServices = provider.GetSceneServices().OrderBy(x => x.Priority).ToArray();
        _serviceResults = new Result[_sceneServices.Length];
    }
    
    public Id<IScene> Id { get; }
    public int LayerOrder { get; }
    
    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;


    public Result Load()
    {
        for (var i = 0; i < _sceneServices.Length; i++)
        {
            _serviceResults[i] = _sceneServices[i].Load();
        }
        
        if (_serviceResults.TryPickProblems(out var problems))
        {
            return problems;
        }
        
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