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
using Olve.Trains.Scenes.Game.Terrain;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackArrowRenderingService(ILoggingManager loggingManager, 
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    TextureEntityManager textureEntityManager,
    ShaderEntityManager shaderEntityManager,
    TerrainRaycastService terrainRaycastService,
    TrackPlacingService trackPlacingService,
    MeshEntityManager meshEntityManager) : SceneService(loggingManager)
{
    private float _scale = 1f;
    
    private readonly Shaders.Default _shader = new()
    {
        AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
        AmbientLightIntensity = 1f,
    };
    
    private RenderingId<MeshData> MeshRenderingId { get; set; }
    private RenderingInstanceId InstanceId { get; set; }
    
    public override int Priority { get; } = GetPriorityFromDependencies([cameraSceneService, terrainRaycastService]);
    
    private Result<Texture2D> LoadTexture(AssetPath<TextureData> texturePath) => RenderingServiceHelper.LoadTexture(texturePath, textureEntityManager);
    private Result LoadShader(IShader shader) => RenderingServiceHelper.LoadShader(shader, shaderEntityManager);

    protected override Result OnLoad()
    {
        if (LoadTexture(Textures.PolygonPrototype_Texture_01).TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        _shader.TextureSampler = textureId;
        
        if (LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (AssetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01).TryPickProblems(out problems, out var meshData))
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

        if (renderingManager3D.RegisterInstance(meshRenderingId, _shader.RenderingId, Matrix4X4<float>.Identity).TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register mesh");
        }

        InstanceId = instanceId;

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        var worldMatrix = ComputeWorldMatrix();
        if (renderingManager3D.SetInstanceWorld(InstanceId, worldMatrix).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to update instance");
        }
        
        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        
        return renderingManager3D.Render(_shader);
    }

    private Matrix4X4<float> ComputeWorldMatrix()
    {
        if (trackPlacingService.CurrentPoint is not { } currentPoint)
        {
            return Matrix4X4<float>.Identity * 0f;
        }
        
        var yOffset = Vector3D<float>.UnitY * 0.15f;
        
        Vector2D<float> currentTangent2d = new(currentPoint.Tangent.X, currentPoint.Tangent.Z);
        
        var yRotation = -float.Atan2(currentTangent2d.Y, currentTangent2d.X) + float.Pi / 2f;
        
        return Matrix4X4.CreateScale(new Vector3D<float>(0.7f, 0.7f, 0.2f) * _scale) *
               Matrix4X4.CreateRotationX(float.Pi / 2f) *
                Matrix4X4.CreateRotationY(yRotation) *
               Matrix4X4.CreateTranslation(currentPoint.Point + yOffset);
    }
}