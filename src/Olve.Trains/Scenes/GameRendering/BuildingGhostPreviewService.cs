using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingGhostPreviewService(
    MeshLoadingManager meshLoadingManager,
    MeshRenderingService meshRenderingService,
    FootprintRenderingService footprintRenderingService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingMeshBlueprintService buildingMeshBlueprintService,
    BuildingPositionService buildingPositionService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService, footprintRenderingService]);

    private const float FootprintBorderWidth = 0.06f;
    private const float FootprintCornerRadius = 0.15f;
    private const float FootprintFillOpacity = 0.3f;

    private const float GhostOpacity = 0.5f;
    private const float InvalidOpacity = 0.7f;
    private static readonly Vector3D<float> InvalidColor = new(1, 0, 0);
    private static readonly Vector3D<float> NeutralColor = new(0.7f, 0.7f, 0.7f);
    private static readonly Vector3D<float> GhostFootprintColor = new(0.5f, 0.5f, 0.5f);

    public record struct GhostPreviewState(
        bool Show,
        bool Valid,
        BuildingPosition Position);

    private record struct GhostRenderInfo(
        MeshRenderingService.MeshGroupHandle MeshGroup,
        MeshRenderingService.MeshInstanceHandle MeshInstance,
        FootprintRenderingService.FootprintGroupHandle FootprintGroup,
        FootprintRenderingService.FootprintInstanceHandle FootprintInstance);

    private readonly Dictionary<Id<BuildingBlueprint>, GhostRenderInfo> _renderInfos = new();
    private readonly Dictionary<Id<BuildingBlueprint>, GhostPreviewState> _states = new();

    private Shaders.Default.Vertex[] _cubeVertices = null!;
    private uint[] _cubeIndices = null!;

    public Result Load()
    {
        _cubeVertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(_cubeVertices);
        _cubeIndices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(_cubeIndices);

        return Result.Success();
    }

    public void Update(Id<BuildingBlueprint> blueprintId, Func<GhostPreviewState, GhostPreviewState> update)
    {
        var oldState = _states.GetValueOrDefault(blueprintId);
        var newState = update(oldState);

        if (oldState == newState) return;

        _states[blueprintId] = newState;

        if (!EnsureRegistered(blueprintId)) return;
        var info = _renderInfos[blueprintId];

        if (!buildingBlueprintService.TryGetBlueprint(blueprintId, out var blueprint)) return;

        if (oldState.Valid != newState.Valid || (!oldState.Show && newState.Show))
        {
            UpdateAppearance(info, newState.Valid);
        }

        if (newState.Show && newState.Position.CardinalDirection is not CardinalDirection.None)
        {
            UpdateTransform(info, blueprintId, blueprint.Footprint, newState.Position);
        }
        else if (!newState.Show && oldState.Show)
        {
            HideGhost(info);
        }
    }

    private void UpdateAppearance(GhostRenderInfo info, bool valid)
    {
        if (valid)
        {
            meshRenderingService.UpdateGroupParameters(info.MeshGroup,
                new Shaders.Default.EntityParameters(
                    UColor: NeutralColor,
                    UOpacity: GhostOpacity,
                    UColorOverride: new Vector3D<float>(0, 0, 0),
                    UColorMix: 0f));
        }
        else
        {
            meshRenderingService.UpdateGroupParameters(info.MeshGroup,
                new Shaders.Default.EntityParameters(
                    UColor: NeutralColor,
                    UOpacity: InvalidOpacity,
                    UColorOverride: InvalidColor,
                    UColorMix: 1.0f));
        }
    }

    private void UpdateTransform(
        GhostRenderInfo info,
        Id<BuildingBlueprint> blueprintId,
        TileFootprint footprint,
        BuildingPosition position)
    {
        var meshMatrix = buildingPositionService.ComputeBuildingWorldMatrix(footprint, position, blueprintId);
        meshRenderingService.UpdateInstance(info.MeshInstance, meshMatrix);

        var footprintScale = new Vector3D<float>(footprint.Width, 1f, footprint.Depth);
        var footprintMatrix = buildingPositionService.ComputeFootprintWorldMatrix(
            footprint, position, footprintScale, yOffset: 0.01f);
        var size = new Vector2D<float>(footprint.Width, footprint.Depth);
        var color = GhostFootprintColor;
        var border = new Vector4D<float>(FootprintBorderWidth);
        var borderColor = new Vector4D<float>(color.X, color.Y, color.Z, 1f);
        var tint = new Vector4D<float>(color.X, color.Y, color.Z, FootprintFillOpacity);
        var radius = new Vector4D<float>(FootprintCornerRadius);

        footprintRenderingService.UpdateInstance(
            info.FootprintInstance, footprintMatrix, size, tint, border, borderColor, radius);
    }

    private void HideGhost(GhostRenderInfo info)
    {
        // Move the ghost far away to hide it
        var hiddenMatrix = Matrix4X4.CreateTranslation(0f, -1000f, 0f);
        meshRenderingService.UpdateInstance(info.MeshInstance, hiddenMatrix);

        var hiddenFootprint = new Vector2D<float>(0, 0);
        var zero4 = new Vector4D<float>(0, 0, 0, 0);
        footprintRenderingService.UpdateInstance(
            info.FootprintInstance, hiddenMatrix, hiddenFootprint, zero4, zero4, zero4, zero4);
    }

    private bool EnsureRegistered(Id<BuildingBlueprint> blueprintId)
    {
        if (_renderInfos.ContainsKey(blueprintId)) return true;

        if (!buildingBlueprintService.TryGetBlueprint(blueprintId, out _)) return false;

        var meshGroupResult = CreateMeshGroup(blueprintId);
        if (meshGroupResult.TryPickProblems(out _, out var meshGroup)) return false;

        var meshInstanceResult = meshRenderingService.AddInstance(meshGroup, Matrix4X4.CreateTranslation(0f, -1000f, 0f));
        if (meshInstanceResult.TryPickProblems(out _, out var meshInstance)) return false;

        var footprintGroupResult = footprintRenderingService.RegisterGroup(
            new Vector4D<float>(0, 0, 0, 0),
            new Vector4D<float>(FootprintBorderWidth),
            new Vector4D<float>(0, 0, 0, 0),
            new Vector4D<float>(FootprintCornerRadius));
        if (footprintGroupResult.TryPickProblems(out _, out var footprintGroup)) return false;

        var zero4 = new Vector4D<float>(0, 0, 0, 0);
        var footprintInstanceResult = footprintRenderingService.AddInstance(
            footprintGroup, Matrix4X4.CreateTranslation(0f, -1000f, 0f),
            new Vector2D<float>(0, 0), zero4, zero4, zero4, zero4);
        if (footprintInstanceResult.TryPickProblems(out _, out var footprintInstance)) return false;

        _renderInfos[blueprintId] = new GhostRenderInfo(meshGroup, meshInstance, footprintGroup, footprintInstance);
        return true;
    }

    private Result<MeshRenderingService.MeshGroupHandle> CreateMeshGroup(Id<BuildingBlueprint> blueprintId)
    {
        if (buildingMeshBlueprintService.TryGetProperties(blueprintId, out var meshProps))
        {
            if (meshLoadingManager.LoadMesh(meshProps.MeshPath).TryPickProblems(out var loadProblems, out var meshId))
            {
                return loadProblems.Prepend("Failed to load mesh for ghost blueprint '{0}'", blueprintId);
            }

            return meshRenderingService.RegisterMeshGroup(
                meshId,
                meshProps.TexturePath,
                renderState: RenderState.AlphaBlend,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: NeutralColor,
                    UOpacity: GhostOpacity,
                    UColorOverride: new Vector3D<float>(0, 0, 0),
                    UColorMix: 0f));
        }

        return meshRenderingService.RegisterMeshGroup(
            _cubeVertices, _cubeIndices,
            renderState: RenderState.AlphaBlend,
            parameters: new Shaders.Default.EntityParameters(
                UColor: NeutralColor,
                UOpacity: GhostOpacity,
                UColorOverride: new Vector3D<float>(0, 0, 0),
                UColorMix: 0f));
    }

}
