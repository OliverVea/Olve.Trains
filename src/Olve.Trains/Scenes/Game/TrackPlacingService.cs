using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.Game;

public class TrackPlacingService(TerrainRaycastService terrainRaycastService, MouseManager mouseManager, KeyboardManager keyboardManager, ILoggingManager loggingManager, TrackService trackService) : SceneService
{
    public TrackPoint? PreviousPoint { get; private set; }
    public TrackPoint? CurrentPoint { get; private set; }
    
    public Direction Direction { get; private set; } = Direction.North;

    public override int Priority => GetPriorityFromDependencies([terrainRaycastService]);

    public override Result<Pass> Input(TimeSpan deltaTime)
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
                
                if (trackService.AddTrack(PreviousPoint.Value, CurrentPoint.Value).TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to add track");
                }
                
                loggingManager.Log(LogLevel.Info, $"Placed track from {PreviousPoint.Value} to {CurrentPoint.Value}");
                
                PreviousPoint = null;
            }
        }
        
        if (keyboardManager.State.IsKeyPressed(Key.R)) 
        {
            Direction = Direction switch
            {
                Direction.North => Direction.East,
                Direction.East => Direction.South,
                Direction.South => Direction.West,
                Direction.West => Direction.North,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        return Pass.Pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        CurrentPoint = terrainRaycastService.TerrainIntersectionTileCenter is { } tileCenter
            ? new TrackPoint(tileCenter, Direction)
            : null;
        
        return Result.Success();
    }
}