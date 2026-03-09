using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Commands.GameLogic;

public class DeleteTrainHandlerService(
    ILogger<DeleteTrainHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainService trainService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The ID of the train to delete", true);

    public override string Verb => "delete-train";
    public override string HelpString => "Deletes a train by ID. Example: delete-train train=<train-id>";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Train>(TrainArgument).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        var result = trainService.DeleteTrain(trainId);
        if (result.MapToResult(allowNotFound: false).TryPickProblems(out problems))
        {
            return problems;
        }

        logger.LogInformation("Deleted train {TrainId}", trainId);
        return new CommandOutput($"Deleted train {trainId}");
    }
}
