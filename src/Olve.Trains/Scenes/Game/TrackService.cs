using Olve.CodeGen;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class TrackService(TrackArrowRenderingService trackArrowRenderingService) : ISceneService
{
    public Result Load()
    {
        var result = trackArrowRenderingService.Load();
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track arrow rendering service");
        }

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        return Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    public Result Unload()
    {
        return Result.Success();
    }
}

public class TrackArrowRenderingService(
    RenderingManager renderingManager,
    ShaderEntityManager shaderEntityManager,
    MeshEntityManager meshEntityManager)
{
    public IShader Shader { get; set; } = null!;
    public RenderingId<MeshData> MeshRenderingId { get; set; }
    public RenderingInstanceId InstanceId { get; set; }

    public Result Load()
    {
        var shaderResult = LoadShader();
        if (shaderResult.TryPickProblems(out var problems, out var shader))
        {
            return problems.Prepend("Failed to load shader");
        }

        Shader = shader;

        var meshResult = LoadMesh();
        if (meshResult.TryPickProblems(out problems, out var meshRenderingId))
        {
            return problems.Prepend("Failed to load mesh");
        }

        MeshRenderingId = meshRenderingId;

        if (renderingManager.RegisterInstance(MeshRenderingId, shader.RenderingId, Matrix4X4<float>.Identity).TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register mesh");
        }

        InstanceId = instanceId;

        return Result.Success();
    }

    private Result<RenderingId<MeshData>> LoadMesh() => Result.Chain(
        () => AssetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01),
        meshEntityManager.Register);

    private Result<IShader> LoadShader()
    {
        Shaders.Default shader = new()
        {
            AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
            AmbientLightIntensity = 1f
        };

        if (shaderEntityManager.Register(shader.ShaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        shader.RenderingId = shaderId;

        return shader;
    }
}