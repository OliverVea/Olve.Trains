using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public sealed class Scene(
    ILogger<Scene> logger,
    FaultLogger faultLogger,
    IReadOnlyList<ISceneService> sceneServices,
    Id<IScene> sceneId,
    string name,
    int layerOrder = 0) : IScene
{
    private const string CriticalProblemMessage = "Critical problem in {0}";

    private readonly ServiceEntry[] _services = CreateEntries(sceneServices, name);

    public Id<IScene> Id { get; } = sceneId;
    public int LayerOrder { get; } = layerOrder;

    public SceneLayer Layer => SceneLayer.Main;
    public SceneState State { get; set; } = SceneState.Unloaded;


    public Result Load()
    {
        logger.LogDebug("Loading scene: {SceneName}", name);

        if (RunAllServices(nameof(ISceneService.Load), s => s.Load()).TryPickProblems(out var problems))
        {
            return problems;
        }

        logger.LogDebug("Finished loading scene: {SceneName}", name);

        return Result.Success();
    }

    public Result Unload() => RunAllServices(nameof(ISceneService.Unload), s => s.Unload());

    public Result<Pass> Input() => RunActiveServices(e => e.InputSource, s => s.Input());

    public Result Update() => RunActiveServices(e => e.UpdateSource, s => s.Update().WithValueOnSuccess(Pass.Pass)).ToEmptyResult();

    public Result Render() => RunActiveServices(e => e.RenderSource, s => s.Render().WithValueOnSuccess(Pass.Pass)).ToEmptyResult();

    private Result RunAllServices(string phase, Func<ISceneService, Result> call)
    {
        var results = _services.Select(entry =>
        {
            logger.LogDebug("{Phase} service {Service}", phase, entry.Service.GetType().Name);
            return call(entry.Service);
        }).ToArray();

        return results.TryPickProblems(out var problems) ? problems : Result.Success();
    }

    private Result<Pass> RunActiveServices(Func<ServiceEntry, string> sourceOf, Func<ISceneService, Result<Pass>> call)
    {
        foreach (var entry in _services)
        {
            if (State != SceneState.Active)
            {
                break;
            }

            var source = sourceOf(entry);
            if (call(entry.Service).TryPickProblems(out var problems, out var pass))
            {
                if (problems.AnyCritical())
                {
                    return problems.Prepend(CriticalProblemMessage, source);
                }

                faultLogger.LogFault(source, problems);
                continue;
            }

            faultLogger.LogSuccess(source);

            if (pass == Pass.Block)
            {
                return Result<Pass>.Success(Pass.Block);
            }
        }

        return Result<Pass>.Success(Pass.Pass);
    }

    private static ServiceEntry[] CreateEntries(IReadOnlyList<ISceneService> services, string sceneName) =>
        services
            .OrderBy(x => x.Priority)
            .Select(service => new ServiceEntry(
                service,
                SourceName(service, sceneName, nameof(ISceneService.Input)),
                SourceName(service, sceneName, nameof(ISceneService.Update)),
                SourceName(service, sceneName, nameof(ISceneService.Render))))
            .ToArray();

    private static string SourceName(ISceneService service, string sceneName, string phase) =>
        $"{service.GetType().Name}.{phase} (scene '{sceneName}')";

    private readonly record struct ServiceEntry(
        ISceneService Service,
        string InputSource,
        string UpdateSource,
        string RenderSource);
}
