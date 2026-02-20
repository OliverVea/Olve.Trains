using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class DeleteTrackHandlerService(
    ILogger<DeleteTrackHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new("track", "The ID of the track to delete", true);

    public override string Verb => "delete-track";
    public override string HelpString => "Deletes a track by ID. Example: delete-track track=<track-id>";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrackArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Track>(TrackArgument).TryPickProblems(out var problems, out var trackId))
        {
            return problems;
        }

        var result = trackService.DeleteTrack(trackId);
        if (result.MapToResult(allowNotFound: false).TryPickProblems(out problems))
        {
            return problems;
        }

        logger.LogInformation("Deleted track {TrackId}", trackId);
        return new CommandOutput($"Deleted track {trackId}");
    }
}
