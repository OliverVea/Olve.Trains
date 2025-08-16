using Olve.Logging;

namespace Olve.Engine3D.Scenes;

public abstract class SceneService
{
    private const string TemplateBase = ": '{0}' [{1}] => {2}";
    private const string LoadTemplate = nameof(Load) + TemplateBase;
    private const string UnloadTemplate = nameof(Unload) + TemplateBase;
    private const string InputTemplate = nameof(Input) + TemplateBase;
    private const string UpdateTemplate = nameof(Update) + TemplateBase;
    private const string RenderTemplate = nameof(Render) + TemplateBase;
    
    private readonly string[] _loadTags;
    private readonly string[] _unloadTags;
    private readonly string[] _inputTags;
    private readonly string[] _updateTags;
    private readonly string[] _renderTags;
    
    protected readonly ILoggingManager LoggingManager;
    
    private string TypeName => GetType().Name;
    private bool _doNotLog;

    protected SceneService(ILoggingManager loggingManager)
    {
        LoggingManager = loggingManager;
        
        _loadTags = [TypeName, nameof(Load)];
        _unloadTags = [TypeName, nameof(Unload)];
        _inputTags = [TypeName, nameof(Input)];
        _updateTags = [TypeName, nameof(Update)];
        _renderTags = [TypeName, nameof(Render)];
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
        var result = OnInput(deltaTime);
        //LogEvent(InputTemplate, result, _inputTags);
        return result;
    }

    public Result Update(TimeSpan deltaTime)
    {
        _doNotLog = false;
        var result = OnUpdate(deltaTime);
        //LogEvent(UpdateTemplate, result, _updateTags);
        return result;
    }

    public Result Render(TimeSpan deltaTime)
    {
        _doNotLog = false;
        var result = OnRender(deltaTime);
        //LogEvent(RenderTemplate, result, _renderTags);
        return result;
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
}