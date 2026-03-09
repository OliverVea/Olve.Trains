using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class AddWagonHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainWagonService trainWagonService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID to add a wagon to", true);
    private static readonly CommandArgument BlueprintArgument = new("blueprint", "The wagon blueprint ID", true);

    public override string Verb => "add-wagon";
    public override string HelpString => "Adds a wagon to a train";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument, BlueprintArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (Result.Concat(
                commandContext.GetId<Train>(TrainArgument),
                commandContext.GetId<WagonBlueprint>(BlueprintArgument))
            .TryPickProblems(out var problems, out var ids))
        {
            return problems;
        }

        var (trainId, blueprintId) = ids;

        if (trainWagonService.AddWagon(trainId, blueprintId).TryPickProblems(out problems, out var wagonId))
        {
            return problems;
        }

        var json = JsonSerializer.Serialize(new { wagonId = wagonId.ToString() });
        return new CommandOutput(json);
    }
}
