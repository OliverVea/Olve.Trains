using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class PlaceTrackHandlerService(
    ILogger<PlaceTrackHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackPlacingService trackPlacingService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument StartArgument = new("start", "Start position as x,y,z", true);
    private static readonly CommandArgument EndArgument = new("end", "End position as x,y,z", true);
    private static readonly CommandArgument StartDirArgument = new("start-dir", "Start direction: north, south, east, west (optional)", false);
    private static readonly CommandArgument EndDirArgument = new("end-dir", "End direction: north, south, east, west (optional)", false);

    public override string Verb => "place-track";
    public override string HelpString => """
        Places a track between two positions with optional direction control.
        Example: place-track start=0,0.125,0 end=4,0.125,0 start-dir=east end-dir=east
        Example (curved): place-track start=0,0.125,0 end=4,0.125,4 start-dir=east end-dir=north
        """;
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [StartArgument, EndArgument, StartDirArgument, EndDirArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(StartArgument).Bind(s => s.ParseVector3())
            .TryPickProblems(out var problems, out var start))
        {
            return problems;
        }

        if (commandContext.GetRequiredArgument(EndArgument).Bind(s => s.ParseVector3())
            .TryPickProblems(out problems, out var end))
        {
            return problems;
        }

        var defaultDirection = Vector3D.Normalize(end - start);

        var startDirection = defaultDirection;
        if (commandContext.GetOptionalArgument(StartDirArgument) is {} startDirArg)
        {
            if (startDirArg.ParseDirection().TryPickProblems(out problems, out var parsedDir))
                return problems;
            startDirection = parsedDir.ToVector3D();
        }

        var endDirection = defaultDirection;
        if (commandContext.GetOptionalArgument(EndDirArgument) is {} endDirArg)
        {
            if (endDirArg.ParseDirection().TryPickProblems(out problems, out var parsedDir))
                return problems;
            endDirection = parsedDir.ToVector3D();
        }

        var startEndpoint = new TrackEndpoint(start, startDirection);
        var endEndpoint = new TrackEndpoint(end, endDirection);

        if (trackPlacingService.PlaceTrack(startEndpoint, endEndpoint)
            .TryPickProblems(out problems, out var trackIds))
        {
            return problems;
        }

        logger.LogInformation("Placed {Count} track segments from {Start} to {End}", trackIds.Count, start, end);
        var idList = string.Join("\n", trackIds);
        return new CommandOutput($"Placed {trackIds.Count} track segments:\n{idList}");
    }
}
