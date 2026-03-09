using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class ListWagonsHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainWagonService trainWagonService,
    WagonInventoryService wagonInventoryService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID to list wagons for", true);

    public override string Verb => "list-wagons";
    public override string HelpString => "Lists all wagons attached to a train";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Train>(TrainArgument).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        var wagons = trainWagonService.GetWagons(trainId);
        var wagonList = new List<object>();

        foreach (var wagon in wagons)
        {
            wagonInventoryService.TryGetInventory(wagon.Id, out var inventoryId);
            wagonList.Add(new
            {
                wagonId = wagon.Id.ToString(),
                blueprintId = wagon.BlueprintId.ToString(),
                inventoryId = inventoryId.ToString(),
            });
        }

        var json = JsonSerializer.Serialize(new { wagons = wagonList });
        return new CommandOutput(json);
    }
}
