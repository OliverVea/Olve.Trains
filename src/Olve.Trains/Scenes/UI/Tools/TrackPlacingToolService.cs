using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI.Indicators;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.UI.Tools;

public sealed class TrackPlacingToolService(ILoggingManager loggingManager,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    TrackArrowIndicatorService arrowIndicatorService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager,
    TrackPlacingService trackPlacingService) : BaseToolService<TrackPlacingToolService.State>(loggingManager, toolManagementService, new State())
{
    public record State(TrackPoint? From = null, CardinalDirection Direction = CardinalDirection.North, bool ActivatedThisFrame = false);

    public static Id<Tool> ToolId { get; } = Id<Tool>.New();
    protected override Tool Tool => new(ToolId, "Place Tracks");
    
    private Id<ArrowIndicator> _arrowIndicatorId;

    protected override Result OnLoad()
    {
        if (arrowIndicatorService.AddArrowIndicator().TryPickProblems(out var problems, out _arrowIndicatorId))
        {
            return problems;
        }
        
        return base.OnLoad();
    }

    protected override Result OnUnload()
    {
        if (arrowIndicatorService.RemoveArrowIndicator(_arrowIndicatorId).TryPickProblems(out var problems))
        {
            return problems;
        }
        
        return base.OnUnload();
    }

    protected override State OnToolSelected(State toolState)
    {
        arrowIndicatorService.Show(_arrowIndicatorId);
        return toolState with { From = null, ActivatedThisFrame = false };
    }

    protected override State OnToolDeselected(State toolState)
    {
        arrowIndicatorService.Hide(_arrowIndicatorId);
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame =  mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };

        if (keyboardManager.State.IsKeyPressed(Key.R))
        {
            var newDirection = keyboardManager.State.Shift
                ? ToolState.Direction.RotateClockwise()
                : ToolState.Direction.RotateCounterClockwise();
            
            ToolState = ToolState with { Direction = newDirection };
        }

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {

        if (terrainRaycastService.TerrainIntersectionTileCenter is not { } terrainIntersectionTileCenter)
        {
            return Result.Success();
        }

        arrowIndicatorService.SetPosition(_arrowIndicatorId, terrainIntersectionTileCenter, ToolState.Direction.ToVector3D());

        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }
        
        TrackPoint trackPoint = new(terrainIntersectionTileCenter, ToolState.Direction.ToVector3D());

        if (ToolState.From is not { } from)
        {
            LoggingManager.Log(LogLevel.Debug, $"Set start of track placement to '{trackPoint}'");
            ToolState = ToolState with { From = trackPoint };
            return Result.Success();
        }
        
        ToolState = ToolState with { From = null };
        return trackPlacingService.PlaceTrack(from, trackPoint);
    }
}