using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Rendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.UI.Tracks;

public class TrackPlacingService(ILoggingManager loggingManager,
    TerrainRaycastService terrainRaycastService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager,
    TrackService trackService) : SceneService(loggingManager)
{
    private static readonly Vector3D<float> Offset = new(0, 0f, 0);
    
    public TrackPoint? PreviousPoint { get; private set; }
    public TrackPoint? CurrentPoint { get; private set; }
    
    public CardinalDirection CardinalDirection { get; private set; } = CardinalDirection.North;

    public override int Priority => GetPriorityFromDependencies([terrainRaycastService]);

    protected override Result<Pass> OnInput(TimeSpan deltaTime)
    {
        if (mouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            if (PlaceTrack().TryPickProblems(out var problems))
            {
                return problems;
            }
        }
        
        if (keyboardManager.State.IsKeyPressed(Key.R)) 
        {
            if (keyboardManager.State.Shift)
            {
                CardinalDirection = CardinalDirection switch
                {
                    CardinalDirection.North => CardinalDirection.East,
                    CardinalDirection.East => CardinalDirection.South,
                    CardinalDirection.South => CardinalDirection.West,
                    CardinalDirection.West => CardinalDirection.North,
                    _ => throw new NotSupportedException($"CW Rotation of CardinalDirection '{CardinalDirection}' is not supported.")
                };
            }
            else
            {
                CardinalDirection = CardinalDirection switch
                {
                    CardinalDirection.North => CardinalDirection.West,
                    CardinalDirection.West => CardinalDirection.South,
                    CardinalDirection.South => CardinalDirection.East,
                    CardinalDirection.East => CardinalDirection.North,
                    _ => throw new NotSupportedException($"CCW Rotation of CardinalDirection '{CardinalDirection}' is not supported.")
                };
            }
        }
        
        return Pass.Pass;
    }

    private Result PlaceTrack()
    {
        if (PreviousPoint is not {} startPoint)
        {
            PreviousPoint = CurrentPoint;
        }
        else
        {
            if (CurrentPoint is not {} endPoint)
            {
                return  new ResultProblem("Current point is null");
            }
                
            var delta = endPoint.Point - startPoint.Point;
            if (delta.Length < MathConstants.Epsilon)
            {
                return Result.Success();
            }

            List<(TrackPoint Start, TrackPoint End)> tracks = [];
                
            if (TrackIsStraightLine(startPoint, endPoint))
            {
                var deltaNormalized = Vector3D.Normalize(delta);
                var subtracks = float.Round(delta.Length);
                for (var i = 0; i < subtracks; i++)
                {
                    var subtrackStart = startPoint.Point + deltaNormalized * i;
                    var subtrackEnd = startPoint.Point + deltaNormalized * (i + 1);
                        
                    tracks.Add((
                        new TrackPoint(subtrackStart, deltaNormalized), 
                        new TrackPoint(subtrackEnd, deltaNormalized)));
                }
            }
            else
            {
                tracks.Add((startPoint, endPoint));
            }

            var trackResults = tracks.Select(p => PlaceTrack(p.Start, p.End));
            if (trackResults.TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    private Result PlaceTrack(TrackPoint startPoint, TrackPoint endPoint)
    {
        var pointDistance = (startPoint.Point - endPoint.Point).Length;
        if (pointDistance < 0.01f)
        {
            LoggingManager.Log(LogLevel.Warning, "Tried to place track with distance 0");
            return Result.Success();
        }
        
        startPoint = startPoint with { Tangent = -startPoint.Tangent };
                
        if (trackService.AddTrack(startPoint, endPoint).TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add track");
        }
                
        LoggingManager.Log(LogLevel.Info, $"Created track with id '{trackId}'");
        LoggingManager.Log(LogLevel.Debug, $"Placed track from {startPoint} to {endPoint}");
                
        PreviousPoint = null;
        
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        CurrentPoint = terrainRaycastService.TerrainIntersectionTileCenter is { } tileCenter
            ? new TrackPoint(tileCenter + Offset, CardinalDirection.ToVector3D())
            : null;
        
        return Result.Success();
    }

    private bool TrackIsStraightLine(TrackPoint startPoint, TrackPoint endPoint)
    {
        var delta = endPoint.Point - startPoint.Point;
        
        return delta.CountZeroDimensions() == 2
               && (startPoint.Tangent - endPoint.Tangent).Length < MathConstants.Epsilon
               && (Vector3D.Normalize(startPoint.Tangent) - Vector3D.Normalize(delta)).Length < MathConstants.Epsilon;
    }
}