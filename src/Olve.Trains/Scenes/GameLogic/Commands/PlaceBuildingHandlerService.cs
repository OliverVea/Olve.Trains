using System.Globalization;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class PlaceBuildingHandlerService(
    ILogger<PlaceBuildingHandlerService> logger,
    BuildingBlueprintService blueprintService,
    BuildingService buildingService,
    CommandHandlerServiceCollection commandHandlerServiceCollection) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PositionArgument = new("pos", "Tile position as x,z (y defaults to terrain height) or x,y,z", true);
    private static readonly CommandArgument TypeArgument = new("type", "Building type: residential, station (default: residential)");
    private static readonly CommandArgument DirectionArgument = new("dir", "Direction: north, south, east, west (default: north)");

    public override string Verb => "place-building";
    public override string HelpString => """
        Places a building at the specified tile position.
        Example: place-building pos=10,5
        Example: place-building pos=10,0,5 type=station dir=east
        """;
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [PositionArgument, TypeArgument, DirectionArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (TryParseTilePosition(commandContext.GetArgument(PositionArgument)!, out var tilePosition)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        var typeArg = commandContext.GetArgument(TypeArgument) ?? "residential";
        var blueprintId = typeArg.ToLowerInvariant() switch
        {
            "residential" or "res" => BuildingBlueprintCatalog.Residential,
            "station" => BuildingBlueprintCatalog.Station,
            _ => default,
        };

        if (blueprintId == default)
        {
            return new ResultProblem("Unknown building type '{0}'. Use: residential, station", typeArg);
        }

        var dirArg = commandContext.GetArgument(DirectionArgument) ?? "north";
        var direction = dirArg.ToLowerInvariant() switch
        {
            "north" or "n" => CardinalDirection.North,
            "south" or "s" => CardinalDirection.South,
            "east" or "e" => CardinalDirection.East,
            "west" or "w" => CardinalDirection.West,
            _ => CardinalDirection.None,
        };

        if (direction == CardinalDirection.None)
        {
            return new ResultProblem("Invalid direction '{0}'. Use: north, south, east, west (or n, s, e, w)", dirArg);
        }

        if (!blueprintService.TryGetBlueprint(blueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found for type '{0}'", typeArg);
        }

        BuildingPosition position = new(tilePosition, direction);
        var buildingId = buildingService.AddBuilding(blueprintId, position);

        logger.LogInformation("Placed {Type} building '{BuildingId}' at {Position}", blueprint.Description, buildingId, tilePosition);
        return new CommandOutput($"Placed {blueprint.Description} building: {buildingId}");
    }

    private static Result TryParseTilePosition(string input, out TilePosition result)
    {
        result = default;
        var parts = input.Split(',');

        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0].Trim(), NumberFormatInfo.InvariantInfo, out var x)
                || !int.TryParse(parts[1].Trim(), NumberFormatInfo.InvariantInfo, out var z))
            {
                return new ResultProblem("Could not parse tile position from '{0}'", input);
            }

            result = new TilePosition(x, 0, z);
            return Result.Success();
        }

        if (parts.Length == 3)
        {
            if (!int.TryParse(parts[0].Trim(), NumberFormatInfo.InvariantInfo, out var x)
                || !int.TryParse(parts[1].Trim(), NumberFormatInfo.InvariantInfo, out var y)
                || !int.TryParse(parts[2].Trim(), NumberFormatInfo.InvariantInfo, out var z))
            {
                return new ResultProblem("Could not parse tile position from '{0}'", input);
            }

            result = new TilePosition(x, y, z);
            return Result.Success();
        }

        return new ResultProblem("Expected 2 or 3 comma-separated values (x,z or x,y,z), got '{0}'", input);
    }
}
