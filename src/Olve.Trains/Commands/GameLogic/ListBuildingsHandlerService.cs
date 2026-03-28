using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;

namespace Olve.Trains.Commands.GameLogic;

public class ListBuildingsHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-buildings";
    public override string HelpString => "Lists all buildings with their type, position, and direction";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var buildings = buildingService.Buildings
            .Select(b =>
            {
                var typeName = buildingBlueprintService.TryGetBlueprint(b.BlueprintId, out var blueprint)
                    ? blueprint.Description
                    : "Unknown";

                return new
                {
                    buildingId = b.Id.ToString(),
                    type = typeName,
                    position = new
                    {
                        x = b.Position.BottomLeft.X,
                        y = b.Position.BottomLeft.Y,
                        z = b.Position.BottomLeft.Z,
                    },
                    direction = b.Position.CardinalDirection.ToString().ToLowerInvariant(),
                };
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new { buildings });
        return new CommandOutput(json);
    }
}
