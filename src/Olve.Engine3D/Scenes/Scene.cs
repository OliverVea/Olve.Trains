using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene<TProvider>(TProvider provider, Id<IScene> sceneId, int layerOrder = 0) : IScene where TProvider : class, ISceneServicesProvider
{
    private readonly IReadOnlyCollection<SceneService> _sceneServices = provider.GetSceneServices().OrderBy(x => x.Priority).ToList();
    
    public Id<IScene> Id => sceneId;
    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;

    public int LayerOrder => layerOrder;

    public Result Load()
    {
        var results = _sceneServices.Select(x => x.Load());
        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }
        
        return Result.Success();
    }

    public Result Unload()
    {
        var results = _sceneServices.Select(x => x.Unload());
        if (results.TryPickProblems(out var problems))
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
        var results = _sceneServices.Select(x => x.Update(deltaTime));
        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }
        
        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        var results = _sceneServices.Select(x => x.Render(deltaTime));
        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }
        
        return Result.Success();
    }
}