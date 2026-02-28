using Olve.Engine3D;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Buildings;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    MeshRenderingService meshRenderingService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    GridService gridService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _ghostInstanceIds = new();

    private MeshRenderingService.MeshGroupHandle _groupHandle;
    private MeshRenderingService.MeshGroupHandle _ghostGroupHandle;

    public Result Load()
    {
        var vertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(vertices);

        var indices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        if (meshRenderingService.RegisterMeshGroup(
                vertices, indices,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f)))
            .TryPickProblems(out var problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register building mesh group");
        }

        _groupHandle = groupHandle;

        if (meshRenderingService.RegisterMeshGroup(
                vertices, indices,
                renderState: RenderState.AlphaBlend,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f),
                    UOpacity: 0.5f,
                    UColorOverride: new Vector3D<float>(0, 0, 0),
                    UColorMix: 0f))
            .TryPickProblems(out problems, out var ghostGroupHandle))
        {
            return problems.Prepend("Failed to register ghost building mesh group");
        }

        _ghostGroupHandle = ghostGroupHandle;

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

        var worldMatrix = ComputeWorldMatrix(blueprint.Footprint, building.Position);

        if (meshRenderingService.AddInstance(_groupHandle, worldMatrix)
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add building instance for '{0}'", buildingId);
        }

        _instanceIds[buildingId] = instanceHandle;

        return Result.Success();
    }

    public Result RegisterGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        var worldMatrix = ComputeWorldMatrix(footprint, position);

        if (meshRenderingService.AddInstance(_ghostGroupHandle, worldMatrix)
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add ghost building instance for '{0}'", ghostId);
        }

        _ghostInstanceIds[ghostId] = instanceHandle;

        return Result.Success();
    }

    public Result UpdateGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        if (!_ghostInstanceIds.TryGetValue(ghostId, out var instanceHandle))
        {
            return new ResultProblem("Could not find ghost instance for building '{0}'", ghostId);
        }

        var worldMatrix = ComputeWorldMatrix(footprint, position);

        if (meshRenderingService.UpdateInstance(instanceHandle, worldMatrix)
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to update ghost building instance for '{0}'", ghostId);
        }

        return Result.Success();
    }

    public void SetGhostAppearance(float opacity, Vector3D<float>? colorOverride = null, float colorMix = 0f)
    {
        meshRenderingService.UpdateGroupParameters(_ghostGroupHandle,
            new Shaders.Default.EntityParameters(
                UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f),
                UOpacity: opacity,
                UColorOverride: colorOverride ?? new Vector3D<float>(0, 0, 0),
                UColorMix: colorMix));
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (_instanceIds.Remove(buildingId, out var instanceHandle))
        {
            return meshRenderingService.RemoveInstance(instanceHandle);
        }

        if (_ghostInstanceIds.Remove(buildingId, out var ghostInstanceHandle))
        {
            return meshRenderingService.RemoveInstance(ghostInstanceHandle);
        }

        return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    private Matrix4X4<float> ComputeWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        const float inset = 0f;
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var sw = w - inset * 2;
        var sd = d - inset * 2;
        var sh = h - inset;

        var y = gridService.ToTileOrigin(position.BottomLeft).Y;

        var rotation = position.CardinalDirection.ToYRotation();

        return Matrix4X4.CreateScale(sw, sh, sd)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(
                   position.BottomLeft.X + inset,
                   y,
                   position.BottomLeft.Z + inset)
            ;
    }
}
