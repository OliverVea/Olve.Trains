using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Scenes.GameRendering;

public class TrainRenderingService(
    ILogger<TrainRenderingService> logger,
    MeshLoadingManager meshLoadingManager,
    MeshRenderingService meshRenderingService,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService,
    TrackRenderingService trackRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackRenderingService, meshRenderingService]);

    private readonly Dictionary<Id<Train>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();

    private MeshRenderingService.MeshGroupHandle _groupHandle;

    public Result Load()
    {
        if (meshLoadingManager.LoadMesh(Meshes.SM_Veh_Freight_01)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load train mesh");
        }

        if (meshRenderingService.RegisterMeshGroup(meshId, Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register train mesh group");
        }

        _groupHandle = groupHandle;

        return Result.Success();
    }

    public Result AddTrain(Id<Train> trainId)
    {
        if (_instanceIds.ContainsKey(trainId))
        {
            return new ResultProblem("Tried to add train with id '{0}' twice.", trainId);
        }

        if (meshRenderingService.AddInstance(_groupHandle, new Matrix4X4<float>())
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add train instance for '{0}'", trainId);
        }

        _instanceIds[trainId] = instanceHandle;
        return Result.Success();
    }

    public Result RemoveTrain(Id<Train> trainId)
    {
        if (!_instanceIds.Remove(trainId, out var instanceHandle))
        {
            return Result.Success();
        }

        return meshRenderingService.RemoveInstance(instanceHandle);
    }

    public Result Update(TimeSpan deltaTime)
    {
        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (!_instanceIds.TryGetValue(trainId, out var instanceHandle))
            {
                logger.LogWarning("Did not find rendering instance for train with id '{TrainId}'. Enqueueing it for registration", trainId);
                AddTrain(trainId);
                continue;
            }

            if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time).TryPickProblems(out var problems, out var position))
            {
                return problems.Prepend("Failed to sample point with t '{0}' on track with id '{1}' for train with id '{2}'", trackPosition.Time, trackPosition.TrackId, trainId);
            }

            var worldMatrix = TrainWorldMatrix.Compute(trackPosition.Velocity, position);

            if (meshRenderingService.UpdateInstance(instanceHandle, worldMatrix)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to update train instance for '{0}'", trainId);
            }
        }

        return Result.Success();
    }
}
