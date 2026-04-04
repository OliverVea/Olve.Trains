using System.Globalization;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Commands.GameLogic;

public class SetTrainSpeedHandlerService(
    ILogger<SetTrainSpeedHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainPositionService trainPositionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID.", true);
    private static readonly CommandArgument SpeedArgument = new("speed", "The new speed.", true);

    public override string Verb => "set-train-speed";
    public override string HelpString => "Sets the speed of a train";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument, SpeedArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var trainIdResult = commandContext.GetId<Train>(TrainArgument);

        if (trainIdResult.TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        if (!trainPositionService.TryGetMotion(trainId, out var motion))
        {
            return new ResultProblem("Train '{0}' has no motion state", trainId);
        }

        var speedString = commandContext.Arguments.GetValueOrDefault(SpeedArgument.Key, "0");
        if (!float.TryParse(speedString, NumberFormatInfo.InvariantInfo, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }

        trainPositionService.SetMotion(trainId, motion with { TargetSpeed = speed, UserTargetSpeed = speed });

        logger.LogInformation("Set train '{TrainId}' target speed to {Speed}", trainId, speed);

        return new CommandOutput($"Train {trainId} target speed set to {speed}");
    }
}
