using Olve.Engine3D;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingValidationService(
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService)
{
    public bool IsValid(BuildingPosition position, TileFootprint footprint)
    {
        var (minX, minZ, maxX, maxZ) = GetBounds(position, footprint);

        foreach (var building in buildingService.Buildings)
        {
            if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var existingBlueprint))
            {
                continue;
            }

            var (eMinX, eMinZ, eMaxX, eMaxZ) = GetBounds(building.Position, existingBlueprint.Footprint);

            if (minX <= eMaxX && maxX >= eMinX && minZ <= eMaxZ && maxZ >= eMinZ)
            {
                return false;
            }
        }

        return true;
    }

    public static (int MinX, int MinZ, int MaxX, int MaxZ) GetBounds(
        BuildingPosition position,
        TileFootprint footprint)
    {
        var x = position.BottomLeft.X;
        var z = position.BottomLeft.Z;
        var w = footprint.Width;
        var d = footprint.Depth;

        return position.CardinalDirection switch
        {
            CardinalDirection.North => (x, z, x + w - 1, z + d - 1),
            CardinalDirection.East => (x, z - w + 1, x + d - 1, z),
            CardinalDirection.South => (x - w + 1, z - d + 1, x, z),
            CardinalDirection.West => (x - d + 1, z, x, z + w - 1),
            _ => (x, z, x + w - 1, z + d - 1),
        };
    }
}
