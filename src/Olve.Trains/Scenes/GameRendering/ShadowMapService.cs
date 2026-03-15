using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
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
    CameraSceneService cameraSceneService,
    ScreenshotManager screenshotManager,
    ILogger<ShadowMapService> logger) : ISceneService
{
    public const int ShadowMapSize = 2048;

    /// <summary>
    /// Maximum world-space height that shadow casters can reach (terrain + tallest building).
    /// </summary>
    private const float MaxCasterHeight = 3f;

    /// <summary>
    /// Minimum world-space height for the shadow volume (below terrain).
    /// </summary>
    private const float MinCasterHeight = -1f;

    public int Priority => SceneServicePriority.FromDependencies([sceneLightService, terrainService, cameraSceneService]);

    private Id<Framebuffer<IShadowFrameFormat>> _framebufferId;
    private Id<RenderPass<IShadowFrameFormat>> _passId;
    private TextureId<Rg32f> _shadowMapTexture = null!;

    private readonly Shaders.Shadow _shadowShader = new();
    private readonly Shaders.ShadowTerrain _shadowTerrainShader = new();
    private readonly Shaders.ShadowTrack _shadowTrackShader = new();

    public Matrix4X4<float> LightSpaceMatrix { get; private set; } = Matrix4X4<float>.Identity;
    public TextureId<Rg32f> ShadowMapTexture => _shadowMapTexture;

    // Camera frustum corners in shadow map UV space [0,1], updated each frame for debug overlay
    // [0..3] = corners at Y=MinCasterHeight, [4..7] = corners at Y=MaxCasterHeight
    private readonly Vector2D<float>[] _frustumUvCorners = new Vector2D<float>[8];

    // ── Shadow group handles ──────────────────────────────────────────
    public readonly record struct ShadowGroupHandle<TInstance>(GroupId<TInstance> GroupId)
        where TInstance : IInstanceData;

    // ── Lifecycle ─────────────────────────────────────────────────────

    public Result Load()
    {
        // Create color+depth FBO for VSM (RG32F stores depth moments)
        if (framebufferManager.CreateWithDepth<IShadowFrameFormat, Rg32f>(ShadowMapSize, ShadowMapSize)
            .TryPickProblems(out var problems, out var fbResult))
        {
            return problems.Prepend("Failed to create shadow map framebuffer");
        }

        (_framebufferId, _shadowMapTexture, _) = fbResult;

        // Register the depth texture in the texture entity manager so receiver shaders can sample it
        if (framebufferManager.TryGetTextureHandle(_shadowMapTexture, out var glHandle))
        {
            textureEntityManager.RegisterHandle(
                _shadowMapTexture,
                new Texture2D(glHandle, ShadowMapSize, ShadowMapSize));
        }

        // Create shadow pass (between clear=-100 and main=0)
        // Clear color (1,1,1,1) ensures unrendered areas read as max depth = no shadow
        if (renderPassManager.Create(_framebufferId, priority: -50, ClearFlags.ColorDepth,
                clearColor: new Vector4D<float>(1f, 1f, 1f, 1f))
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

        // Register shadow map as a named screenshot target
        if (framebufferManager.TryGetFboHandle(_framebufferId.Value, out var fboHandle))
        {
            screenshotManager.RegisterTarget("shadow-map",
                new ScreenshotManager.ScreenshotTarget(fboHandle, ShadowMapSize, ShadowMapSize,
                    IsDepth: false, DebugOverlay: DrawFrustumOverlay));
        }

        return Result.Success();
    }

    public Result Update()
    {
        var lightView = ComputeLightView();
        var lightProjection = ComputeLightProjection(lightView);
        LightSpaceMatrix = lightView * lightProjection;

        ComputeFrustumUvCorners();

        // Push light-space view/projection to shadow shaders.
        // The shadow shaders use the same view/projection uniform names as the main shaders,
        // but we set them to the light-space matrices instead of the camera matrices.
        _shadowShader.View = lightView;
        _shadowShader.Projection = lightProjection;

        _shadowTerrainShader.View = lightView;
        _shadowTerrainShader.Projection = lightProjection;

        _shadowTrackShader.View = lightView;
        _shadowTrackShader.Projection = lightProjection;

        return Result.Success();
    }

    public Result Unload()
    {
        screenshotManager.UnregisterTarget("shadow-map");
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

    public Result<Id<TInstance>> AddInstance<TInstance>(
        ShadowGroupHandle<TInstance> handle, TInstance instance)
        where TInstance : IInstanceData
    {
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
        // Intersect the 4 camera frustum depth-edges with Y=MinCasterHeight and Y=MaxCasterHeight
        // to get 8 world-space points, then transform directly to light space.
        // This avoids an intermediate world-space AABB which wastes resolution when
        // the camera diamond is rotated relative to the light.
        var viewProjection = cameraSceneService.ViewMatrix * cameraSceneService.ProjectionMatrix;
        if (!Matrix4X4.Invert(viewProjection, out var invViewProjection))
        {
            invViewProjection = Matrix4X4<float>.Identity;
        }

        // 4 near corners (z=-1) and 4 far corners (z=1) in NDC, paired by screen position
        Span<Vector3D<float>> nearNdc =
        [
            new(-1, -1, -1), new(1, -1, -1), new(-1, 1, -1), new(1, 1, -1),
        ];
        Span<Vector3D<float>> farNdc =
        [
            new(-1, -1, 1), new(1, -1, 1), new(-1, 1, 1), new(1, 1, 1),
        ];

        var terrain = terrainService.Terrain;
        var w = (float)terrain.Heightmap.Width;
        var l = (float)terrain.Heightmap.Length;

        var minLX = float.MaxValue;
        var maxLX = float.MinValue;
        var minLY = float.MaxValue;
        var maxLY = float.MinValue;
        var minLZ = float.MaxValue;
        var maxLZ = float.MinValue;

        for (var i = 0; i < 4; i++)
        {
            var nearWorld = Vector3D.Transform(nearNdc[i], invViewProjection);
            var farWorld = Vector3D.Transform(farNdc[i], invViewProjection);
            var dir = farWorld - nearWorld;

            // Skip near-horizontal edges (shouldn't happen with isometric camera)
            if (float.Abs(dir.Y) < 1e-6f) continue;

            foreach (var targetY in new[] { MinCasterHeight, MaxCasterHeight })
            {
                var t = (targetY - nearWorld.Y) / dir.Y;
                t = float.Clamp(t, 0f, 1f);
                var hit = nearWorld + dir * t;

                // Clamp to terrain bounds
                hit.X = float.Clamp(hit.X, 0f, w);
                hit.Z = float.Clamp(hit.Z, 0f, l);

                // Transform directly to light space
                var ls = Vector3D.Transform(hit, lightView);
                minLX = float.Min(minLX, ls.X);
                maxLX = float.Max(maxLX, ls.X);
                minLY = float.Min(minLY, ls.Y);
                maxLY = float.Max(maxLY, ls.Y);
                minLZ = float.Min(minLZ, ls.Z);
                maxLZ = float.Max(maxLZ, ls.Z);
            }
        }

        // 10% padding for off-screen shadow casters
        var lxPad = (maxLX - minLX) * 0.1f;
        var lyPad = (maxLY - minLY) * 0.1f;
        minLX -= lxPad;
        maxLX += lxPad;
        minLY -= lyPad;
        maxLY += lyPad;

        // Snap ortho bounds to shadow map texel grid to prevent shadow swimming.
        // When the camera moves, the shadow map shifts by sub-texel amounts causing
        // shadows to jitter between texels. Rounding to texel increments fixes this.
        var texelSizeX = (maxLX - minLX) / ShadowMapSize;
        var texelSizeY = (maxLY - minLY) / ShadowMapSize;
        minLX = float.Floor(minLX / texelSizeX) * texelSizeX;
        maxLX = float.Floor(maxLX / texelSizeX) * texelSizeX;
        minLY = float.Floor(minLY / texelSizeY) * texelSizeY;
        maxLY = float.Floor(maxLY / texelSizeY) * texelSizeY;

        // Small Z pad to ensure casters aren't clipped at the depth extremes
        const float zPad = 5f;

        // Build orthographic matrix for OpenGL NDC [-1,1] with right-handed view space.
        // Silk.NET's CreateLookAt is right-handed: objects in front have negative Z.
        // Negate M33 and M43 so closer-to-light objects get smaller depth values,
        // which is required for the shadow comparison (currentDepth > storedDepth → shadow).
        var left = minLX;
        var right = maxLX;
        var bottom = minLY;
        var top = maxLY;
        var near = minLZ - zPad;
        var far = maxLZ + zPad;

        return new Matrix4X4<float>(
            2f / (right - left), 0, 0, 0,
            0, 2f / (top - bottom), 0, 0,
            0, 0, -2f / (far - near), 0,
            -(right + left) / (right - left),
            -(top + bottom) / (top - bottom),
            (far + near) / (far - near),
            1f);
    }

    // ── Debug overlay ──────────────────────────────────────────────────

    private void ComputeFrustumUvCorners()
    {
        // Intersect the 4 camera frustum depth-edges with Y=MinCasterHeight and Y=MaxCasterHeight,
        // then project to shadow map UV space for the debug overlay.
        var viewProjection = cameraSceneService.ViewMatrix * cameraSceneService.ProjectionMatrix;
        if (!Matrix4X4.Invert(viewProjection, out var invViewProjection))
            return;

        Span<Vector3D<float>> nearNdc =
        [
            new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        ];
        Span<Vector3D<float>> farNdc =
        [
            new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1),
        ];

        var heights = new[] { MinCasterHeight, MaxCasterHeight };

        for (var i = 0; i < 4; i++)
        {
            var nearWorld = Vector3D.Transform(nearNdc[i], invViewProjection);
            var farWorld = Vector3D.Transform(farNdc[i], invViewProjection);
            var dir = farWorld - nearWorld;

            for (var h = 0; h < 2; h++)
            {
                var t = float.Abs(dir.Y) > 1e-6f
                    ? (heights[h] - nearWorld.Y) / dir.Y
                    : 0f;
                t = float.Clamp(t, 0f, 1f);
                var worldHit = nearWorld + dir * t;

                // Transform to light clip space via LightSpaceMatrix, then to UV [0,1].
                // Y is flipped because the depth image is vertically flipped (OpenGL reads bottom-up).
                var clip = Vector3D.Transform(worldHit, LightSpaceMatrix);
                _frustumUvCorners[h * 4 + i] = new Vector2D<float>(clip.X * 0.5f + 0.5f, 0.5f - clip.Y * 0.5f);
            }
        }
    }

    private void DrawFrustumOverlay(byte[] pixels, int width, int height)
    {
        // Draw AABB of all frustum points in red — shows total wasted shadow map space
        var uMin = _frustumUvCorners[0];
        var uMax = _frustumUvCorners[0];
        foreach (var c in _frustumUvCorners)
        {
            uMin = new Vector2D<float>(float.Min(uMin.X, c.X), float.Min(uMin.Y, c.Y));
            uMax = new Vector2D<float>(float.Max(uMax.X, c.X), float.Max(uMax.Y, c.Y));
        }

        var ax = (int)(uMin.X * width);
        var ay = (int)(uMin.Y * height);
        var bx = (int)(uMax.X * width);
        var by = (int)(uMax.Y * height);
        DrawLine(pixels, width, height, ax, ay, bx, ay, 255, 80, 80);
        DrawLine(pixels, width, height, bx, ay, bx, by, 255, 80, 80);
        DrawLine(pixels, width, height, bx, by, ax, by, 255, 80, 80);
        DrawLine(pixels, width, height, ax, by, ax, ay, 255, 80, 80);

        // Bottom quad (Y=MinCasterHeight) in green
        for (var i = 0; i < 4; i++)
        {
            var a = _frustumUvCorners[i];
            var b = _frustumUvCorners[(i + 1) % 4];
            DrawLine(pixels, width, height,
                (int)(a.X * width), (int)(a.Y * height),
                (int)(b.X * width), (int)(b.Y * height),
                80, 255, 80);
        }

        // Top quad (Y=MaxCasterHeight) in yellow
        for (var i = 0; i < 4; i++)
        {
            var a = _frustumUvCorners[4 + i];
            var b = _frustumUvCorners[4 + (i + 1) % 4];
            DrawLine(pixels, width, height,
                (int)(a.X * width), (int)(a.Y * height),
                (int)(b.X * width), (int)(b.Y * height),
                255, 255, 0);
        }

        // Vertical connecting lines in cyan
        for (var i = 0; i < 4; i++)
        {
            var lo = _frustumUvCorners[i];
            var hi = _frustumUvCorners[4 + i];
            DrawLine(pixels, width, height,
                (int)(lo.X * width), (int)(lo.Y * height),
                (int)(hi.X * width), (int)(hi.Y * height),
                0, 255, 255);
        }
    }

    private static void DrawLine(byte[] pixels, int width, int height,
        int x0, int y0, int x1, int y1, byte r, byte g, byte b)
    {
        // Bresenham's line algorithm
        var dx = int.Abs(x1 - x0);
        var dy = -int.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            // Draw a 3px thick point for visibility
            for (var oy = -1; oy <= 1; oy++)
            for (var ox = -1; ox <= 1; ox++)
            {
                var px = x0 + ox;
                var py = y0 + oy;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    var idx = (py * width + px) * 4;
                    pixels[idx + 0] = r;
                    pixels[idx + 1] = g;
                    pixels[idx + 2] = b;
                    pixels[idx + 3] = 255;
                }
            }

            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

}
