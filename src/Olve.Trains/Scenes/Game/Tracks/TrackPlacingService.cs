using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Results;
using Olve.Trains.Scenes.Game.Terrain;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.Tracks;

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
            if (PreviousPoint is null)
            {
                PreviousPoint = CurrentPoint;
            }
            else
            {
                if (CurrentPoint is null)
                {
                    return new ResultProblem("Current point is null");
                }

                var start = PreviousPoint.Value with { Tangent = -PreviousPoint.Value.Tangent };
                var end = CurrentPoint.Value;
                
                if (trackService.AddTrack(start, end).TryPickProblems(out var problems, out var trackId))
                {
                    return problems.Prepend("Failed to add track");
                }
                
                LoggingManager.Log(LogLevel.Info, $"Created track with id '{trackId}'");
                LoggingManager.Log(LogLevel.Debug, $"Placed track from {start} to {end}");
                
                PreviousPoint = null;
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
                    _ => throw new ArgumentOutOfRangeException()
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
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        
        return Pass.Pass;
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        CurrentPoint = terrainRaycastService.TerrainIntersectionTileCenter is { } tileCenter
            ? new TrackPoint(tileCenter + Offset, CardinalDirection.ToVector3D())
            : null;
        
        return Result.Success();
    }
}