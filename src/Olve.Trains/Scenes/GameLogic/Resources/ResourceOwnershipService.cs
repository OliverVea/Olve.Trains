using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;

namespace Olve.Trains.Scenes.GameLogic.Resources;

public class ResourceOwnershipService(
    ILogger<ResourceOwnershipService> logger,
    ResourceService resourceService,
    IndustryService industryService,
    IndustryBlueprintService industryBlueprintService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService) : ISceneService
{
    private const float K = 5f / 6f;

    private readonly Dictionary<Id<Industry>, float> _productivity = new();
    private bool _dirty = true;

    public void MarkDirty() => _dirty = true;

    public float GetProductivity(Id<Industry> industryId)
        => _productivity.GetValueOrDefault(industryId);

    public Result Update()
    {
        if (!_dirty) return Result.Success();
        _dirty = false;

        Recalculate();
        return Result.Success();
    }

    private void Recalculate()
    {
        _productivity.Clear();

        // Collect extractive industries with their positions and properties
        var extractiveIndustries = new List<(Industry Industry, IndustryProperties Properties, float X, float Z)>();

        foreach (var industry in industryService.Industries)
        {
            if (!buildingService.TryGetBuilding(industry.BuildingId, out var building)) continue;
            if (!industryBlueprintService.TryGetProperties(building.BlueprintId, out var props)) continue;
            if (props.RequiredResourceType is null || props.HarvestRange is null) continue;

            if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint)) continue;

            var w = (float)blueprint.Footprint.Width;
            var d = (float)blueprint.Footprint.Depth;
            var cx = building.Position.BottomLeft.X + w / 2f;
            var cz = building.Position.BottomLeft.Z + d / 2f;

            extractiveIndustries.Add((industry, props, cx, cz));
        }

        if (extractiveIndustries.Count == 0) return;

        // Assign each resource to the nearest extractive industry of matching type within range
        var owned = new Dictionary<int, List<float>>(); // industry list index -> list of distances
        for (var i = 0; i < extractiveIndustries.Count; i++)
        {
            owned[i] = new List<float>();
        }

        foreach (var resource in resourceService.Resources)
        {
            var bestIndex = -1;
            var bestDist = float.MaxValue;

            for (var i = 0; i < extractiveIndustries.Count; i++)
            {
                var (_, props, cx, cz) = extractiveIndustries[i];
                if (props.RequiredResourceType != resource.ResourceTypeId) continue;

                var dx = resource.Position.X - cx;
                var dz = resource.Position.Z - cz;
                var dist = MathF.Sqrt(dx * dx + dz * dz);

                if (dist > props.HarvestRange!.Value) continue;
                if (dist >= bestDist) continue;

                bestDist = dist;
                bestIndex = i;
            }

            if (bestIndex >= 0)
            {
                owned[bestIndex].Add(bestDist);
            }
        }

        // Compute productivity for each extractive industry
        for (var i = 0; i < extractiveIndustries.Count; i++)
        {
            var (industry, props, _, _) = extractiveIndustries[i];
            var distances = owned[i];
            var n = distances.Count;

            if (n == 0)
            {
                _productivity[industry.Id] = 0f;
                continue;
            }

            var range = props.HarvestRange!.Value;
            var countFactor = MathF.Pow(n, K - 1f); // N^(k-1)
            var distanceSum = 0f;

            foreach (var d in distances)
            {
                var normalizedDist = d / range;
                distanceSum += 1f - normalizedDist * normalizedDist;
            }

            var productivity = countFactor * distanceSum;
            _productivity[industry.Id] = productivity;

            logger.LogDebug(
                "Industry {IndustryId} owns {Count} resources, productivity = {Productivity:F3}",
                industry.Id, n, productivity);
        }
    }
}
