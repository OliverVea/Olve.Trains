using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Commands.GameLogic;

public class PlaceTrainHandlerService(
    ILogger<PlaceTrainHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService,
    TrainService trainService,
    TrainPositionService trainPositionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new ("track", "The track to place the train on.", true);
    private static readonly CommandArgument TrainIdArgument = new("train", "The train to place. If empty, a new train will be created.");
    private static readonly CommandArgument SpeedArgument = new("speed", "The speed of the train.");

    public override string Verb => "place-train";
    public override string HelpString => "Places the specified train on the specified track";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainIdArgument, TrackArgument, SpeedArgument];
    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var trackIdResult = commandContext.GetId<Track>(TrackArgument);

        var trainIdResult = commandContext.Arguments.ContainsKey(TrainIdArgument.Key)
            ? commandContext.GetId<Train>(TrainIdArgument)
            : CreateTrain();

        if (Result.Concat(trackIdResult, trainIdResult).TryPickProblems(out var problems, out var trackAndTrainId))
        {
            return problems;
        }

        var (trackId, trainId) = trackAndTrainId;

        if (!trackService.TrackExists(trackId))
        {
            return new ResultProblem("Track with id '{0}' doesnt exist", trackId);
        }

        var speedString = commandContext.Arguments.GetValueOrDefault(SpeedArgument.Key, "0");
        if (!float.TryParse(speedString, NumberFormatInfo.InvariantInfo, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }

        TrackPoint trackPoint = new(trackId, 0);
        TrainTrackPosition trainTrackPosition = new(trackPoint, speed, speed);

        trainPositionService.SetTrackPosition(trainId, trainTrackPosition);

        logger.LogInformation("Placed train '{TrainId}' on track with id '{TrackId}' with position '{TrainTrackPosition}'", trainId, trackId, trainTrackPosition);

        var json = JsonSerializer.Serialize(new { trainId = trainId.ToString() });
        return new CommandOutput(json);
    }

    private Result<Id<Train>> CreateTrain()
    {
        var trainName = "Train_" + trainService.Count + 1;
        return trainService.AddTrain(trainName);
    }
}
