using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class RemoveWagonHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainWagonService trainWagonService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID to remove a wagon from", true);
    private static readonly CommandArgument IndexArgument = new("index", "The index of the wagon to remove", true);

    public override string Verb => "remove-wagon";
    public override string HelpString => "Removes a wagon from a train by index";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument, IndexArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Train>(TrainArgument).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        var indexString = commandContext.Arguments.GetValueOrDefault(IndexArgument.Key, "");
        if (!int.TryParse(indexString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return new ResultProblem("Invalid wagon index '{0}'", indexString);
        }

        if (trainWagonService.RemoveWagon(trainId, index).TryPickProblems(out problems))
        {
            return problems;
        }

        return new CommandOutput("Removed wagon at index " + index);
    }
}
