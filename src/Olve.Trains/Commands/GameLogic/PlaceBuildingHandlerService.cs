using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;

namespace Olve.Trains.Commands.GameLogic;

public class PlaceBuildingHandlerService(
    ILogger<PlaceBuildingHandlerService> logger,
    BuildingBlueprintService blueprintService,
    BuildingService buildingService,
    CommandHandlerServiceCollection commandHandlerServiceCollection) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PositionArgument = new("pos", "Tile position as x,z (y defaults to terrain height) or x,y,z", true);
    private static readonly CommandArgument TypeArgument = new("type", "Building type: residential, station, depot, forest, mine, sawmill (default: residential)");
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
        if (commandContext.GetRequiredArgument(PositionArgument).Bind(s => s.ParseTilePosition())
            .TryPickProblems(out var problems, out var tilePosition))
        {
            return problems;
        }

        var typeArg = commandContext.GetOptionalArgument(TypeArgument) ?? "residential";
        var blueprintId = typeArg.ToLowerInvariant() switch
        {
            "residential" or "res" => BuildingBlueprintCatalog.Residential,
            "station" => BuildingBlueprintCatalog.Station,
            "forest" => BuildingBlueprintCatalog.Forest,
            "mine" => BuildingBlueprintCatalog.Mine,
            "sawmill" => BuildingBlueprintCatalog.Sawmill,
            "depot" => BuildingBlueprintCatalog.Depot,
            _ => default,
        };

        if (blueprintId == default)
        {
            return new ResultProblem("Unknown building type '{0}'. Use: residential, station, depot, forest, mine, sawmill", typeArg);
        }

        var direction = CardinalDirection.North;
        if (commandContext.GetOptionalArgument(DirectionArgument) is {} dirArg)
        {
            if (dirArg.ParseDirection().TryPickProblems(out problems, out direction))
                return problems;
        }

        if (!blueprintService.TryGetBlueprint(blueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found for type '{0}'", typeArg);
        }

        BuildingPosition position = new(tilePosition, direction);

        if (buildingService.AddBuilding(blueprintId, position).TryPickProblems(out var addProblems, out var buildingId))
        {
            return addProblems;
        }

        logger.LogInformation("Placed {Type} building '{BuildingId}' at {Position}", blueprint.Description, buildingId, tilePosition);
        return new CommandOutput($"Placed {blueprint.Description} building: {buildingId}");
    }
}
