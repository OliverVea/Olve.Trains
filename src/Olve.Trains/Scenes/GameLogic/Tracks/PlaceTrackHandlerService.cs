using System.Globalization;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

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
        if (TryParseVector3(commandContext.GetArgument(StartArgument)!, out var start)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        if (TryParseVector3(commandContext.GetArgument(EndArgument)!, out var end)
            .TryPickProblems(out problems))
        {
            return problems;
        }

        var defaultDirection = Vector3D.Normalize(end - start);

        var startDirArg = commandContext.GetArgument(StartDirArgument);
        var startDirection = defaultDirection;
        if (startDirArg is not null)
        {
            if (TryParseDirection(startDirArg, out var parsedStartDir).TryPickProblems(out problems))
            {
                return problems;
            }
            startDirection = parsedStartDir.ToVector3D();
        }

        var endDirArg = commandContext.GetArgument(EndDirArgument);
        var endDirection = defaultDirection;
        if (endDirArg is not null)
        {
            if (TryParseDirection(endDirArg, out var parsedEndDir).TryPickProblems(out problems))
            {
                return problems;
            }
            endDirection = parsedEndDir.ToVector3D();
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

    private static Result TryParseVector3(string input, out Vector3D<float> result)
    {
        result = default;
        var parts = input.Split(',');
        if (parts.Length != 3)
        {
            return new ResultProblem("Expected 3 comma-separated values (x,y,z), got '{0}'", input);
        }

        if (!float.TryParse(parts[0].Trim(), NumberFormatInfo.InvariantInfo, out var x)
            || !float.TryParse(parts[1].Trim(), NumberFormatInfo.InvariantInfo, out var y)
            || !float.TryParse(parts[2].Trim(), NumberFormatInfo.InvariantInfo, out var z))
        {
            return new ResultProblem("Could not parse coordinates from '{0}'", input);
        }

        result = new Vector3D<float>(x, y, z);
        return Result.Success();
    }

    private static Result TryParseDirection(string input, out CardinalDirection result)
    {
        result = input.ToLowerInvariant() switch
        {
            "north" or "n" => CardinalDirection.North,
            "south" or "s" => CardinalDirection.South,
            "east" or "e" => CardinalDirection.East,
            "west" or "w" => CardinalDirection.West,
            _ => CardinalDirection.None
        };

        if (result == CardinalDirection.None)
        {
            return new ResultProblem("Invalid direction '{0}'. Use: north, south, east, west (or n, s, e, w)", input);
        }

        return Result.Success();
    }
}