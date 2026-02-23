using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TerrainRenderingService(
    TerrainService terrainService,
    ShaderEntityManager shaderEntityManager,
    TextureManager textureManager,
    TextureEntityManager textureEntityManager,
    RenderingManager3D renderingManager3D,
    CameraSceneService cameraSceneService,
    TerrainRaycastService terrainRaycastService,
    SceneLightService sceneLightService) : ISceneService
{
    public RenderingInstanceId TerrainInstanceId { get; set; }

    private readonly Shaders.Terrain _terrainShader = new()
    {
        MouseRadius = 5f
    };

    public int Priority => SceneServicePriority.FromDependencies([cameraSceneService, terrainService, terrainRaycastService, sceneLightService]);

    public Result Load()
    {
        if (terrainService.Terrain is not {} terrain)
        {
            return new ResultProblem("TerrainService.Terrain is null");
        }

        // TODO: investigate this
        var heightmap = terrain.Heightmap;

        // Compute vertex count: 6 vertices per quad (2 triangles), all derived from gl_VertexID in shader
        var quadsX = heightmap.Width - 1;
        var quadsZ = heightmap.Length - 1;
        var vertexCount = (uint)(quadsX * quadsZ * 6);

        if (renderingManager3D.RegisterDrawArraysGeometry(vertexCount)
            .TryPickProblems(out var problems, out var geometryId))
        {
            return problems.Prepend("Failed to register terrain geometry");
        }

        // Create R32F heightmap texture through the texture system
        var heightmapPixels = new float[heightmap.Heights.Length];
        for (var i = 0; i < heightmap.Heights.Length; i++)
        {
            heightmapPixels[i] = heightmap.Heights[i] * heightmap.Step;
        }

        var floatTextureData = new TextureData<float>
        {
            Pixels = heightmapPixels,
            Width = heightmap.Width,
            Height = heightmap.Length,
        };

        var heightmapTextureId = textureManager.RegisterTexture(floatTextureData);

        if (textureEntityManager.Register<float, R32FPixelFormat>(heightmapTextureId, new TextureUploadOptions(Wrap: GLEnum.ClampToEdge))
            .TryPickProblems(out var textureProblems))
        {
            return textureProblems.Prepend("Failed to register heightmap texture with OpenGL");
        }

        Vector2D<float> textureSize = new(1f / heightmap.Width, 1f / heightmap.Length);
        _terrainShader.TexelSize = textureSize;
        _terrainShader.HeightMap = heightmapTextureId;

        if (RegisterShader(geometryId, _terrainShader.ShaderData).TryPickProblems(out var shaderProblems, out var terrainShaderIds))
        {
            return shaderProblems.Prepend("Failed to register terrain shader");
        }

        _terrainShader.RenderingId = terrainShaderIds.ShaderId;
        TerrainInstanceId = terrainShaderIds.InstanceId;

        return Result.Success();
    }

    private Result<(RenderingId<ShaderData> ShaderId, RenderingInstanceId InstanceId)> RegisterShader(
        GeometryId geometryId, ShaderData shaderData)
    {
        if (shaderEntityManager.Register(shaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        var worldMatrix = Matrix4X4<float>.Identity;

        if (renderingManager3D.RegisterInstance(geometryId, shaderId, worldMatrix).TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register instance");
        }

        return (shaderId, instanceId);
    }

    public Result Render(TimeSpan deltaTime)
    {
        sceneLightService.ApplyShaderParameters(_terrainShader);
        cameraSceneService.ApplyCameraPositionParameters(_terrainShader);
        cameraSceneService.ApplyCameraDirectionParameters(_terrainShader);
        terrainRaycastService.ApplyTerrainIntersectionParameters(_terrainShader);

        if (renderingManager3D.Render(_terrainShader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed rendering terrain");
        }

        return Result.Success();
    }
}
