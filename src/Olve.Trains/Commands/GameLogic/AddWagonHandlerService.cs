using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class AddWagonHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainWagonService trainWagonService,
    WagonBlueprintService wagonBlueprintService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID to add a wagon to", true);
    private static readonly CommandArgument BlueprintArgument = new("blueprint", "Wagon blueprint ID or name (e.g. 'goods')", true);

    public override string Verb => "add-wagon";
    public override string HelpString => "Adds a wagon to a train. Blueprint can be an ID or a name like 'goods'.";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument, BlueprintArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Train>(TrainArgument).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        if (commandContext.GetRequiredArgument(BlueprintArgument).TryPickProblems(out problems, out var blueprintArg))
        {
            return problems;
        }

        Id<WagonBlueprint> blueprintId;
        if (Id.TryParse<WagonBlueprint>(blueprintArg, out var parsedId))
        {
            blueprintId = parsedId;
        }
        else
        {
            // Try name-based lookup: "goods" → "wagon/goods"
            var name = blueprintArg.ToLowerInvariant();
            blueprintId = Id.FromName<WagonBlueprint>($"wagon/{name}");
            if (!wagonBlueprintService.TryGet(blueprintId, out _))
            {
                return new ResultProblem("Unknown wagon blueprint '{0}'. Try a name like 'goods' or a valid ID.", blueprintArg);
            }
        }

        if (trainWagonService.AddWagon(trainId, blueprintId).TryPickProblems(out problems, out var wagonId))
        {
            return problems;
        }

        var json = JsonSerializer.Serialize(new { wagonId = wagonId.ToString() });
        return new CommandOutput(json);
    }
}
