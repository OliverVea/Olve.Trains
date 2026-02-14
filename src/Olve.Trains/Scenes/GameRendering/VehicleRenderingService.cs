using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
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
using RenderingServiceHelper = Olve.Engine3D.Rendering.RenderingServiceHelper;

namespace Olve.Trains.Scenes.GameRendering;

public class VehicleRenderingService(
    ILogger<VehicleRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    RenderingManager3D renderingManager3D,
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

    private GeometryId _geometryId;
    private readonly Dictionary<Id<Vehicle>, RenderingInstanceId> _instanceIds  = new();
    private readonly EventQueue<Id<Vehicle>> _toAddQueue = eventQueueFactory.Create(vehicleService.OnVehicleAdded);
    private readonly EventQueue<Id<Vehicle>> _toRemoveQueue = eventQueueFactory.Create(vehicleService.OnVehicleRemoved);
    private readonly Shaders.Default _shader = new();
    private float _scale = 1;

    private Result LoadShader(IShader shader) => renderingServiceHelper.LoadShader(shader);

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

        if (LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Veh_Bullet_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        // TODO: investigate this
        // Convert MeshData to Shaders.Default.Vertex[] and register geometry
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
            vertices[i] = new(meshData.Positions[i], meshData.Normals[i], meshData.TextureCoordinates[i]);

        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        if (renderingManager3D.RegisterGeometry(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register geometry");
        }

        _geometryId = geometryId;

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

        var registerInstanceResult = renderingManager3D.RegisterInstance(_geometryId, _shader.RenderingId, new Matrix4X4<float>());
        if (registerInstanceResult.TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add vehicle");
        }

        _instanceIds[vehicleId] = instanceId;
        return Result.Success();
    }

    private Result RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (_instanceIds.TryGetValue(vehicleId, out var instanceId))
        {
            renderingManager3D.DeregisterInstance(instanceId);
            _instanceIds.Remove(vehicleId);
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();

        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_instanceIds.TryGetValue(vehicleId, out var instanceId))
            {
                logger.LogWarning("Did not find rendering instance id for vehicle with id '{VehicleId}'. Enqueueing it for registration", vehicleId);
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

            renderingManager3D.SetInstanceWorld(instanceId, worldMatrix);
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        return renderingManager3D.Render(_shader);
    }
}
