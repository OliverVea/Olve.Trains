using Olve.Logging;

namespace Olve.Engine3D.Scenes;

public abstract class SceneService
{
    private const string TemplateBase = ": '{0}' [{1}] => {2}";
    private const string LoadTemplate = nameof(Load) + TemplateBase;
    private const string UnloadTemplate = nameof(Unload) + TemplateBase;

    private readonly string[] _loadTags;
    private readonly string[] _unloadTags;

    protected readonly ILoggingManager LoggingManager;

    private string TypeName => GetType().Name;
    private bool _doNotLog;

    protected SceneService(ILoggingManager loggingManager)
    {
        LoggingManager = loggingManager;

        _loadTags = [TypeName, nameof(Load)];
        _unloadTags = [TypeName, nameof(Unload)];
    }

    private void LogEvent(string template, object? result, string[] tags)
    {
        if (_doNotLog)
        {
            return;
        }

        LoggingManager.Log(
            LogLevel.Debug,
            string.Format(template, TypeName, Priority, result),
            tags);
    }

    /// <summary>
    /// Priority of the service. Services with lower priority are executed first.
    /// </summary>
    public virtual int Priority => 0;
    public Result Load()
    {
        _doNotLog = false;
        var result = OnLoad();
        LogEvent(LoadTemplate, result, _loadTags);
        return result;
    }

    public Result Unload()
    {
        _doNotLog = false;
        var result = OnUnload();
        LogEvent(UnloadTemplate, result, _unloadTags);
        return OnUnload();
    }

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        _doNotLog = false;
        return OnInput(deltaTime);
    }

    public Result Update(TimeSpan deltaTime)
    {
        _doNotLog = false;
        return OnUpdate(deltaTime);
    }

    public Result Render(TimeSpan deltaTime)
    {
        _doNotLog = false;
        return OnRender(deltaTime);
    }

    protected virtual Result OnLoad()
    {
        _doNotLog = true;
        return Result.Success();
    }

    protected virtual Result OnUnload()
    {
        _doNotLog = true;
        return Result.Success();
    }

    protected virtual Result<Pass> OnInput(TimeSpan deltaTime)
    {
        _doNotLog = true;
        return Result<Pass>.Success(Pass.Pass);
    }

    protected virtual Result OnUpdate(TimeSpan deltaTime)
    {
        _doNotLog = true;
        return Result.Success();
    }

    protected virtual Result OnRender(TimeSpan deltaTime)
    {
        _doNotLog = true;
        return Result.Success();
    }

    protected static int GetPriorityFromDependencies(IReadOnlyCollection<SceneService> dependencies)
    {
        return dependencies.Max(service => service.Priority) + 1;
    }

    protected static int GetPriorityFromDependents(IReadOnlyCollection<SceneService> dependents)
    {
        return dependents.Min(service => service.Priority) - 1;
    }
}