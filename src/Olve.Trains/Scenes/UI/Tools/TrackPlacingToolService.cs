using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
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
    TrackPlacingService trackPlacingService,
    TrackRenderingService trackRenderingService,
    TrackLineStripDataService trackLineStripDataService) : BaseToolService<TrackPlacingToolService.State>(loggingManager, toolManagementService, new State())
{
    public record State(TrackEndpoint? From = null, CardinalDirection Direction = CardinalDirection.North, bool ActivatedThisFrame = false);

    private static readonly Shaders.LineStrip.EntityParameters GhostShaderParameters = new(UOpacity: 0.5f);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Tracks");

    private Id<ArrowIndicator> _arrowIndicatorId;
    private readonly Id<Track> _ghostTrackId = Id.New<Track>();
    private bool _ghostRegistered;

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
        UnregisterGhost();

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
        UnregisterGhost();
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
            return UpdateGhost(terrainIntersectionTileCenter);
        }

        TrackEndpoint trackEndpoint = new(terrainIntersectionTileCenter, ToolState.Direction.ToVector3D());

        if (ToolState.From is not { } from)
        {
            LoggingManager.Log(LogLevel.Debug, $"Set start of track placement to '{trackEndpoint}'");
            ToolState = ToolState with { From = trackEndpoint };
            return Result.Success();
        }

        UnregisterGhost();
        ToolState = ToolState with { From = null };
        return trackPlacingService.PlaceTrack(from, trackEndpoint);
    }

    private Result UpdateGhost(Vector3D<float> mousePosition)
    {
        if (ToolState.From is not { } f)
        {
            return Result.Success();
        }

        TrackEndpoint currentEndpoint = new(mousePosition, ToolState.Direction.ToVector3D());
        f = f with { Tangent = -f.Tangent };
        if (trackLineStripDataService
            .GetLineStripData(f, currentEndpoint)
            .TryPickProblems(out var problems, out var data))
        {
            return problems;
        }

        if (_ghostRegistered)
        {
            trackRenderingService.Update(_ghostTrackId, data, GhostShaderParameters);
        }
        else
        {
            trackRenderingService.Register(_ghostTrackId, data, GhostShaderParameters);
            _ghostRegistered = true;
        }

        return Result.Success();
    }

    private void UnregisterGhost()
    {
        if (!_ghostRegistered)
        {
            return;
        }

        trackRenderingService.Unregister(_ghostTrackId);
        _ghostRegistered = false;
    }
}
