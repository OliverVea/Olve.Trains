using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Scenes.GameRendering;

public class VehicleRenderingService(
    ILogger<VehicleRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    SceneLightService sceneLightService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService,
    TrackRenderingService trackRenderingService
    ) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackRenderingService]);

    private readonly Dictionary<Id<Vehicle>, Id<Shaders.Default.Instance>> _instanceIds = new();

    private readonly EventQueue<Id<Vehicle>> _toAddQueue = eventQueueFactory.Create(vehicleService.OnVehicleAdded);
    private readonly EventQueue<Id<Vehicle>> _toRemoveQueue = eventQueueFactory.Create(vehicleService.OnVehicleRemoved);
    private readonly Shaders.Default _shader = new();
    private GroupId<Shaders.Default.Instance> _groupId = null!;
    private float _scale = 1;

    public Result Load()
    {
        // Load texture and get its Id
        if (textureLoadingManager.LoadTexture(Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(textureId, new TextureUploadOptions())
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register texture with OpenGL");
        }

        _shader.TextureSampler = textureId;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Veh_Bullet_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        // Populate typed vertices from mesh data
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        meshData.Populate(vertices);

        // Extract uint[] indices from mesh triangles
        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        // Register geometry with the unified manager
        if (geometryManager.Register<Shaders.Default.Vertex>(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register vehicle geometry");
        }

        // Register rendering group
        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance>(
                geometryId, _shader, RenderState.Opaque)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register vehicle group");
        }

        _groupId = groupId;

        AABB aabbTarget = new(Vector3D<float>.Zero, Vector3D<float>.One);
        var scaleResult = AABBHelper.GetUniformScaleToFitInside(meshData, aabbTarget);
        if (scaleResult.TryPickProblems(out problems, out _scale))
        {
            return problems.Prepend("Failed to compute scale");
        }

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

        if (renderingInstanceManager.Add(_groupId, new Shaders.Default.Instance(new Matrix4X4<float>()))
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add vehicle instance for '{0}'", vehicleId);
        }

        _instanceIds[vehicleId] = instanceId;
        return Result.Success();
    }

    private Result RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (!_instanceIds.Remove(vehicleId, out var instanceId))
        {
            return Result.Success();
        }

        return renderingInstanceManager.Remove(_groupId, instanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();

        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_instanceIds.TryGetValue(vehicleId, out var instanceId))
            {
                logger.LogWarning("Did not find rendering instance for vehicle with id '{VehicleId}'. Enqueueing it for registration", vehicleId);
                AddVehicle(vehicleId);
                continue;
            }

            if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time).TryPickProblems(out var problems, out var position))
            {
                return problems.Prepend("Failed to sample point with t '{0}' on track with id '{1}' for vehicle with id '{2}'", trackPosition.Time, trackPosition.TrackId, vehicleId);
            }

            var worldMatrix = Matrix4X4.CreateScale(_scale);
            if (trackPosition.Velocity > 0)
            {
                worldMatrix *= Matrix4X4.CreateRotationY(float.Pi);
            }

            worldMatrix *= position.ToMatrix4X4();

            if (renderingInstanceManager.Update(_groupId, instanceId, new Shaders.Default.Instance(worldMatrix))
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to update vehicle instance for '{0}'", vehicleId);
            }
        }

        return Result.Success();
    }
}
