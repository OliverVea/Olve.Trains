using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public class SceneManager(
    IServiceProvider rootProvider,
    IEnumerable<SceneDefinition> definitions,
    SceneScopeAccessor sceneScopeAccessor,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<SceneManager> _logger = loggerFactory.CreateLogger<SceneManager>();
    private readonly ILogger<Scene> _sceneLogger = loggerFactory.CreateLogger<Scene>();

    private readonly Dictionary<Id<IScene>, SceneDefinition> _definitions =
        definitions.ToDictionary(d => d.Id);

    // Loaded scene tracking
    private readonly Dictionary<Id<IScene>, IScene> _loadedScenes = new();
    private readonly Dictionary<Id<IScene>, List<Id<IScene>>> _children = new();

    // Scope tracking: root scene ID -> scope
    private readonly Dictionary<Id<IScene>, IServiceScope> _scopes = new();
    private readonly Dictionary<Id<IScene>, Id<IScene>> _scopeOwner = new(); // scene -> root scene

    private readonly Dictionary<Id<IScene>, object> _sceneParameters = new();

    private List<IScene>? _orderedCache;

    public Result LoadScene(Id<IScene> sceneId)
    {
        if (!_definitions.TryGetValue(sceneId, out var definition))
        {
            return new ResultProblem("Scene definition with id '{0}' does not exist", sceneId);
        }

        if (_loadedScenes.ContainsKey(sceneId))
        {
            return Result.Success(); // already loaded
        }

        // If has parent and parent not loaded, load parent first
        if (definition.ParentId is { } parentId && !_loadedScenes.ContainsKey(parentId))
        {
            var parentResult = LoadScene(parentId);
            if (parentResult.TryPickProblems(out var parentProblems))
            {
                return parentProblems.Prepend("Failed to load parent scene '{0}' for scene '{1}'", parentId, sceneId);
            }
        }

        // Get or create scope
        IServiceScope scope;
        Id<IScene> rootId;
        if (definition.ParentId is { } pid && _scopeOwner.TryGetValue(pid, out var existingRoot))
        {
            // Child scene: reuse parent's scope
            rootId = existingRoot;
            scope = _scopes[rootId];
        }
        else if (definition.ParentId is null)
        {
            // Root scene: create new scope
            rootId = sceneId;
            scope = rootProvider.CreateScope();
            _scopes[rootId] = scope;
        }
        else
        {
            return new ResultProblem("Parent scene '{0}' is loaded but has no scope owner", definition.ParentId);
        }

        _scopeOwner[sceneId] = rootId;

        // Resolve scene services from the scope
        var sp = scope.ServiceProvider;
        var sceneServices = sp.GetKeyedServices<ISceneService>(sceneId);

        var scene = new Scene(_sceneLogger, sceneServices, sceneId, definition.Name, definition.LayerOrder);

        _logger.LogDebug("Loading scene '{SceneName}' (id: {SceneId})", definition.Name, sceneId);

        // Pass 1: Load parameters (before scene services Load())
        if (_sceneParameters.TryGetValue(sceneId, out var parameters))
        {
            _sceneParameters.Remove(sceneId);

            var parameterServices = sp.GetKeyedServices<ISceneParameterService>(sceneId);
            foreach (var parameterService in parameterServices)
            {
                if (parameterService.LoadParameters(parameters).TryPickProblems(out var paramProblems))
                {
                    // Cleanup on failure
                    _scopeOwner.Remove(sceneId);
                    if (rootId == sceneId)
                    {
                        scope.Dispose();
                        _scopes.Remove(rootId);
                    }

                    return paramProblems.Prepend("Error occurred while loading parameters for scene '{0}'", sceneId);
                }
            }
        }

        // Pass 2: Load scene services
        var loadResult = scene.Load();
        if (loadResult.TryPickProblems(out var problems))
        {
            // Cleanup on failure
            _scopeOwner.Remove(sceneId);
            if (rootId == sceneId)
            {
                scope.Dispose();
                _scopes.Remove(rootId);
            }

            return problems.Prepend("Error occurred while loading scene '{0}'", sceneId);
        }

        scene.State = SceneState.Inactive;
        _loadedScenes[sceneId] = scene;

        // Track parent-child relationship
        if (definition.ParentId is { } parentSceneId)
        {
            _children.GetOrAdd(parentSceneId, () => []).Add(sceneId);
        }

        _orderedCache = null;

        return Result.Success();
    }

    /// <summary>
    /// Performs the thread-safe-on-a-background-thread part of loading a root scene: creates the DI scope,
    /// resolves the scene's services, applies parameters, and runs each service's <see cref="ISceneService.Load"/>.
    /// Does NOT touch any shared <see cref="SceneManager"/> state — call <see cref="CommitPreparedScene"/> on the
    /// main thread to register the result. Only root scenes (no parent) may be prepared this way, because child
    /// scenes reuse their parent's scope, which only exists once the parent has been committed.
    /// </summary>
    public Result<PreparedScene> PrepareScene(Id<IScene> sceneId, object? parameters = null)
    {
        if (!_definitions.TryGetValue(sceneId, out var definition))
        {
            return new ResultProblem("Scene definition with id '{0}' does not exist", sceneId);
        }

        if (definition.ParentId is not null)
        {
            return new ResultProblem(
                "Only root scenes can be prepared off-thread, but scene '{0}' has parent '{1}'",
                sceneId, definition.ParentId);
        }

        if (_loadedScenes.ContainsKey(sceneId))
        {
            return new ResultProblem("Scene with id '{0}' is already loaded", sceneId);
        }

        var scope = rootProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var sceneServices = sp.GetKeyedServices<ISceneService>(sceneId);

        var scene = new Scene(_sceneLogger, sceneServices, sceneId, definition.Name, definition.LayerOrder);

        _logger.LogDebug("Preparing scene '{SceneName}' (id: {SceneId}) off-thread", definition.Name, sceneId);

        // Pass 1: Load parameters (before scene services Load())
        if (parameters is not null)
        {
            var parameterServices = sp.GetKeyedServices<ISceneParameterService>(sceneId);
            foreach (var parameterService in parameterServices)
            {
                if (parameterService.LoadParameters(parameters).TryPickProblems(out var paramProblems))
                {
                    scope.Dispose();
                    return paramProblems.Prepend("Error occurred while loading parameters for scene '{0}'", sceneId);
                }
            }
        }

        // Pass 2: Load scene services
        if (scene.Load().TryPickProblems(out var problems))
        {
            scope.Dispose();
            return problems.Prepend("Error occurred while loading scene '{0}'", sceneId);
        }

        scene.State = SceneState.Inactive;

        return new PreparedScene(sceneId, definition, scope, scene);
    }

    /// <summary>
    /// Registers a scene produced by <see cref="PrepareScene"/> into the manager's tracking state.
    /// Must run on the main thread. After this the scene is loaded and inactive; activate it (and load any
    /// child scenes) via <see cref="LoadAndActivateScene(Id{IScene})"/>.
    /// </summary>
    public Result CommitPreparedScene(PreparedScene prepared)
    {
        var sceneId = prepared.SceneId;

        if (_loadedScenes.ContainsKey(sceneId))
        {
            prepared.Scope.Dispose();
            return new ResultProblem("Scene with id '{0}' is already loaded", sceneId);
        }

        // Prepared scenes are always roots: they own their scope.
        _scopes[sceneId] = prepared.Scope;
        _scopeOwner[sceneId] = sceneId;
        _loadedScenes[sceneId] = prepared.Scene;
        _orderedCache = null;

        return Result.Success();
    }

    public Result UnloadScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGetValue(sceneId, out var scene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        // Unload children first (depth-first cascading)
        if (_children.TryGetValue(sceneId, out var childIds))
        {
            foreach (var childId in childIds.ToArray())
            {
                var childResult = UnloadScene(childId);
                if (childResult.TryPickProblems(out var childProblems))
                {
                    return childProblems.Prepend("Failed to unload child scene '{0}'", childId);
                }
            }

            _children.Remove(sceneId);
        }

        // Deactivate if active
        if (scene.State == SceneState.Active)
        {
            scene.State = SceneState.Inactive;
            EngineMetrics.ActiveScenes.Add(-1);
        }

        _logger.LogDebug("Unloading scene '{SceneId}'", sceneId);

        scene.Unload();
        scene.State = SceneState.Unloaded;
        _loadedScenes.Remove(sceneId);

        // Remove from parent's child list
        if (_definitions.TryGetValue(sceneId, out var definition) && definition.ParentId is { } parentId)
        {
            if (_children.TryGetValue(parentId, out var parentChildren))
            {
                parentChildren.Remove(sceneId);
            }
        }

        // If this is the scope owner (root), dispose the scope
        if (_scopeOwner.TryGetValue(sceneId, out var rootId))
        {
            _scopeOwner.Remove(sceneId);

            if (rootId == sceneId && _scopes.TryGetValue(rootId, out var scope))
            {
                scope.Dispose();
                _scopes.Remove(rootId);
            }
        }

        _orderedCache = null;

        return Result.Success();
    }

    public Result ActivateScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGetValue(sceneId, out var scene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        if (scene.State != SceneState.Inactive)
        {
            return new ResultProblem("Scene with id '{0}' is not inactive (state: {1})", sceneId, scene.State);
        }

        if (_scopeOwner.TryGetValue(sceneId, out var rootId) && _scopes.TryGetValue(rootId, out var scope))
        {
            sceneScopeAccessor.Add(sceneId, scope.ServiceProvider);
        }

        scene.State = SceneState.Active;
        EngineMetrics.ActiveScenes.Add(1);

        return Result.Success();
    }

    public Result DeactivateScene(Id<IScene> sceneId)
    {
        if (!_loadedScenes.TryGetValue(sceneId, out var scene))
        {
            return new ResultProblem("Scene with id '{0}' is not loaded", sceneId);
        }

        if (scene.State != SceneState.Active)
        {
            return new ResultProblem("Scene with id '{0}' is not active (state: {1})", sceneId, scene.State);
        }

        // Deactivate children first
        if (_children.TryGetValue(sceneId, out var childIds))
        {
            foreach (var childId in childIds.ToArray())
            {
                if (_loadedScenes.TryGetValue(childId, out var child) && child.State == SceneState.Active)
                {
                    var childResult = DeactivateScene(childId);
                    if (childResult.TryPickProblems(out var childProblems))
                    {
                        return childProblems.Prepend("Failed to deactivate child scene '{0}'", childId);
                    }
                }
            }
        }

        scene.State = SceneState.Inactive;
        EngineMetrics.ActiveScenes.Add(-1);

        sceneScopeAccessor.Remove(sceneId);

        return Result.Success();
    }

    public Result LoadAndActivateScene(Id<IScene> sceneId)
    {
        // LoadScene auto-loads parents. Collect all scenes that get loaded.
        var loadedInOrder = new List<Id<IScene>>();
        CollectLoadOrder(sceneId, loadedInOrder);

        foreach (var id in loadedInOrder)
        {
            var loadResult = LoadScene(id);
            if (loadResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to load scene '{0}'", id);
            }
        }

        // Activate in parent-first order
        foreach (var id in loadedInOrder)
        {
            if (_loadedScenes.TryGetValue(id, out var scene) && scene.State == SceneState.Inactive)
            {
                var activateResult = ActivateScene(id);
                if (activateResult.TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to activate scene '{0}'", id);
                }
            }
        }

        return Result.Success();
    }

    public Result LoadAndActivateScene(Id<IScene> sceneId, Id<IScene> parameterTargetSceneId, object parameters)
    {
        _sceneParameters[parameterTargetSceneId] = parameters;
        return LoadAndActivateScene(sceneId);
    }

    public Result DeactivateAndUnloadScene(Id<IScene> sceneId)
    {
        if (_loadedScenes.TryGetValue(sceneId, out var scene) && scene.State == SceneState.Active)
        {
            var deactivateResult = DeactivateScene(sceneId);
            if (deactivateResult.TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return UnloadScene(sceneId);
    }

    private void CollectLoadOrder(Id<IScene> sceneId, List<Id<IScene>> result)
    {
        if (result.Contains(sceneId)) return;

        if (_definitions.TryGetValue(sceneId, out var definition) && definition.ParentId is { } parentId)
        {
            CollectLoadOrder(parentId, result);
        }

        result.Add(sceneId);
    }

    private List<IScene> GetOrderedScenes()
    {
        if (_orderedCache != null) return _orderedCache;

        _orderedCache = _loadedScenes.Values
            .OrderBy(x => x.LayerOrder)
            .ThenBy(x => x.Layer)
            .ToList();

        return _orderedCache;
    }

    public Result Input()
    {
        foreach (var scene in GetOrderedScenes())
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            var inputResult = scene.Input();
            if (inputResult.TryPickProblems(out var problems, out var passInput))
            {
                return problems.Prepend("Got problem while updating scene input for scene '{0}'", scene.Id);
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
        foreach (var scene in GetOrderedScenes())
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            Stopwatch? sw = EngineMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            var updateResult = scene.Update();
            if (updateResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Got problem while updating scene for scene '{0}'", scene.Id);
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
        foreach (var scene in GetOrderedScenes())
        {
            if (scene.State != SceneState.Active)
            {
                continue;
            }

            Stopwatch? sw = EngineMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            var renderResult = scene.Render();
            if (renderResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Got problem while rendering scene for scene '{0}'", scene.Id);
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
        // Unload all root scenes (cascading to children)
        var rootSceneIds = _loadedScenes.Keys
            .Where(id => _definitions.TryGetValue(id, out var def) && def.ParentId is null)
            .ToArray();

        foreach (var rootId in rootSceneIds)
        {
            UnloadScene(rootId);
        }
    }
}

/// <summary>
/// An opaque handle to a root scene that has been loaded off the main thread by
/// <see cref="SceneManager.PrepareScene"/> but not yet registered. Pass it to
/// <see cref="SceneManager.CommitPreparedScene"/> on the main thread to finish loading.
/// </summary>
public sealed class PreparedScene
{
    internal Id<IScene> SceneId { get; }
    internal SceneDefinition Definition { get; }
    internal IServiceScope Scope { get; }
    internal Scene Scene { get; }

    internal PreparedScene(Id<IScene> sceneId, SceneDefinition definition, IServiceScope scope, Scene scene)
    {
        SceneId = sceneId;
        Definition = definition;
        Scope = scope;
        Scene = scene;
    }
}

public class SceneScopeAccessor
{
    public IEnumerable<IServiceProvider> ActiveScopeProviders => _providers.Values;

    private readonly Dictionary<Id<IScene>, IServiceProvider> _providers = new();

    public void Add(Id<IScene> sceneId, IServiceProvider provider) => _providers[sceneId] = provider;
    public void Remove(Id<IScene> sceneId) => _providers.Remove(sceneId);
}