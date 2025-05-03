using Olve.CodeGen;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class TrackArrowRenderingService(
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    TextureEntityManager textureEntityManager,
    ShaderEntityManager shaderEntityManager,
    TerrainRaycastService terrainRaycastService,
    TrackPlacingService trackPlacingService,
    MeshEntityManager meshEntityManager) : SceneService
{
    private float _scale = 1f;
    
    public Shaders.Default? Shader { get; set; }
    public RenderingId<MeshData> MeshRenderingId { get; set; }
    public RenderingInstanceId InstanceId { get; set; }
    
    public override int Priority => GetPriorityFromDependencies([cameraSceneService, terrainRaycastService]);

    public override Result Load()
    {
        var textureResult = LoadTexture();
        if (textureResult.TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }
        
        var shaderResult = LoadShader(textureId);
        if (shaderResult.TryPickProblems(out problems, out var shader))
        {
            return problems.Prepend("Failed to load shader");
        }

        Shader = shader;

        var meshResult = AssetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01);
        if (meshResult.TryPickProblems(out problems, out var meshData))
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

        if (renderingManager3D.RegisterInstance(meshRenderingId, shader.RenderingId, Matrix4X4<float>.Identity).TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register mesh");
        }

        InstanceId = instanceId;

        return Result.Success();
    }
    
    private Result<Texture2D> LoadTexture()
    {
        var textureResult = AssetLoader.LoadAsset(Textures.PolygonPrototype_Texture_01);
        if (textureResult.TryPickProblems(out var problems, out var textureData))
        {
            return problems.Prepend("Failed to load texture");
        }

        var registrationResult = textureEntityManager.Register(textureData);
        if (registrationResult.TryPickProblems(out problems, out var textureId))
        {
            return problems.Prepend("Failed to register texture");
        }
        

        if (textureEntityManager.GetRegistration(textureId).TryPickProblems(out problems, out var textureRegistration))
        {
            return problems;
        }

        return textureRegistration.Texture;
    }

    private Result<Shaders.Default> LoadShader(Texture2D texture2D)
    {
        Shaders.Default shader = new()
        {
            AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
            AmbientLightIntensity = 1f,
            TextureSampler = texture2D
        };

        var registrationResult = shaderEntityManager.Register(shader.ShaderData);
        if (registrationResult.TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        shader.RenderingId = shaderId;

        return shader;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        var worldMatrix = ComputeWorldMatrix();
        if (renderingManager3D.SetInstanceWorld(InstanceId, worldMatrix).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to update instance");
        }
        
        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        if (Shader == null)
        {
            return new ResultProblem("Shader is not loaded");
        }
        
        Shader.View = cameraSceneService.ViewMatrix;
        Shader.Projection = cameraSceneService.ProjectionMatrix;
        Shader.CameraDirection = cameraSceneService.CameraViewDirection;
        
        return renderingManager3D.Render(Shader);
    }

    private Matrix4X4<float> ComputeWorldMatrix()
    {
        if (trackPlacingService.CurrentPoint is not { } currentPoint)
        {
            return Matrix4X4<float>.Identity * 0f;
        }
        
        var yOffset = Vector3D<float>.UnitY * 0.15f;
        
        Vector2D<float> currentTangent2d = new(currentPoint.Tangent.X, currentPoint.Tangent.Z);
        Vector2D<float> northTangent2d = new(0f, 1f);
        
        var yRotation = -float.Atan2(currentTangent2d.Y, currentTangent2d.X) + float.Pi / 2f;
        
        return Matrix4X4.CreateScale(new Vector3D<float>(0.7f, 0.7f, 0.2f) * _scale) *
               Matrix4X4.CreateRotationX(float.Pi / 2f) *
                Matrix4X4.CreateRotationY(yRotation) *
               Matrix4X4.CreateTranslation(currentPoint.Point + yOffset);
    }
}