using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Scenes.GameRendering;

public class VehicleRenderingService(
    ILogger<VehicleRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    MeshRenderingService meshRenderingService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService,
    TrackRenderingService trackRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackRenderingService, meshRenderingService]);

    private readonly Dictionary<Id<Vehicle>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();

    private readonly EventQueue<Id<Vehicle>> _toAddQueue = eventQueueFactory.Create(vehicleService.OnVehicleAdded);
    private readonly EventQueue<Id<Vehicle>> _toRemoveQueue = eventQueueFactory.Create(vehicleService.OnVehicleRemoved);

    private MeshRenderingService.MeshGroupHandle _groupHandle;

    public Result Load()
    {
        if (meshRenderingService.RegisterMeshGroup(Meshes.SM_Veh_Bullet_01, Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out var problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register vehicle mesh group");
        }

        _groupHandle = groupHandle;

        _toAddQueue.SetHandler(AddVehicle).Init();
        _toRemoveQueue.SetHandler(RemoveVehicle).Init();

        return Result.Success();
    }

    public Result Unload()
    {
        _toAddQueue.Cleanup();
        _toRemoveQueue.Cleanup();

        return Result.Success();
    }

    private Result AddVehicle(Id<Vehicle> vehicleId)
    {
        if (_instanceIds.ContainsKey(vehicleId))
        {
            return new ResultProblem("Tried to add vehicle with id '{0}' twice.", vehicleId);
        }

        if (meshRenderingService.AddInstance(_groupHandle, new Matrix4X4<float>())
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add vehicle instance for '{0}'", vehicleId);
        }

        _instanceIds[vehicleId] = instanceHandle;
        return Result.Success();
    }

    private Result RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (!_instanceIds.Remove(vehicleId, out var instanceHandle))
        {
            return Result.Success();
        }

        return meshRenderingService.RemoveInstance(instanceHandle);
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();

        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_instanceIds.TryGetValue(vehicleId, out var instanceHandle))
            {
                logger.LogWarning("Did not find rendering instance for vehicle with id '{VehicleId}'. Enqueueing it for registration", vehicleId);
                AddVehicle(vehicleId);
                continue;
            }

            if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time).TryPickProblems(out var problems, out var position))
            {
                return problems.Prepend("Failed to sample point with t '{0}' on track with id '{1}' for vehicle with id '{2}'", trackPosition.Time, trackPosition.TrackId, vehicleId);
            }

            var worldMatrix = Matrix4X4<float>.Identity;
            if (trackPosition.Velocity > 0)
            {
                worldMatrix *= Matrix4X4.CreateRotationY(float.Pi);
            }

            worldMatrix *= position.ToMatrix4X4();

            if (meshRenderingService.UpdateInstance(instanceHandle, worldMatrix)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to update vehicle instance for '{0}'", vehicleId);
            }
        }

        return Result.Success();
    }
}
