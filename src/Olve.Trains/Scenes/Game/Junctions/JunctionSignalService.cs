using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionSignalService(ILoggingManager loggingManager, JunctionService junctionService) : SceneService(loggingManager), IEntityService<Junction>
{
    private readonly HashSet<Id<Junction>> _signalJunctions = [];

    private readonly EventQueue<Id<Junction>> _junctionConnectionQueue =
        new(junctionService.OnJunctionConnectionsUpdated);

    public Event<Id<Junction>> OnAdded { get; } = new();
    public Event<Id<Junction>> OnRemoved { get; } = new();

    public IEnumerable<Id<Junction>> SignalJunctions => _signalJunctions;
    public Result<bool> JunctionHasSignal(Id<Junction> junctionId) => _signalJunctions.Contains(junctionId);

    protected override Result OnLoad()
    {
        _junctionConnectionQueue.SetHandler(OnJunctionConnectionsChanged).Init();
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        _junctionConnectionQueue.Cleanup();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime) => _junctionConnectionQueue.Update();

    private Result OnJunctionConnectionsChanged(Id<Junction> junctionId)
    {
        if (!junctionService.Exists(junctionId))
        {
            LoggingManager.Log(LogLevel.Warning, "OnAdded called on non-existant junction id");
            return Result.Success();
        }

        var connections = junctionService.GetConnections(junctionId);

        var shouldHaveSignal = connections.Count >= 3;
        var hasSignal = _signalJunctions.Contains(junctionId);

        if (shouldHaveSignal && !hasSignal) return AddSignal(junctionId);
        if (!shouldHaveSignal && hasSignal) return RemoveSignal(junctionId);

        return Result.Success();
    }

    private Result AddSignal(Id<Junction> junctionId)
    {
        if (!_signalJunctions.Add(junctionId))
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to add signal for junction '{junctionId}' but it already exists");
            return Result.Success();
        }
        
        LoggingManager.Log(LogLevel.Info, $"Added signal for junction '{junctionId}'");
        OnAdded.Invoke(junctionId);
        return Result.Success();
    }

    private Result RemoveSignal(Id<Junction> junctionId)
    {
        if (!_signalJunctions.Remove(junctionId))
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to remove signal for junction '{junctionId}' but it was not found");
            return Result.Success();
        }
        
        LoggingManager.Log(LogLevel.Info, $"Removed signal for junction '{junctionId}'");
        OnAdded.Invoke(junctionId);
        return Result.Success();
    }
}