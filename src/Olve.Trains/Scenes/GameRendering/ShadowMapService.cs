using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.ShaderExtensions;
using Olve.Trains.Scenes.GameLogic.Terrain;

namespace Olve.Trains.Scenes.GameRendering;

public class ShadowMapService(
    FramebufferManager framebufferManager,
    RenderPassManager renderPassManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    TextureEntityManager textureEntityManager,
    SceneLightService sceneLightService,
    TerrainService terrainService,
    ILogger<ShadowMapService> logger) : ISceneService
{
    public const int ShadowMapSize = 2048;

    public int Priority => SceneServicePriority.FromDependencies([sceneLightService, terrainService]);

    private Id<Framebuffer<IShadowFrameFormat>> _framebufferId;
    private Id<RenderPass<IShadowFrameFormat>> _passId;
    private TextureId<Depth> _shadowMapTexture = null!;

    private readonly Shaders.Shadow _shadowShader = new();
    private readonly Shaders.ShadowTerrain _shadowTerrainShader = new();
    private readonly Shaders.ShadowTrack _shadowTrackShader = new();

    public Matrix4X4<float> LightSpaceMatrix { get; private set; } = Matrix4X4<float>.Identity;
    public TextureId<Depth> ShadowMapTexture => _shadowMapTexture;

    // ── Shadow group handles ──────────────────────────────────────────
    public readonly record struct ShadowGroupHandle<TInstance>(GroupId<TInstance> GroupId)
        where TInstance : IInstanceData;

    // ── Lifecycle ─────────────────────────────────────────────────────

    public Result Load()
    {
        // Create depth-only FBO
        if (framebufferManager.CreateWithDepth<IShadowFrameFormat>(ShadowMapSize, ShadowMapSize)
            .TryPickProblems(out var problems, out var fbResult))
        {
            return problems.Prepend("Failed to create shadow map framebuffer");
        }

        (_framebufferId, _shadowMapTexture) = fbResult;

        // Register the depth texture in the texture entity manager so receiver shaders can sample it
        if (framebufferManager.TryGetTextureHandle(_shadowMapTexture, out var glHandle))
        {
            textureEntityManager.RegisterHandle(
                _shadowMapTexture,
                new Texture2D(glHandle, ShadowMapSize, ShadowMapSize));
        }

        // Create shadow pass (between clear=-100 and main=0)
        if (renderPassManager.Create(_framebufferId, priority: -50, ClearFlags.Depth)
            .TryPickProblems(out problems, out var passId))
        {
            return problems.Prepend("Failed to create shadow render pass");
        }

        _passId = passId;

        logger.LogInformation("Shadow pass created: passId={PassId}, fbId={FbId}", passId, _framebufferId);

        // Load shadow shaders
        if (renderingServiceHelper.LoadShader(_shadowShader).TryPickProblems(out problems)
            || renderingServiceHelper.LoadShader(_shadowTerrainShader).TryPickProblems(out problems)
            || renderingServiceHelper.LoadShader(_shadowTrackShader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shadow shaders");
        }

        return Result.Success();
    }

    private int _debugFrameCount;

    public Result Update(TimeSpan deltaTime)
    {
        var lightView = ComputeLightView();
        var lightProjection = ComputeLightProjection(lightView);
        LightSpaceMatrix = lightView * lightProjection;

        // Push light-space view/projection to shadow shaders.
        // The shadow shaders use the same view/projection uniform names as the main shaders,
        // but we set them to the light-space matrices instead of the camera matrices.
        _shadowShader.View = lightView;
        _shadowShader.Projection = lightProjection;

        _shadowTerrainShader.View = lightView;
        _shadowTerrainShader.Projection = lightProjection;

        _shadowTrackShader.View = lightView;
        _shadowTrackShader.Projection = lightProjection;

        _debugFrameCount++;
        if (_debugFrameCount % 300 == 1)
        {
            var sunDir = sceneLightService.SunDirection;
            logger.LogInformation(
                "Shadow debug: sunDir=({SunX:F2},{SunY:F2},{SunZ:F2}) passExists={PassExists} " +
                "lightView.M41={LvM41:F2} lightView.M42={LvM42:F2} lightView.M43={LvM43:F2}",
                sunDir.X, sunDir.Y, sunDir.Z,
                renderPassManager.Exists(_passId),
                lightView.M41, lightView.M42, lightView.M43);
        }

        return Result.Success();
    }

    public Result Unload()
    {
        renderPassManager.Destroy(_passId);
        framebufferManager.Delete(_framebufferId);

        return Result.Success();
    }

    // ── Terrain shadow shader configuration ─────────────────────────────

    public void ConfigureTerrainShadowShader(TextureId<float> heightMap, Vector2D<int> gridSize)
    {
        _shadowTerrainShader.HeightMap = heightMap;
        _shadowTerrainShader.GridSize = gridSize;
    }

    // ── Shadow group registration ─────────────────────────────────────

    public Result<ShadowGroupHandle<Shaders.Default.Instance>> RegisterDefaultShadowGroup(
        GeometryId<Shaders.Default.Vertex> geometryId)
    {
        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IShadowFrameFormat>(
                geometryId, _shadowShader, _passId, RenderState.Opaque)
            .TryPickProblems(out var problems, out var groupId))
        {
            return problems.Prepend("Failed to register default shadow group");
        }

        return new ShadowGroupHandle<Shaders.Default.Instance>(groupId!);
    }

    public Result<ShadowGroupHandle<Shaders.Terrain.Instance>> RegisterTerrainShadowGroup(
        UntypedGeometryId geometryId)
    {
        if (renderingGroupManager.RegisterDrawArrays<Shaders.Terrain.Instance, IShadowFrameFormat>(
                geometryId, _shadowTerrainShader, _passId, RenderState.Opaque)
            .TryPickProblems(out var problems, out var groupId))
        {
            return problems.Prepend("Failed to register terrain shadow group");
        }

        return new ShadowGroupHandle<Shaders.Terrain.Instance>(groupId!);
    }

    public Result<ShadowGroupHandle<Shaders.Track.Instance>> RegisterTrackShadowGroup(
        GeometryId<Shaders.Track.Vertex> geometryId)
    {
        if (renderingGroupManager.Register<Shaders.Track.Vertex, Shaders.Track.Instance, IShadowFrameFormat>(
                geometryId, _shadowTrackShader, _passId, RenderState.Opaque)
            .TryPickProblems(out var problems, out var groupId))
        {
            return problems.Prepend("Failed to register track shadow group");
        }

        return new ShadowGroupHandle<Shaders.Track.Instance>(groupId!);
    }

    // ── Shadow instance management ────────────────────────────────────

    private int _instanceCount;

    public Result<Id<TInstance>> AddInstance<TInstance>(
        ShadowGroupHandle<TInstance> handle, TInstance instance)
        where TInstance : IInstanceData
    {
        _instanceCount++;
        if (_instanceCount <= 10)
        {
            logger.LogInformation("Shadow instance added (#{Count}): groupId={GroupId}", _instanceCount, handle.GroupId);
        }

        return renderingInstanceManager.Add(handle.GroupId, instance);
    }

    public Result UpdateInstance<TInstance>(
        ShadowGroupHandle<TInstance> handle, Id<TInstance> instanceId, TInstance instance)
        where TInstance : IInstanceData
    {
        return renderingInstanceManager.Update(handle.GroupId, instanceId, instance);
    }

    public Result RemoveInstance<TInstance>(
        ShadowGroupHandle<TInstance> handle, Id<TInstance> instanceId)
        where TInstance : IInstanceData
    {
        return renderingInstanceManager.Remove(handle.GroupId, instanceId);
    }

    // ── Apply shadow parameters to receiver shaders ───────────────────

    public void ApplyShaderParameters(IShadowShader shader)
    {
        shader.LightSpaceMatrix = LightSpaceMatrix;
        shader.ShadowMap = _shadowMapTexture;
    }

    // ── Light-space matrix computation ────────────────────────────────

    private Matrix4X4<float> ComputeLightView()
    {
        var terrain = terrainService.Terrain;
        var halfW = terrain.Heightmap.Width / 2f;
        var halfL = terrain.Heightmap.Length / 2f;
        var sceneCenter = new Vector3D<float>(halfW, 0f, halfL);

        var sunDir = sceneLightService.SunDirection;

        // Avoid degenerate cases when sun is near horizon
        var lightDir = Vector3D.Normalize(sunDir);
        if (float.Abs(lightDir.Y) < 0.01f)
        {
            lightDir = new Vector3D<float>(lightDir.X, -0.01f, lightDir.Z);
            lightDir = Vector3D.Normalize(lightDir);
        }

        var lightDistance = float.Max(halfW, halfL) * 2f;
        var lightPos = sceneCenter - lightDir * lightDistance;

        // Up vector perpendicular to light direction
        var up = new Vector3D<float>(0f, 0f, 1f);

        return Matrix4X4.CreateLookAt(lightPos, sceneCenter, up);
    }

    private Matrix4X4<float> ComputeLightProjection(Matrix4X4<float> lightView)
    {
        var terrain = terrainService.Terrain;
        var w = (float)terrain.Heightmap.Width;
        var l = (float)terrain.Heightmap.Length;

        // Transform scene AABB corners to light space to find tight ortho bounds
        Span<Vector3D<float>> corners =
        [
            new(0, -1, 0),
            new(w, -1, 0),
            new(0, 5, 0),
            new(w, 5, 0),
            new(0, -1, l),
            new(w, -1, l),
            new(0, 5, l),
            new(w, 5, l),
        ];

        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (var corner in corners)
        {
            var lightSpaceCorner = Vector3D.Transform(corner, lightView);
            minX = float.Min(minX, lightSpaceCorner.X);
            maxX = float.Max(maxX, lightSpaceCorner.X);
            minY = float.Min(minY, lightSpaceCorner.Y);
            maxY = float.Max(maxY, lightSpaceCorner.Y);
            minZ = float.Min(minZ, lightSpaceCorner.Z);
            maxZ = float.Max(maxZ, lightSpaceCorner.Z);
        }

        // Pad slightly to avoid edge clipping
        const float pad = 5f;
        var left = minX - pad;
        var right = maxX + pad;
        var bottom = minY - pad;
        var top = maxY + pad;
        var near = minZ - pad;
        var far = maxZ + pad;

        // Build orthographic matrix for OpenGL NDC [-1,1] with right-handed view space.
        // Silk.NET's CreateLookAt is right-handed: objects in front have negative Z.
        // Negate M33 and M43 so closer-to-light objects get smaller depth values,
        // which is required for the shadow comparison (currentDepth > storedDepth → shadow).
        return new Matrix4X4<float>(
            2f / (right - left), 0, 0, 0,
            0, 2f / (top - bottom), 0, 0,
            0, 0, -2f / (far - near), 0,
            -(right + left) / (right - left),
            -(top + bottom) / (top - bottom),
            (far + near) / (far - near),
            1f);
    }

}
