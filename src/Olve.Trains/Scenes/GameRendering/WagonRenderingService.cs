using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Scenes.GameRendering;

public class WagonRenderingService(
    ILogger<WagonRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    MeshLoadingManager meshLoadingManager,
    MeshRenderingService meshRenderingService,
    TrainWagonService trainWagonService,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService,
    TrainTrackHistoryService trainTrackHistoryService,
    WagonBlueprintService wagonBlueprintService,
    TrackRenderingService trackRenderingService)
    : ISceneService
{
    private const float Scale = TrainWorldMatrix.TrainScale;
    private const float LocomotiveLength = 1.0f * Scale;
    private const float WagonLength = 1.0f * Scale;
    private const float CouplingGap = 0.1f * Scale;

    public int Priority => SceneServicePriority.FromDependencies([trackRenderingService, meshRenderingService]);

    private readonly Dictionary<Id<Wagon>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();

    private readonly EventQueue<(Id<Train> TrainId, Wagon Wagon)> _toAddQueue =
        eventQueueFactory.Create(trainWagonService.OnWagonAdded);

    private readonly EventQueue<(Id<Train> TrainId, Wagon Wagon)> _toRemoveQueue =
        eventQueueFactory.Create(trainWagonService.OnWagonRemoved);

    private MeshRenderingService.MeshGroupHandle _groupHandle;

    public Result Load()
    {
        if (meshLoadingManager.LoadMesh(Meshes.SM_Veh_Carriage_Container_01)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load wagon mesh");
        }

        if (meshRenderingService.RegisterMeshGroup(meshId, Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register wagon mesh group");
        }

        _groupHandle = groupHandle;

        _toAddQueue.SetHandler(e => AddWagon(e.Wagon)).Init();
        _toRemoveQueue.SetHandler(e => RemoveWagon(e.Wagon)).Init();

        return Result.Success();
    }

    public Result Unload()
    {
        _toAddQueue.Cleanup();
        _toRemoveQueue.Cleanup();

        return Result.Success();
    }

    private Result AddWagon(Wagon wagon)
    {
        if (_instanceIds.ContainsKey(wagon.Id))
        {
            return new ResultProblem("Tried to add wagon with id '{0}' twice.", wagon.Id);
        }

        if (meshRenderingService.AddInstance(_groupHandle, new Matrix4X4<float>())
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add wagon instance for '{0}'", wagon.Id);
        }

        _instanceIds[wagon.Id] = instanceHandle;
        return Result.Success();
    }

    private Result RemoveWagon(Wagon wagon)
    {
        if (!_instanceIds.Remove(wagon.Id, out var instanceHandle))
        {
            return Result.Success();
        }

        return meshRenderingService.RemoveInstance(instanceHandle);
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();

        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            var wagons = trainWagonService.GetWagons(trainId);
            if (wagons.Count == 0) continue;

            var cumulativeOffset = LocomotiveLength / 2f + CouplingGap;

            for (var i = 0; i < wagons.Count; i++)
            {
                var wagon = wagons[i];

                var wagonLength = WagonLength;
                if (wagonBlueprintService.TryGet(wagon.BlueprintId, out var blueprint))
                {
                    wagonLength = blueprint.Length * Scale;
                }

                var offset = cumulativeOffset + wagonLength / 2f;

                if (!_instanceIds.TryGetValue(wagon.Id, out var instanceHandle))
                {
                    logger.LogWarning(
                        "Did not find rendering instance for wagon with id '{WagonId}'. Enqueueing it for registration",
                        wagon.Id);
                    AddWagon(wagon);
                    cumulativeOffset += wagonLength + CouplingGap;
                    continue;
                }

                if (ComputeWagonPosition(trainId, trackPosition, offset)
                    .TryPickProblems(out var problems, out var wagonPos))
                {
                    return problems.Prepend("Failed to compute wagon position for wagon '{0}'", wagon.Id);
                }

                if (trackSplineService.GetPosition(wagonPos.TrackId, wagonPos.Time)
                    .TryPickProblems(out problems, out var position))
                {
                    return problems.Prepend("Failed to sample position for wagon '{0}'", wagon.Id);
                }

                var worldMatrix = TrainWorldMatrix.Compute(wagonPos.Velocity, position);

                if (meshRenderingService.UpdateInstance(instanceHandle, worldMatrix)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to update wagon instance for '{0}'", wagon.Id);
                }

                cumulativeOffset += wagonLength + CouplingGap;
            }
        }

        return Result.Success();
    }

    private Result<(Id<Track> TrackId, float Time, float Velocity)> ComputeWagonPosition(
        Id<Train> trainId,
        TrainTrackPosition locoPosition,
        float offset)
    {
        var trackId = locoPosition.TrackId;
        var velocity = locoPosition.Velocity;

        if (trackSplineService.GetLength(trackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }

        var locoArcDist = locoPosition.Time * trackLength;

        float availableBehind;
        if (velocity > 0)
        {
            availableBehind = locoArcDist;
        }
        else
        {
            availableBehind = trackLength - locoArcDist;
        }

        if (offset <= availableBehind)
        {
            float wagonTime;
            if (velocity > 0)
            {
                wagonTime = (locoArcDist - offset) / trackLength;
            }
            else
            {
                wagonTime = (locoArcDist + offset) / trackLength;
            }

            return (trackId, wagonTime, velocity);
        }

        var overflow = offset - availableBehind;
        var history = trainTrackHistoryService.GetHistory(trainId);

        return WalkHistory(trackId, velocity, overflow, history);
    }

    private Result<(Id<Track> TrackId, float Time, float Velocity)> WalkHistory(
        Id<Track> currentTrackId,
        float currentVelocity,
        float overflow,
        IReadOnlyList<TrainTrackHistoryService.TrackHistoryEntry> history)
    {
        foreach (var entry in history)
        {
            if (trackSplineService.GetLength(entry.TrackId).TryPickProblems(out var problems, out var prevTrackLength))
            {
                return problems;
            }

            if (overflow <= prevTrackLength)
            {
                float wagonTime;
                if (entry.Velocity > 0)
                {
                    wagonTime = (prevTrackLength - overflow) / prevTrackLength;
                }
                else
                {
                    wagonTime = overflow / prevTrackLength;
                }

                return (entry.TrackId, wagonTime, entry.Velocity);
            }

            overflow -= prevTrackLength;
        }

        // History exhausted or no history — clamp to the tail end of the known path
        if (history.Count > 0)
        {
            var lastEntry = history[^1];
            var clampTime = lastEntry.Velocity > 0 ? 0f : 1f;
            return (lastEntry.TrackId, clampTime, lastEntry.Velocity);
        }

        // No history (train just placed) — clamp to current track endpoint
        var clampToStart = currentVelocity > 0 ? 0f : 1f;
        return (currentTrackId, clampToStart, currentVelocity);
    }
}
