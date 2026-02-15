using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

public class JunctionSignalService(ILogger<JunctionSignalService> logger, JunctionService junctionService)
{
    private readonly HashSet<Id<Junction>> _signalJunctions = [];

    public Event<Id<Junction>> OnJunctionAdded { get; } = new();
    public Event<Id<Junction>> OnJunctionRemoved { get; } = new();

    public IEnumerable<Id<Junction>> SignalJunctions => _signalJunctions;
    public Result<bool> JunctionHasSignal(Id<Junction> junctionId) => _signalJunctions.Contains(junctionId);

    public Result EvaluateSignal(Id<Junction> junctionId)
    {
        if (!junctionService.JunctionExists(junctionId))
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
        OnJunctionAdded.Invoke(junctionId);
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
        OnJunctionRemoved.Invoke(junctionId);
        return Result.Success();
    }
}
