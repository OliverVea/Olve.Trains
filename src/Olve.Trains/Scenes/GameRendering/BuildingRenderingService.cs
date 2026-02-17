using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Buildings;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    ILogger<BuildingRenderingService> logger,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    RenderingServiceHelper renderingServiceHelper,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    TerrainRenderingService terrainRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private GeometryId _geometryId;
    private readonly Dictionary<Id<Building>, RenderingInstanceId> _instanceIds = new();
    private readonly Shaders.Building _shader = new() { BlendState = RenderState.AlphaBlend };

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load building shader");
        }

        _shader.UOpacity = 1.0f;
        _shader.UColorOverride = new Vector3D<float>(0, 0, 0);
        _shader.UColorMix = 0.0f;

        // Generate a unit cube: 24 vertices (4 per face with normals), 36 indices
        // TODO: Extract unit cube (and unit quad) to a BaseGeometryService in Olve.Trains/Rendering/
        var vertices = GenerateUnitCubeVertices();
        var indices = GenerateUnitCubeIndices();

        if (renderingManager3D.RegisterGeometry(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register building geometry");
        }

        _geometryId = geometryId;

        return Result.Success();
    }

    public Result Unload()
    {
        var deregisterInstanceResults = _instanceIds.Select(ids => renderingManager3D.DeregisterInstance(ids.Value));

        var result = Result.Concat([
            ..deregisterInstanceResults,
            renderingManager3D.DeregisterGeometry(_geometryId),
            renderingServiceHelper.UnloadShader(_shader),
        ]);

        if (result.TryPickProblems(out var problems))
        {
            logger.Log(problems.Prepend("Failed to deregister rendering resources owned by {0}", nameof(BuildingRenderingService)));
        }

        return Result.Success();
    }

    public Result Register(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        var color = new RGB(0.7f, 0.7f, 0.7f);
        var entityParams = new Shaders.Building.EntityParameters(UColor: color.ToVector());

        return RegisterDirect(buildingId, building.Position, blueprint.Footprint, entityParams);
    }

    public Result RegisterDirect(
        Id<Building> buildingId,
        BuildingPosition position,
        TileFootprint footprint,
        Shaders.Building.EntityParameters? entityParameters = null)
    {
        if (_instanceIds.ContainsKey(buildingId))
        {
            return new ResultProblem("Tried to add rendering instance of building that already has a rendering instance");
        }

        var worldMatrix = ComputeWorldMatrix(footprint, position);

        if (renderingManager3D.RegisterInstance(_geometryId, _shader.RenderingId, worldMatrix)
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to register building instance");
        }

        if (entityParameters is { } ep)
        {
            renderingManager3D.SetInstanceParameters(instanceId, ep);
        }

        _instanceIds[buildingId] = instanceId;
        return Result.Success();
    }

    public Result UpdateDirect(
        Id<Building> buildingId,
        BuildingPosition position,
        TileFootprint footprint,
        Shaders.Building.EntityParameters? entityParameters = null)
    {
        if (!_instanceIds.TryGetValue(buildingId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
        }

        var worldMatrix = ComputeWorldMatrix(footprint, position);
        renderingManager3D.SetInstanceWorld(instanceId, worldMatrix);

        if (entityParameters is { } ep)
        {
            renderingManager3D.SetInstanceParameters(instanceId, ep);
        }

        return Result.Success();
    }

    private static Matrix4X4<float> ComputeWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        const float inset = 0.1f;
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var sw = w - inset * 2;
        var sd = d - inset * 2;

        var rotation = position.CardinalDirection.ToYRotation();

        return Matrix4X4.CreateScale(sw, h, sd)
               * Matrix4X4.CreateTranslation(sw / 2f + inset, 0, sd / 2f + inset)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation<float>(
                   position.BottomLeft.X,
                   position.BottomLeft.Y,
                   position.BottomLeft.Z);
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (!_instanceIds.Remove(buildingId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
        }

        return renderingManager3D.DeregisterInstance(instanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        return renderingManager3D.Render(_shader);
    }

    private static Shaders.Building.Vertex[] GenerateUnitCubeVertices()
    {
        var vertices = new Shaders.Building.Vertex[24];
        var i = 0;

        var n = V(0, 0, 1);
        vertices[i++] = new(V(-0.5f, 0, 0.5f), n);
        vertices[i++] = new(V(0.5f, 0, 0.5f), n);
        vertices[i++] = new(V(0.5f, 1, 0.5f), n);
        vertices[i++] = new(V(-0.5f, 1, 0.5f), n);

        // Back face (z = -0.5), normal (0, 0, -1)
        n = V(0, 0, -1);
        vertices[i++] = new(V(0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(-0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(-0.5f, 1, -0.5f), n);
        vertices[i++] = new(V(0.5f, 1, -0.5f), n);

        // Right face (x = +0.5), normal (1, 0, 0)
        n = V(1, 0, 0);
        vertices[i++] = new(V(0.5f, 0, 0.5f), n);
        vertices[i++] = new(V(0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(0.5f, 1, -0.5f), n);
        vertices[i++] = new(V(0.5f, 1, 0.5f), n);

        // Left face (x = -0.5), normal (-1, 0, 0)
        n = V(-1, 0, 0);
        vertices[i++] = new(V(-0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(-0.5f, 0, 0.5f), n);
        vertices[i++] = new(V(-0.5f, 1, 0.5f), n);
        vertices[i++] = new(V(-0.5f, 1, -0.5f), n);

        // Top face (y = 1), normal (0, 1, 0)
        n = V(0, 1, 0);
        vertices[i++] = new(V(-0.5f, 1, 0.5f), n);
        vertices[i++] = new(V(0.5f, 1, 0.5f), n);
        vertices[i++] = new(V(0.5f, 1, -0.5f), n);
        vertices[i++] = new(V(-0.5f, 1, -0.5f), n);

        // Bottom face (y = 0), normal (0, -1, 0)
        n = V(0, -1, 0);
        vertices[i++] = new(V(-0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(0.5f, 0, -0.5f), n);
        vertices[i++] = new(V(0.5f, 0, 0.5f), n);
        vertices[i++] = new(V(-0.5f, 0, 0.5f), n);

        return vertices;

        Vector3D<float> V(float x, float y, float z) => new(x, y, z);
    }

    private static uint[] GenerateUnitCubeIndices()
    {
        var indices = new uint[36];
        for (uint face = 0; face < 6; face++)
        {
            var offset = face * 4;
            var i = (int)(face * 6);
            indices[i] = offset;
            indices[i + 1] = offset + 1;
            indices[i + 2] = offset + 2;
            indices[i + 3] = offset;
            indices[i + 4] = offset + 2;
            indices[i + 5] = offset + 3;
        }

        return indices;
    }
}
