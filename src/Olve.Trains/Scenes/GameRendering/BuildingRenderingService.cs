using Olve.Engine3D;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    MeshRenderingService meshRenderingService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    StationBlueprintService stationBlueprintService,
    ResidenceBlueprintService residenceBlueprintService,
    IndustryBlueprintService industryBlueprintService,
    GridService gridService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    private record struct BlueprintRenderInfo(
        MeshRenderingService.MeshGroupHandle MeshGroup,
        MeshRenderingService.MeshGroupHandle FootprintGroup,
        bool IsCentered,
        Matrix4X4<float> ModelTransform);

    private readonly Dictionary<Id<BuildingBlueprint>, BlueprintRenderInfo> _blueprintRenderInfos = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _footprintInstanceIds = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _ghostInstanceIds = new();

    private Shaders.Default.Vertex[] _quadVertices = null!;
    private uint[] _quadIndices = null!;
    private Shaders.Default.Vertex[] _cubeVertices = null!;
    private uint[] _cubeIndices = null!;

    private MeshRenderingService.MeshGroupHandle _ghostGroupHandle;

    public Result Load()
    {
        var up = new Vector3D<float>(0, 1, 0);
        _quadVertices =
        [
            new(new(0, 0, 0), up, new(0, 0)),
            new(new(1, 0, 0), up, new(1, 0)),
            new(new(1, 0, 1), up, new(1, 1)),
            new(new(0, 0, 1), up, new(0, 1)),
        ];
        _quadIndices = [0, 1, 2, 0, 2, 3];

        _cubeVertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(_cubeVertices);
        _cubeIndices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(_cubeIndices);

        if (meshRenderingService.RegisterMeshGroup(
                _cubeVertices, _cubeIndices,
                renderState: RenderState.AlphaBlend,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f),
                    UOpacity: 0.5f,
                    UColorOverride: new Vector3D<float>(0, 0, 0),
                    UColorMix: 0f))
            .TryPickProblems(out var problems, out var ghostGroupHandle))
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

        if (GetOrCreateBlueprintRenderInfo(building.BlueprintId)
            .TryPickProblems(out var problems, out var renderInfo))
        {
            return problems.Prepend("Failed to get render info for blueprint '{0}'", building.BlueprintId);
        }

        var worldMatrix = ComputeBuildingWorldMatrix(blueprint.Footprint, building.Position, renderInfo);

        if (meshRenderingService.AddInstance(renderInfo.MeshGroup, worldMatrix)
            .TryPickProblems(out problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add building instance for '{0}'", buildingId);
        }

        _instanceIds[buildingId] = instanceHandle;

        var footprintMatrix = ComputeFootprintWorldMatrix(blueprint.Footprint, building.Position);

        if (meshRenderingService.AddInstance(renderInfo.FootprintGroup, footprintMatrix)
            .TryPickProblems(out problems, out var footprintHandle))
        {
            return problems.Prepend("Failed to add footprint instance for '{0}'", buildingId);
        }

        _footprintInstanceIds[buildingId] = footprintHandle;

        return Result.Success();
    }

    public Result RegisterGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        var worldMatrix = ComputeCornerOriginWorldMatrix(footprint, position);

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

        var worldMatrix = ComputeCornerOriginWorldMatrix(footprint, position);

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
        var found = false;

        if (_instanceIds.Remove(buildingId, out var instanceHandle))
        {
            if (meshRenderingService.RemoveInstance(instanceHandle).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to remove building instance for '{0}'", buildingId);
            }

            found = true;
        }

        if (_footprintInstanceIds.Remove(buildingId, out var footprintHandle))
        {
            if (meshRenderingService.RemoveInstance(footprintHandle).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to remove footprint instance for '{0}'", buildingId);
            }

            found = true;
        }

        if (_ghostInstanceIds.Remove(buildingId, out var ghostHandle))
        {
            if (meshRenderingService.RemoveInstance(ghostHandle).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to remove ghost instance for '{0}'", buildingId);
            }

            found = true;
        }

        if (!found)
        {
            return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    private Result<BlueprintRenderInfo> GetOrCreateBlueprintRenderInfo(Id<BuildingBlueprint> blueprintId)
    {
        if (_blueprintRenderInfos.TryGetValue(blueprintId, out var info))
        {
            return info;
        }

        bool isCentered;
        var modelTransform = Matrix4X4<float>.Identity;
        Result<MeshRenderingService.MeshGroupHandle> meshGroupResult;

        if (blueprintId == BuildingBlueprintCatalog.Station)
        {
            meshGroupResult = meshRenderingService.RegisterMeshGroup(
                Meshes.SM_Bld_Station_Small_01,
                Textures.SimpleTrains_Texture_01);
            isCentered = true;
            modelTransform = Matrix4X4.CreateTranslation(0f, 0f, 0.1f);
        }
        else if (blueprintId == BuildingBlueprintCatalog.Residential)
        {
            meshGroupResult = meshRenderingService.RegisterMeshGroup(
                Meshes.apartment_small_mesh,
                Textures.Building01a);
            isCentered = true;
        }
        else
        {
            meshGroupResult = meshRenderingService.RegisterMeshGroup(
                _cubeVertices, _cubeIndices,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f)));
            isCentered = false;
        }

        if (meshGroupResult.TryPickProblems(out var problems, out var meshGroup))
        {
            return problems.Prepend("Failed to register mesh group for blueprint '{0}'", blueprintId);
        }

        var color = ComputeFootprintColor(blueprintId);

        if (meshRenderingService.RegisterMeshGroup(
                _quadVertices, _quadIndices,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: color))
            .TryPickProblems(out problems, out var footprintGroup))
        {
            return problems.Prepend("Failed to register footprint group for blueprint '{0}'", blueprintId);
        }

        info = new BlueprintRenderInfo(meshGroup, footprintGroup, isCentered, modelTransform);
        _blueprintRenderInfos[blueprintId] = info;

        return info;
    }

    private Vector3D<float> ComputeFootprintColor(Id<BuildingBlueprint> blueprintId)
    {
        float r = 0, g = 0, b = 0;
        var count = 0;

        if (stationBlueprintService.HasProperties(blueprintId))
        {
            b += 1;
            count++;
        }

        if (residenceBlueprintService.HasProperties(blueprintId))
        {
            g += 1;
            count++;
        }

        if (industryBlueprintService.HasProperties(blueprintId))
        {
            r += 1;
            count++;
        }

        if (count == 0) return new Vector3D<float>(0.5f, 0.5f, 0.5f);
        return new Vector3D<float>(r / count, g / count, b / count);
    }

    private Matrix4X4<float> ComputeBuildingWorldMatrix(
        TileFootprint footprint,
        BuildingPosition position,
        BlueprintRenderInfo renderInfo)
    {
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var y = gridService.ToTileOrigin(position.BottomLeft).Y;
        var rotation = position.CardinalDirection.ToYRotation();

        if (renderInfo.IsCentered)
        {
            var uniformScale = float.Min(w, float.Min(h, d));

            return renderInfo.ModelTransform
                   * Matrix4X4.CreateScale(uniformScale)
                   * Matrix4X4.CreateRotationY(rotation)
                   * Matrix4X4.CreateTranslation(
                       position.BottomLeft.X + w / 2f, y, position.BottomLeft.Z + d / 2f);
        }

        return Matrix4X4.CreateScale(w, h, d)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(position.BottomLeft.X, y, position.BottomLeft.Z);
    }

    private Matrix4X4<float> ComputeFootprintWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        var w = (float)footprint.Width;
        var d = (float)footprint.Depth;
        var y = gridService.ToTileOrigin(position.BottomLeft).Y;
        var rotation = position.CardinalDirection.ToYRotation();

        return Matrix4X4.CreateScale(w, 1f, d)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(position.BottomLeft.X, y + 0.01f, position.BottomLeft.Z);
    }

    private static Matrix4X4<float> ComputeCornerOriginWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;

        return Matrix4X4.CreateScale(w, h, d)
               * Matrix4X4.CreateRotationY(position.CardinalDirection.ToYRotation())
               * Matrix4X4.CreateTranslation(position.BottomLeft.X, 0f, position.BottomLeft.Z);
    }
}
