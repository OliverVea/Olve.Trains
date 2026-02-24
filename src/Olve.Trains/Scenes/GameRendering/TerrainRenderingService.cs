using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
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
    OpenGLInstancedBufferManager instancedBufferManager,
    RenderingServiceHelper renderingServiceHelper,
    RenderingManager3D renderingManager3D,
    CameraSceneService cameraSceneService,
    TerrainRaycastService terrainRaycastService,
    SceneLightService sceneLightService) : ISceneService
{
    private readonly Shaders.Terrain _terrainShader = new()
    {
        MouseRadius = 5f,
    };

    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;

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

        // Single identity world matrix instance
        var instance = new Shaders.Terrain.Instance(Matrix4X4<float>.Identity);
        var instanceData = new float[Shaders.Terrain.Instance.FloatCount];
        instance.WriteTo(instanceData);

        if (instancedBufferManager.CreateMeshInstanceBuffer(
                meshVertexData: ReadOnlySpan<float>.Empty,
                meshVertexCount: vertexCount,
                meshIndices: ReadOnlySpan<uint>.Empty,
                configureMeshAttributes: _ => { },
                instanceData: instanceData,
                instanceCount: 1,
                configureInstanceAttributes: Shaders.Terrain.Instance.ConfigureAttributes,
                instanceUsage: BufferUsageARB.StaticDraw)
            .TryPickProblems(out var bufferProblems, out var registration))
        {
            return bufferProblems.Prepend("Failed to create terrain instanced buffer");
        }

        _registration = registration;

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        sceneLightService.ApplyShaderParameters(_terrainShader);
        cameraSceneService.ApplyCameraPositionParameters(_terrainShader);
        cameraSceneService.ApplyCameraDirectionParameters(_terrainShader);
        terrainRaycastService.ApplyTerrainIntersectionParameters(_terrainShader);

        if (renderingManager3D.RenderInstanced(_terrainShader, _registration, 1)
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed rendering terrain");
        }

        return Result.Success();
    }
}
