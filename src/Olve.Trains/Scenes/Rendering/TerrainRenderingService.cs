using Olve.Generated;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Light;
using Olve.Trains.Scenes.Game.Terrain;

namespace Olve.Trains.Scenes.Rendering;

public class TerrainRenderingService(ILoggingManager loggingManager,
    TerrainService terrainService,
    ShaderEntityManager shaderEntityManager,
    HeightmapEntityManager heightmapEntityManager,
    RenderingManager3D renderingManager3D,
    CameraSceneService cameraSceneService,
    TerrainRaycastService terrainRaycastService,
    SceneLightService sceneLightService) : SceneService(loggingManager)
{
    public RenderingId<HeightmapData> TerrainRenderingId { get; set; }
    public RenderingInstanceId TerrainInstanceId { get; set; }
    public RenderingInstanceId WireframeTerrainInstanceId { get; set; }

    private readonly Shaders.Terrain _terrainShader = new();
    private readonly Shaders.TerrainWireframe _terrainWireframe = new()
    {
        MouseRadius = 5f,
        BlendState = RenderState.Additive
    };

    public override int Priority => GetPriorityFromDependencies([cameraSceneService, terrainService, terrainRaycastService, sceneLightService]);

    protected override Result OnLoad()
    {
        if (terrainService.Terrain is not {} terrain)
        {
            return new ResultProblem("TerrainService.Terrain is null");
        }

        if (heightmapEntityManager.Register(terrain.Heightmap).TryPickProblems(out var problems, out var heightmapId))
        {
            return problems.Prepend("Failed to register heightmap");
        }

        if (heightmapEntityManager.GetRegistration(heightmapId)
            .TryPickProblems(out problems, out var heightmapRegistration))
        {
            return problems.Prepend("Failed to get heightmap registration");
        }

        Vector2D<float> textureSize = new(1f / terrain.Heightmap.Width, 1f / terrain.Heightmap.Length);
        _terrainShader.TexelSize = textureSize;
        _terrainWireframe.TexelSize = textureSize;

        _terrainShader.HeightMap = heightmapRegistration.Texture;
        _terrainWireframe.HeightMap = heightmapRegistration.Texture;

        if (RegisterShader(heightmapId, _terrainShader.ShaderData).TryPickProblems(out problems, out var terrainShaderIds))
        {
            return problems.Prepend("Failed to register terrain shader");
        }

        if (RegisterShader(heightmapId, _terrainWireframe.ShaderData).TryPickProblems(out problems, out var wireframeShaderIds))
        {
            return problems.Prepend("Failed to register wireframe shader");
        }

        TerrainRenderingId = heightmapId;
        _terrainShader.RenderingId = terrainShaderIds.ShaderId;
        _terrainWireframe.RenderingId = wireframeShaderIds.ShaderId;
        TerrainInstanceId = terrainShaderIds.InstanceId;
        WireframeTerrainInstanceId = wireframeShaderIds.InstanceId;

        return Result.Success();
    }

    private Result<(RenderingId<ShaderData> ShaderId, RenderingInstanceId InstanceId)> RegisterShader(
        RenderingId<HeightmapData> heightmapId, ShaderData shaderData)
    {

        if (shaderEntityManager.Register(shaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        var worldMatrix = Matrix4X4<float>.Identity;

        if (renderingManager3D.RegisterInstance(heightmapId, shaderId, worldMatrix).TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register instance");
        }

        return (shaderId, instanceId);
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        sceneLightService.ApplyShaderParameters(_terrainShader);
        cameraSceneService.ApplyCameraPositionParameters(_terrainShader);
        cameraSceneService.ApplyCameraDirectionParameters(_terrainShader);
        cameraSceneService.ApplyCameraPositionParameters(_terrainWireframe);
        terrainRaycastService.ApplyTerrainIntersectionParameters(_terrainWireframe);


        var terrainShaderResult = renderingManager3D.Render(_terrainShader).IfProblem(p => p.Prepend("Failed rendering terrain"));
        var wireframeShaderResult = renderingManager3D.Render(_terrainWireframe).IfProblem(p => p.Prepend("Failed rendering terrain wireframe"));

        if (terrainShaderResult.TryPickProblems(out var problems)
            || wireframeShaderResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Failed rendering terrain");
        }

        return Result.Success();
    }
}