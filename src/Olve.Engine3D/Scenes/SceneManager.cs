using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Stores;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;
using Olve.Utilities.Stores;

namespace Olve.Engine3D.Scenes;

public class SceneManager
{
    private static readonly Comparer<LoadedScene> LoadedSceneComparer = Comparer<LoadedScene>.Create((a, b) =>
    {
        var byLayerOrder = a.Scene.LayerOrder.CompareTo(b.Scene.LayerOrder);
        return byLayerOrder != 0 ? byLayerOrder : Comparer<SceneLayer>.Default.Compare(a.Scene.Layer, b.Scene.Layer);
    });
    
    private readonly ILogger<SceneManager> _logger;
    private readonly ILogger<Scene> _sceneLogger;
    private readonly Dictionary<Id<IScene>, SceneDefinition> _definitions;
    private readonly IServiceProvider _rootProvider;
    private readonly FaultLogger _faultLogger;
    private readonly EntityStore<LoadedScene, Id<IScene>> _loadedScenes;
    private readonly OrderedEntityStoreValueCache<LoadedScene, Id<IScene>> _orderedLoadedScenes;

    public SceneManager(IServiceProvider rootProvider,
        IEnumerable<SceneDefinition> definitions,
        SceneScopeAccessor sceneScopeAccessor,
        FaultLogger faultLogger,
        ILoggerFactory loggerFactory)
    {
        _rootProvider = rootProvider;
        _faultLogger = faultLogger;
        _logger = loggerFactory.CreateLogger<SceneManager>();
        _sceneLogger = loggerFactory.CreateLogger<Scene>();
        _definitions = definitions.ToDictionary(x => x.Id);
        _loadedScenes = [];
        _orderedLoadedScenes = _loadedScenes.BuildOrderedValueCache(LoadedSceneComparer);

        sceneScopeAccessor.Source = () => _loadedScenes.List()
            .Where(loadedScene => loadedScene.Scene.State == SceneState.Active)
            .Select(loadedScene => loadedScene.ServiceScope)
            .Distinct()
            .Select(scope => scope.ServiceProvider);
    }

    private Result<LoadedScene> CreateLoadedScene(Id<IScene> sceneId, SceneArguments[] arguments)
    {
        if (!_definitions.TryGetValue(sceneId, out var definition))
        {
            return new ResultProblem("Scene definition with id '{0}' does not exist", sceneId);
        }
        
        _logger.LogDebug("Creating scene '{SceneName}' (id: {SceneId})", definition.Name, sceneId);

        IServiceScope? scope = null;
        
        if (definition.ParentId is { } parentId)
        {
            if (LoadScene(parentId, arguments).TryPickProblems(out var parentProblems))
            {
                return parentProblems.Prepend("Failed to load parent scene '{0}' for scene '{1}'", parentId, sceneId);
            }

            if (!_loadedScenes.TryGet(parentId, out var parentScene))
            {
                return new ResultProblem("Could not get parent scene with id '{0}' after it was initialized", parentId);
            }

            scope = parentScene.ServiceScope;
        }
        
        scope ??= _rootProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        var sceneServices = sp.GetKeyedServices<ISceneService>(sceneId).ToArray();

        var scene = new Scene(_sceneLogger, _faultLogger, sceneServices, sceneId, definition.Name, definition.LayerOrder);

        return new LoadedScene(scene, scope, definition.ParentId);
    }

    private Result LoadScene(Id<IScene> sceneId, SceneArguments[] arguments)
    {
        if (_loadedScenes.Contains(sceneId))
        {
            return Result.Success();
        }

        if (CreateLoadedScene(sceneId, arguments).TryPickProblems(out var creationProblems, out var loadedScene))
        {
            return creationProblems;
        }

        loadedScene.Scene.State = SceneState.Inactive;

        if (ApplyArguments(sceneId, loadedScene.ServiceScope.ServiceProvider, arguments)
            .TryPickProblems(out var argumentProblems))
        {
            ReleaseScope(loadedScene);
            return argumentProblems.Prepend("Error occurred while loading scene '{0}'", sceneId);
        }
        
        if (loadedScene.Scene.Load().TryPickProblems(out var loadProblems))
        {
            if (loadedScene.Scene.Unload().TryPickProblems(out var unloadProblems))
            {
                loadProblems = ResultProblemCollection.Merge(loadProblems,
                    unloadProblems.Prepend("Got problems when unloading scene during cleanup"));
            }
            ReleaseScope(loadedScene);
            return loadProblems.Prepend("Error occurred while loading scene '{0}'", sceneId);
        }

        _loadedScenes.Set(loadedScene);

        return Result.Success();
    }

    private static void ReleaseScope(LoadedScene loadedScene)
    {
        if (loadedScene.OwnsScope)
        {
            loadedScene.ServiceScope.Dispose();
        }
    }

    public Result UnloadScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGet(sceneId, out var loadedScene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        _logger.LogDebug("Unloading scene '{SceneId}'", sceneId);

        ResultProblemCollection teardownProblems = [];

        if (DeactivateScene(sceneId).TryPickProblems(out var problems))
        {
            teardownProblems = teardownProblems.Append(problems);
        }

        if (ForEachChild(sceneId, UnloadScene).TryPickProblems(out problems))
        {
            teardownProblems = teardownProblems.Append(problems);
        }

        if (loadedScene.Scene.Unload().TryPickProblems(out problems))
        {
            teardownProblems = teardownProblems.Append(problems);
        }
        
        loadedScene.Scene.State = SceneState.Unloaded;

        if (_loadedScenes.Delete(sceneId).TryPickProblems(out problems))
        {
            teardownProblems = teardownProblems.Append(problems);
        }
        
        ReleaseScope(loadedScene);

        return teardownProblems.Any() ? teardownProblems.Prepend("Failed while unloading scene '{0}'", sceneId) : Result.Success();
    }

    private Result ForEachChild(Id<IScene> sceneId, Func<Id<IScene>, Result> action)
    {
        ResultProblemCollection problems = [];

        foreach (var child in _loadedScenes.Where(x => x.ParentId == sceneId))
        {
            if (action(child.Id).TryPickProblems(out var childProblems))
            {
                problems = problems.Append(childProblems);
            }
        }

        return problems.Any() ? problems : Result.Success();
    }

    private Result ActivateScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGet(sceneId, out var loadedScene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        if (loadedScene.Scene.State == SceneState.Active)
        {
            return Result.Success();
        }

        if (loadedScene.ParentId is { } parentId && ActivateScene(parentId).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to activate parent '{0}' of scene '{1}'", parentId, sceneId);
        }

        loadedScene.Scene.State = SceneState.Active;
        EngineMetrics.ActiveScenes.Add(1);

        return Result.Success();
    }

    private Result DeactivateScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGet(sceneId, out var loadedScene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        if (loadedScene.Scene.State != SceneState.Active)
        {
            return Result.Success();
        }

        var childProblems = ForEachChild(sceneId, DeactivateScene);

        loadedScene.Scene.State = SceneState.Inactive;
        EngineMetrics.ActiveScenes.Add(-1);

        return childProblems.TryPickProblems(out var problems)
            ? problems.Prepend("Failed while deactivating scene '{0}'", sceneId)
            : Result.Success();
    }

    public Result LoadAndActivateScene(Id<IScene> sceneId, params SceneArguments[] arguments)
    {
        var branchRoot = TopmostUnloadedInLineage(sceneId);

        if (!LoadScene(sceneId, arguments).TryPickProblems(out var problems) &&
            !ActivateScene(sceneId).TryPickProblems(out problems))
        {
            return Result.Success();
        }

        if (branchRoot is { } rootId && _loadedScenes.Contains(rootId) &&
            UnloadScene(rootId).TryPickProblems(out var rollbackProblems))
        {
            problems = ResultProblemCollection.Merge(problems,
                rollbackProblems.Prepend("Failed to roll back scene '{0}'", rootId));
        }

        return problems;
    }

    private Id<IScene>? TopmostUnloadedInLineage(Id<IScene> sceneId)
    {
        Id<IScene>? topmost = null;

        for (Id<IScene>? id = sceneId;
             id is { } current && !_loadedScenes.Contains(current);
             id = _definitions.TryGetValue(current, out var definition) ? definition.ParentId : null)
        {
            topmost = current;
        }

        return topmost;
    }

    private static Result ApplyArguments(Id<IScene> sceneId, IServiceProvider provider, SceneArguments[] arguments)
    {
        foreach (var argument in arguments.Where(argument => argument.SceneId == sceneId))
        {
            if (argument.Apply(provider).TryPickProblems(out var problems))
            {
                return problems.Prepend("Error occurred while loading parameters for scene '{0}'", sceneId);
            }
        }

        return Result.Success();
    }

    public Result Input()
    {
        foreach (var scene in _orderedLoadedScenes.Select(loadedScene => loadedScene.Scene))
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var inputResult = scene.Input();
            if (inputResult.TryPickProblems(out var problems, out var passInput))
            {
                return problems;
            }

            if (passInput == Pass.Block)
            {
                break;
            }
        }

        return Result.Success();
    }

    public Result Update()
    {
        foreach (var scene in _orderedLoadedScenes.Select(loadedScene => loadedScene.Scene))
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            Stopwatch? sw = EngineMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            var updateResult = scene.Update();
            if (updateResult.TryPickProblems(out var problems))
            {
                return problems;
            }

            if (sw is not null)
            {
                sw.Stop();
                var tags = new TagList { { "scene.name", GetSceneName(scene.Id) } };
                EngineMetrics.SceneUpdateDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
            }
        }

        return Result.Success();
    }

    public Result Render()
    {
        foreach (var scene in _orderedLoadedScenes.Select(loadedScene => loadedScene.Scene))
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            Stopwatch? sw = EngineMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            var renderResult = scene.Render();
            if (renderResult.TryPickProblems(out var problems))
            {
                return problems;
            }

            if (sw is not null)
            {
                sw.Stop();
                var tags = new TagList { { "scene.name", GetSceneName(scene.Id) } };
                EngineMetrics.SceneRenderDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
            }
        }

        return Result.Success();
    }

    private string GetSceneName(Id<IScene> sceneId) =>
        _definitions.TryGetValue(sceneId, out var def) ? def.Name : sceneId.ToString();

    public void Close()
    {
        var rootSceneIds = _loadedScenes
            .Where(loadedScene => loadedScene.ParentId is null)
            .Select(loadedScene => loadedScene.Id)
            .ToArray();

        foreach (var rootId in rootSceneIds)
        {
            if (UnloadScene(rootId).TryPickProblems(out var problems))
            {
                _logger.LogError("Problems while closing scene '{SceneId}':{NewLine}{Problems}",
                    rootId, Environment.NewLine, string.Join(Environment.NewLine, problems.Select(p => p.ToDebugString())));
            }
        }
    }
    
    private sealed record LoadedScene(
        IScene Scene,
        IServiceScope ServiceScope,
        Id<IScene>? ParentId)
        : IHasId<Id<IScene>>
    {
        public Id<IScene> Id => Scene.Id;
        public bool OwnsScope => ParentId is null;
    }
}

public class SceneScopeAccessor
{
    internal Func<IEnumerable<IServiceProvider>> Source { get; set; } = () => [];

    public IEnumerable<IServiceProvider> ActiveScopeProviders => Source();
}