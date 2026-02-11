using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionSignalService(ILogger<JunctionSignalService> logger, JunctionService junctionService) : ISceneService, IEntityService<Junction>
{
    private readonly HashSet<Id<Junction>> _signalJunctions = [];

    private readonly EventQueue<Id<Junction>> _junctionConnectionQueue =
        new(junctionService.OnJunctionConnectionsUpdated);

    public Event<Id<Junction>> OnAdded { get; } = new();
    public Event<Id<Junction>> OnRemoved { get; } = new();

    public IEnumerable<Id<Junction>> SignalJunctions => _signalJunctions;
    public Result<bool> JunctionHasSignal(Id<Junction> junctionId) => _signalJunctions.Contains(junctionId);

    public Result Load()
    {
        _junctionConnectionQueue.SetHandler(OnJunctionConnectionsChanged).Init();
        return Result.Success();
    }

    public Result Unload()
    {
        _junctionConnectionQueue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime) => _junctionConnectionQueue.Update();

    private Result OnJunctionConnectionsChanged(Id<Junction> junctionId)
    {
        if (!junctionService.Exists(junctionId))
        {
            logger.LogWarning("OnAdded called on non-existant junction id");
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
            logger.LogWarning("Tried to add signal for junction '{JunctionId}' but it already exists", junctionId);
            return Result.Success();
        }

        logger.LogInformation("Added signal for junction '{JunctionId}'", junctionId);
        OnAdded.Invoke(junctionId);
        return Result.Success();
    }

    private Result RemoveSignal(Id<Junction> junctionId)
    {
        if (!_signalJunctions.Remove(junctionId))
        {
            logger.LogWarning("Tried to remove signal for junction '{JunctionId}' but it was not found", junctionId);
            return Result.Success();
        }

        logger.LogInformation("Removed signal for junction '{JunctionId}'", junctionId);
        OnAdded.Invoke(junctionId);
        return Result.Success();
    }
}