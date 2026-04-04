using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Math;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Silk.NET.Maths;

namespace Olve.Trains.Commands.GameLogic;

public class ListEntitiesHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CollisionSystem collisionSystem,
    BuildingCollisionService buildingCollisionService,
    TrackCollisionService trackCollisionService,
    TrainCollisionService trainCollisionService,
    EnvironmentalObjectCollisionService environmentalObjectCollisionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument MinArgument = new("min", "Minimum x,z corner of the query box (e.g. min=0,0)", false);
    private static readonly CommandArgument MaxArgument = new("max", "Maximum x,z corner of the query box (e.g. max=4,4)", false);
    private static readonly CommandArgument TypeArgument = new("type", "Comma-separated entity types to include: building,track,train,environment (default: all)", false);

    private static readonly Dictionary<string, Id<ColliderGroup>> TypeNameToGroup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["building"] = ColliderGroups.Building,
        ["track"] = ColliderGroups.Track,
        ["train"] = ColliderGroups.Train,
        ["environment"] = ColliderGroups.Environment,
    };

    private static readonly Dictionary<Id<ColliderGroup>, string> GroupToTypeName = TypeNameToGroup
        .ToDictionary(x => x.Value, x => x.Key);

    public override string Verb => "list-entities";
    public override string HelpString => "Lists entities in an x-z box. Example: list-entities min=0,0 max=4,4 type=building,environment";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [MinArgument, MaxArgument, TypeArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (ParseQueryBox(commandContext).TryPickProblems(out var problems, out var queryBox))
        {
            return problems;
        }

        HashSet<Id<ColliderGroup>>? allowedGroups = null;
        var typeArg = commandContext.GetOptionalArgument(TypeArgument);
        if (typeArg is not null)
        {
            allowedGroups = [];
            foreach (var typeName in typeArg.Split(','))
            {
                if (!TypeNameToGroup.TryGetValue(typeName.Trim(), out var group))
                {
                    return new ResultProblem("Unknown entity type '{0}'. Valid types: building, track, train, environment", typeName.Trim());
                }
                allowedGroups.Add(group);
            }
        }

        var hits = collisionSystem.QueryOverlapAABB(queryBox, allowedGroups);

        var seen = new HashSet<string>();
        var entities = new List<object>();
        foreach (var hit in hits)
        {
            if (!GroupToTypeName.TryGetValue(hit.Group, out var typeName))
            {
                continue;
            }

            var entityId = ResolveEntityId(hit);
            if (entityId is null) continue;
            if (!seen.Add(entityId)) continue;

            entities.Add(new
            {
                type = typeName,
                entityId,
            });
        }

        var json = JsonSerializer.Serialize(new { entities, count = entities.Count });
        return new CommandOutput(json);
    }

    private string? ResolveEntityId(OverlapHit hit)
    {
        if (hit.Group == ColliderGroups.Building
            && buildingCollisionService.TryGetBuildingId(hit.ColliderId, out var buildingId))
        {
            return buildingId.ToString();
        }

        if (hit.Group == ColliderGroups.Track
            && trackCollisionService.TryGetTrackId(hit.ColliderId, out var trackId))
        {
            return trackId.ToString();
        }

        if (hit.Group == ColliderGroups.Train
            && trainCollisionService.TryGetTrainId(hit.ColliderId, out var trainId))
        {
            return trainId.ToString();
        }

        if (hit.Group == ColliderGroups.Environment
            && environmentalObjectCollisionService.TryGetObjectId(hit.ColliderId, out var objectId))
        {
            return objectId.ToString();
        }

        return null;
    }

    private static Result<AABB> ParseQueryBox(CommandContext commandContext)
    {
        var minArg = commandContext.GetOptionalArgument(MinArgument);
        var maxArg = commandContext.GetOptionalArgument(MaxArgument);

        float minX = float.MinValue, minZ = float.MinValue;
        float maxX = float.MaxValue, maxZ = float.MaxValue;

        if (minArg is not null)
        {
            if (TryParseXZ(minArg).TryPickProblems(out var problems, out var min))
            {
                return problems;
            }
            minX = min.x;
            minZ = min.z;
        }

        if (maxArg is not null)
        {
            if (TryParseXZ(maxArg).TryPickProblems(out var problems, out var max))
            {
                return problems;
            }
            maxX = max.x;
            maxZ = max.z;
        }

        return new AABB(
            new Vector3D<float>(minX, float.MinValue, minZ),
            new Vector3D<float>(maxX, float.MaxValue, maxZ));
    }

    private static Result<(float x, float z)> TryParseXZ(string input)
    {
        var parts = input.Split(',');
        if (parts.Length != 2)
        {
            return new ResultProblem("Expected 2 comma-separated values (x,z), got '{0}'", input);
        }

        if (!float.TryParse(parts[0].Trim(), NumberFormatInfo.InvariantInfo, out var x)
            || !float.TryParse(parts[1].Trim(), NumberFormatInfo.InvariantInfo, out var z))
        {
            return new ResultProblem("Failed to parse x,z values from '{0}'", input);
        }

        return (x, z);
    }
}
