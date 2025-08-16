using System.Collections.Concurrent;
using Olve.CodeGen;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Results;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.Ids;
using Silk.NET.Maths;
using RenderingServiceHelper = Olve.Engine3D.Rendering.RenderingServiceHelper;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleRenderingService(
    ILoggingManager loggingManager,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    TextureEntityManager textureEntityManager,
    ShaderEntityManager shaderEntityManager,
    SceneLightService sceneLightService,
    MeshEntityManager meshEntityManager,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService
    ) : SceneService(loggingManager)
{

    private RenderingId<MeshData> MeshRenderingId { get; set; }
    private readonly Dictionary<Id<Vehicle>, RenderingInstanceId> _instanceIds  = new();
    private readonly ConcurrentQueue<Id<Vehicle>> _toAdd = new();
    private readonly ConcurrentQueue<Id<Vehicle>> _toRemove = new();
    private readonly Shaders.Default _shader = new();
    private float _scale = 1;
    
    private Result<Texture2D> LoadTexture(AssetPath<TextureData> texturePath) => RenderingServiceHelper.LoadTexture(texturePath, textureEntityManager);
    private Result LoadShader(IShader shader) => RenderingServiceHelper.LoadShader(shader, shaderEntityManager);

    protected override Result OnLoad()
    {
        if (LoadTexture(Textures.SimpleTrains_Texture_01).TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        _shader.TextureSampler = textureId;

        if (LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (AssetLoader.LoadAsset(Meshes.SM_Veh_Bullet_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }
        
        var meshRegistrationResult = meshEntityManager.Register(meshData);
        if (meshRegistrationResult.TryPickProblems(out problems, out var meshRenderingId))
        {
            return problems.Prepend("Failed to register mesh");
        }
        
        AABB aabbTarget = new(Vector3D<float>.Zero, Vector3D<float>.One);
        var scaleResult = AABBHelper.GetUniformScaleToFitInside(meshData, aabbTarget);
        if (scaleResult.TryPickProblems(out problems, out _scale))
        {
            return problems.Prepend("Failed to compute scale");
        }
        
        MeshRenderingId = meshRenderingId;

        vehicleService.OnAdded += OnVehicleAdded;
        vehicleService.OnRemoved += OnVehicleRemoved;

        return Result.Success();
    }

    private void AddVehicle(Id<Vehicle> vehicleId)
    {
        if (_instanceIds.ContainsKey(vehicleId))
        {
            LoggingManager.Log(LogLevel.Debug, $"Tried to add vehicle with id '{vehicleId}' twice. Skipping.");
        }
        
        var registerInstanceResult = renderingManager3D.RegisterInstance(MeshRenderingId, _shader.RenderingId, new Matrix4X4<float>());
        if (registerInstanceResult.TryPickProblems(out var problems, out var meshRenderingId))
        {
            LoggingManager.Log(problems);
            return;
        }
        
        _instanceIds[vehicleId] = meshRenderingId;
    }

    private void RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (_instanceIds.TryGetValue(vehicleId, out var instanceId))
        {
            renderingManager3D.DeregisterInstance(instanceId);
            _instanceIds.Remove(vehicleId);
        }
    }

    private void OnVehicleAdded(Id<Vehicle> vehicleId)
    {
        _toAdd.Enqueue(vehicleId);
    }

    private void OnVehicleRemoved(Id<Vehicle> vehicleId)
    {
        _toRemove.Enqueue(vehicleId);
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (_toRemove.Any())
        {
            foreach (var id in _toRemove) RemoveVehicle(id);
            _toRemove.Clear(); 
        }

        if (_toAdd.Any())
        {
            foreach (var id in _toAdd) AddVehicle(id);
            _toAdd.Clear();
        }
        
        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_instanceIds.TryGetValue(vehicleId, out var instanceId))
            {
                LoggingManager.Log(LogLevel.Warning, $"Did not find rendering instance id for vehicle with id '{vehicleId}'. Enqueueing it for registration");
                _toAdd.Enqueue(vehicleId);
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

    protected override Result OnRender(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);
        
        return renderingManager3D.Render(_shader);
    }
}