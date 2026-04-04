using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Depots;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Commands.GameLogic;

public class PlaceTrainHandlerService(
    ILogger<PlaceTrainHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService,
    TrainService trainService,
    TrainPositionService trainPositionService,
    DepotService depotService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new ("track", "The track to place the train on.");
    private static readonly CommandArgument DepotArgument = new("depot", "The depot building ID to place the train at.");
    private static readonly CommandArgument TrainIdArgument = new("train", "The train to place. If empty, a new train will be created.");
    private static readonly CommandArgument SpeedArgument = new("speed", "The speed of the train.");

    public override string Verb => "place-train";
    public override string HelpString => "Places a train on a track or at a depot. Provide either track= or depot=.";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainIdArgument, TrackArgument, DepotArgument, SpeedArgument];
    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var hasTrack = commandContext.Arguments.ContainsKey(TrackArgument.Key);
        var hasDepot = commandContext.Arguments.ContainsKey(DepotArgument.Key);

        if (!hasTrack && !hasDepot)
        {
            return new ResultProblem("Either 'track' or 'depot' must be provided");
        }

        Id<Track> trackId;
        if (hasDepot)
        {
            if (commandContext.GetId<Building>(DepotArgument).TryPickProblems(out var depotProblems, out var buildingId))
            {
                return depotProblems;
            }

            if (!depotService.TryGetDepot(buildingId, out var depot))
            {
                return new ResultProblem("Building '{0}' is not a depot", buildingId);
            }

            trackId = depot.TrackId;
        }
        else
        {
            if (commandContext.GetId<Track>(TrackArgument).TryPickProblems(out var trackProblems, out trackId))
            {
                return trackProblems;
            }

            if (!trackService.TrackExists(trackId))
            {
                return new ResultProblem("Track with id '{0}' doesnt exist", trackId);
            }
        }

        var trainIdResult = commandContext.Arguments.ContainsKey(TrainIdArgument.Key)
            ? commandContext.GetId<Train>(TrainIdArgument)
            : CreateTrain();

        if (trainIdResult.TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        var speedString = commandContext.Arguments.GetValueOrDefault(SpeedArgument.Key, "0");
        if (!float.TryParse(speedString, NumberFormatInfo.InvariantInfo, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }

        TrackPoint trackPoint = new(trackId, 0);
        TrainTrackPosition trainTrackPosition = new(trackPoint, TrainDirection.Forward);
        TrainMotion trainMotion = TrainMotion.Stopped with { Speed = speed, TargetSpeed = speed, UserTargetSpeed = speed };

        trainPositionService.SetTrackPosition(trainId, trainTrackPosition);
        trainPositionService.SetMotion(trainId, trainMotion);

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
