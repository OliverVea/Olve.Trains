using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
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
using Silk.NET.OpenGL;
using RenderingServiceHelper = Olve.Engine3D.Rendering.RenderingServiceHelper;

namespace Olve.Trains.Scenes.GameRendering;

public class VehicleRenderingService(
    ILogger<VehicleRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
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

    private readonly Dictionary<Id<Vehicle>, Shaders.Default.Instance> _instances = new();
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;
    private bool _dirty;

    private readonly EventQueue<Id<Vehicle>> _toAddQueue = eventQueueFactory.Create(vehicleService.OnVehicleAdded);
    private readonly EventQueue<Id<Vehicle>> _toRemoveQueue = eventQueueFactory.Create(vehicleService.OnVehicleRemoved);
    private readonly Shaders.Default _shader = new();
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

        var (vertexFloats, indices) = MeshDataMarshalHelper.MarshalDefaultShader(meshData);

        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                (uint)meshData.VertexCount,
                indices,
                Shaders.Default.Vertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Default.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create vehicle instanced buffers");
        }

        _registration = registration;

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

        instancedBufferManager.DeleteMeshInstanceBuffers(_registration);

        return Result.Success();
    }

    private Result AddVehicle(Id<Vehicle> vehicleId)
    {
        if (_instances.ContainsKey(vehicleId))
        {
            return new ResultProblem("Tried to add vehicle with id '{0}' twice.", vehicleId);
        }

        _instances[vehicleId] = new Shaders.Default.Instance(new Matrix4X4<float>());
        _dirty = true;
        return Result.Success();
    }

    private Result RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (_instances.Remove(vehicleId))
        {
            _dirty = true;
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();

        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_instances.ContainsKey(vehicleId))
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

            _instances[vehicleId] = new Shaders.Default.Instance(worldMatrix);
            _dirty = true;
        }

        if (_dirty)
        {
            RebuildInstanceBuffer();
            _dirty = false;
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        if (_instances.Count > 0)
        {
            if (renderingManager3D.RenderInstanced(_shader, _registration, (uint)_instances.Count)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    private void RebuildInstanceBuffer()
    {
        var instanceCount = _instances.Count;
        if (instanceCount == 0)
        {
            instancedBufferManager.UpdateMeshInstanceBuffer(
                _registration,
                ReadOnlySpan<float>.Empty,
                0,
                BufferUsageARB.DynamicDraw);
            return;
        }

        var floats = new float[instanceCount * Shaders.Default.Instance.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var instance in _instances.Values)
        {
            instance.WriteTo(span.Slice(offset, Shaders.Default.Instance.FloatCount));
            offset += Shaders.Default.Instance.FloatCount;
        }

        instancedBufferManager.UpdateMeshInstanceBuffer(
            _registration,
            floats,
            (uint)instanceCount,
            BufferUsageARB.DynamicDraw);
    }
}
