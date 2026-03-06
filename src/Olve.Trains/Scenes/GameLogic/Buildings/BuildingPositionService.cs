using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Terrain;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingPositionService(
    GridService gridService,
    BuildingMeshBlueprintService buildingMeshBlueprintService)
{
    public Matrix4X4<float> ComputeBuildingWorldMatrix(
        TileFootprint footprint,
        BuildingPosition position,
        Id<BuildingBlueprint> blueprintId)
    {
        var modelTransform = buildingMeshBlueprintService.TryGetProperties(blueprintId, out var meshProps)
            ? meshProps.ModelTransform
            : Matrix4X4<float>.Identity;

        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var uniformScale = float.Min(w, float.Min(h, d));

        var y = gridService.ToTileOrigin(position.BottomLeft).Y;
        var rotation = position.CardinalDirection.ToYRotation();

        return modelTransform
               * Matrix4X4.CreateScale(uniformScale)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(
                   position.BottomLeft.X + w / 2f, y, position.BottomLeft.Z + d / 2f);
    }

    public Matrix4X4<float> ComputeFootprintWorldMatrix(
        TileFootprint footprint,
        BuildingPosition position,
        Vector3D<float> scale,
        float yOffset = 0f)
    {
        var w = (float)footprint.Width;
        var d = (float)footprint.Depth;
        var y = gridService.ToTileOrigin(position.BottomLeft).Y + yOffset;
        var rotation = position.CardinalDirection.ToYRotation();

        // Unit quad/cube vertices span (0,0,0)-(1,1,1).
        // Center on X/Z before scaling so rotation is around the footprint center.
        return Matrix4X4.CreateTranslation(-0.5f, 0f, -0.5f)
               * Matrix4X4.CreateScale(scale)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(
                   position.BottomLeft.X + w / 2f, y, position.BottomLeft.Z + d / 2f);
    }
}
