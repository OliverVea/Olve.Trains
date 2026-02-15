using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Industries;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    ILogger<BuildingRenderingService> logger,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    RenderingServiceHelper renderingServiceHelper,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService)
    : ISceneService
{
    private GeometryId _geometryId;
    private readonly Dictionary<Id<Building>, RenderingInstanceId> _instanceIds = new();
    private readonly Shaders.Building _shader = new();

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load building shader");
        }

        _shader.UOpacity = 1.0f;

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
        if (_instanceIds.ContainsKey(buildingId))
        {
            return new ResultProblem("Tried to add rendering instance of building that already has a rendering instance");
        }

        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        var footprint = blueprint.Footprint;
        var position = building.Position;

        // The unit cube goes from (-0.5,0,-0.5) to (0.5,1,0.5).
        // After scaling, shift so the -X,-Z corner is at origin (the anchor corner),
        // then rotate around Y for cardinal direction, then translate to the tile.
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;

        var rotation = position.CardinalDirection.ToYRotation();

        var worldMatrix = Matrix4X4.CreateScale(w, h, d)
                          * Matrix4X4.CreateTranslation(w / 2f, 0, d / 2f)
                          * Matrix4X4.CreateRotationY(rotation)
                          * Matrix4X4.CreateTranslation((float)position.BottomLeft.X, (float)position.BottomLeft.Y, (float)position.BottomLeft.Z);

        if (renderingManager3D.RegisterInstance(_geometryId, _shader.RenderingId, worldMatrix)
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems!.Prepend("Failed to register building instance");
        }

        var color = GetBuildingColor(blueprint.BuildingType);
        renderingManager3D.SetInstanceParameters(instanceId,
            new Shaders.Building.EntityParameters(UColor: color.ToVector()));

        _instanceIds[buildingId] = instanceId;
        return Result.Success();
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (!_instanceIds.TryGetValue(buildingId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
        }

        _instanceIds.Remove(buildingId);
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

    private static RGB GetBuildingColor(BuildingType buildingType) => buildingType switch
    {
        BuildingType.Station => new RGB(0.3f, 0.5f, 0.9f),
        _ => new RGB(0.7f, 0.7f, 0.7f),
    };

    private static Shaders.Building.Vertex[] GenerateUnitCubeVertices()
    {
        // Unit cube centered at (0, 0.5, 0) so bottom face is at y=0
        var vertices = new Shaders.Building.Vertex[24];
        var i = 0;

        Vector3D<float> V(float x, float y, float z) => new(x, y, z);

        // Front face (z = +0.5), normal (0, 0, 1)
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
