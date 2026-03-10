using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Scenes.GameRendering;

public class WagonRenderingService(
    ILogger<WagonRenderingService> logger,
    MeshLoadingManager meshLoadingManager,
    MeshRenderingService meshRenderingService,
    TrainWagonService trainWagonService,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService,
    WagonPositioningService wagonPositioningService,
    TrackRenderingService trackRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackRenderingService, meshRenderingService]);

    private readonly Dictionary<Id<Wagon>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();

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

        return Result.Success();
    }

    public Result AddWagon(Wagon wagon)
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

    public Result RemoveWagon(Wagon wagon)
    {
        if (!_instanceIds.Remove(wagon.Id, out var instanceHandle))
        {
            return Result.Success();
        }

        return meshRenderingService.RemoveInstance(instanceHandle);
    }

    public Result Update(TimeSpan deltaTime)
    {
        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (wagonPositioningService.GetWagonPositions(trainId, trackPosition)
                .TryPickProblems(out var problems, out var wagonPositions))
            {
                return problems.Prepend("Failed to get wagon positions for train '{0}'", trainId);
            }

            foreach (var wagonPos in wagonPositions)
            {
                if (!_instanceIds.TryGetValue(wagonPos.WagonId, out var instanceHandle))
                {
                    logger.LogWarning(
                        "Did not find rendering instance for wagon with id '{WagonId}'. Enqueueing it for registration",
                        wagonPos.WagonId);

                    var wagons = trainWagonService.GetWagons(trainId);
                    var wagon = wagons.FirstOrDefault(w => w.Id == wagonPos.WagonId);
                    if (wagon != default) AddWagon(wagon);
                    continue;
                }

                if (trackSplineService.GetPosition(wagonPos.TrackId, wagonPos.Time)
                    .TryPickProblems(out problems, out var position))
                {
                    return problems.Prepend("Failed to sample position for wagon '{0}'", wagonPos.WagonId);
                }

                var worldMatrix = TrainWorldMatrix.Compute(wagonPos.Velocity, position);

                if (meshRenderingService.UpdateInstance(instanceHandle, worldMatrix)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to update wagon instance for '{0}'", wagonPos.WagonId);
                }
            }
        }

        return Result.Success();
    }
}
