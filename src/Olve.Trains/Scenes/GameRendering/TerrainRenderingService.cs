using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TerrainRenderingService(
    TerrainService terrainService,
    TextureManager textureManager,
    TextureEntityManager textureEntityManager,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    CameraSceneService cameraSceneService,
    TerrainRaycastService terrainRaycastService,
    SceneLightService sceneLightService) : ISceneService
{
    private readonly Shaders.Terrain _terrainShader = new()
    {
        MouseRadius = 5f,
    };

    public int Priority => SceneServicePriority.FromDependencies([cameraSceneService, terrainService, terrainRaycastService, sceneLightService]);

    public Result Load()
    {
        if (terrainService.Terrain is not {} terrain)
        {
            return new ResultProblem("TerrainService.Terrain is null");
        }

        if (renderingServiceHelper.LoadShader(_terrainShader).TryPickProblems(out var shaderProblems))
        {
            return shaderProblems.Prepend("Failed to load terrain shader");
        }

        var heightmap = terrain.Heightmap;

        // Compute vertex count: 6 vertices per quad (2 triangles), all derived from gl_VertexID in shader
        var quadsX = heightmap.Width - 1;
        var quadsZ = heightmap.Length - 1;
        var vertexCount = (uint)(quadsX * quadsZ * 6);

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

        // Register draw-arrays geometry (no vertex data — terrain uses gl_VertexID)
        if (geometryManager.RegisterDrawArrays(vertexCount)
            .TryPickProblems(out var geoProblems, out var geometryId))
        {
            return geoProblems.Prepend("Failed to register terrain geometry");
        }

        // Register group
        if (renderingGroupManager.RegisterDrawArrays<Shaders.Terrain.Instance>(
                geometryId, _terrainShader, _terrainShader.BlendState)
            .TryPickProblems(out var groupProblems, out var groupId))
        {
            return groupProblems.Prepend("Failed to register terrain group");
        }

        // Add single identity instance
        if (renderingInstanceManager.Add(groupId, new Shaders.Terrain.Instance(Matrix4X4<float>.Identity))
            .TryPickProblems(out var instanceProblems, out _))
        {
            return instanceProblems.Prepend("Failed to add terrain instance");
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        // Update shader uniforms so RenderAll() picks them up via MakeParameters()
        sceneLightService.ApplyShaderParameters(_terrainShader);
        cameraSceneService.ApplyCameraPositionParameters(_terrainShader);
        cameraSceneService.ApplyCameraDirectionParameters(_terrainShader);
        terrainRaycastService.ApplyTerrainIntersectionParameters(_terrainShader);

        return Result.Success();
    }
}
