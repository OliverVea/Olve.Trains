using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    MeshLoadingManager meshLoadingManager,
    MeshRenderingService meshRenderingService,
    FootprintRenderingService footprintRenderingService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingMeshBlueprintService buildingMeshBlueprintService,
    BuildingPositionService buildingPositionService,
    StationBlueprintService stationBlueprintService,
    ResidenceBlueprintService residenceBlueprintService,
    IndustryBlueprintService industryBlueprintService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService, footprintRenderingService]);

    private const float FootprintBorderWidth = 0.06f;
    private const float FootprintCornerRadius = 0.15f;
    private const float FootprintFillOpacity = 0.2f;

    private record struct BlueprintRenderInfo(
        MeshRenderingService.MeshGroupHandle MeshGroup,
        FootprintRenderingService.FootprintGroupHandle FootprintGroup);

    private readonly Dictionary<Id<BuildingBlueprint>, BlueprintRenderInfo> _blueprintRenderInfos = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();
    private readonly Dictionary<Id<Building>, FootprintRenderingService.FootprintInstanceHandle> _footprintInstanceIds = new();
    private readonly Dictionary<Id<Building>, MeshRenderingService.MeshInstanceHandle> _ghostInstanceIds = new();

    private Shaders.Default.Vertex[] _cubeVertices = null!;
    private uint[] _cubeIndices = null!;

    private MeshRenderingService.MeshGroupHandle _ghostGroupHandle;

    public Result Load()
    {
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

        var worldMatrix = buildingPositionService.ComputeBuildingWorldMatrix(
            blueprint.Footprint, building.Position, building.BlueprintId);

        if (meshRenderingService.AddInstance(renderInfo.MeshGroup, worldMatrix)
            .TryPickProblems(out problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add building instance for '{0}'", buildingId);
        }

        _instanceIds[buildingId] = instanceHandle;

        var footprint = blueprint.Footprint;
        var footprintScale = new Vector3D<float>(footprint.Width, 1f, footprint.Depth);
        var footprintMatrix = buildingPositionService.ComputeFootprintWorldMatrix(
            blueprint.Footprint, building.Position, footprintScale, yOffset: 0.01f);

        var color = ComputeFootprintColor(building.BlueprintId);
        var size = new Vector2D<float>(footprint.Width, footprint.Depth);
        var border = new Vector4D<float>(FootprintBorderWidth);
        var borderColor = new Vector4D<float>(color.X, color.Y, color.Z, 1f);
        var tint = new Vector4D<float>(color.X, color.Y, color.Z, FootprintFillOpacity);
        var radius = new Vector4D<float>(FootprintCornerRadius);

        if (footprintRenderingService.AddInstance(
                renderInfo.FootprintGroup, footprintMatrix, size, tint, border, borderColor, radius)
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
        var ghostScale = new Vector3D<float>(footprint.Width, footprint.Height, footprint.Depth);
        var worldMatrix = buildingPositionService.ComputeFootprintWorldMatrix(footprint, position, ghostScale);

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

        var ghostScale = new Vector3D<float>(footprint.Width, footprint.Height, footprint.Depth);
        var worldMatrix = buildingPositionService.ComputeFootprintWorldMatrix(footprint, position, ghostScale);

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
            if (footprintRenderingService.RemoveInstance(footprintHandle).TryPickProblems(out var problems))
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

        Result<MeshRenderingService.MeshGroupHandle> meshGroupResult;

        if (buildingMeshBlueprintService.TryGetProperties(blueprintId, out var meshProps)
            && meshProps.MeshPath is { } meshPath)
        {
            if (meshLoadingManager.LoadMesh(meshPath).TryPickProblems(out var loadProblems, out var meshId))
            {
                return loadProblems.Prepend("Failed to load mesh for blueprint '{0}'", blueprintId);
            }

            meshGroupResult = meshRenderingService.RegisterMeshGroup(meshId, GetTextureForBlueprint(blueprintId));
        }
        else
        {
            meshGroupResult = meshRenderingService.RegisterMeshGroup(
                _cubeVertices, _cubeIndices,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.7f, 0.7f, 0.7f)));
        }

        if (meshGroupResult.TryPickProblems(out var problems, out var meshGroup))
        {
            return problems.Prepend("Failed to register mesh group for blueprint '{0}'", blueprintId);
        }

        var color = ComputeFootprintColor(blueprintId);
        var borderColor = new Vector4D<float>(color.X, color.Y, color.Z, 1f);
        var tint = new Vector4D<float>(color.X, color.Y, color.Z, FootprintFillOpacity);
        var border = new Vector4D<float>(FootprintBorderWidth);
        var radius = new Vector4D<float>(FootprintCornerRadius);

        if (footprintRenderingService.RegisterGroup(tint, border, borderColor, radius)
            .TryPickProblems(out problems, out var footprintGroup))
        {
            return problems.Prepend("Failed to register footprint group for blueprint '{0}'", blueprintId);
        }

        info = new BlueprintRenderInfo(meshGroup, footprintGroup);
        _blueprintRenderInfos[blueprintId] = info;

        return info;
    }

    private static AssetPath<TextureData<RGBA>>? GetTextureForBlueprint(Id<BuildingBlueprint> blueprintId)
    {
        if (blueprintId == BuildingBlueprintCatalog.Station) return Textures.SimpleTrains_Texture_01;
        if (blueprintId == BuildingBlueprintCatalog.Residential) return Textures.Building01a;
        return null;
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
}
