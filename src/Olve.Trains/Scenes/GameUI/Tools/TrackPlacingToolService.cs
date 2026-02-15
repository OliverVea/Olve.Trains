using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GameUI.Indicators;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class TrackPlacingToolService(ILogger<TrackPlacingToolService> logger,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    TrackArrowIndicatorService arrowIndicatorService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager,
    TrackPlacingService trackPlacingService,
    TrackRenderingService trackRenderingService,
    TrackLineStripDataService trackLineStripDataService,
    TrackValidationService trackValidationService) : BaseToolService<TrackPlacingToolService.State>(toolManagementService, new State())
{
    public record State(TrackEndpoint? From = null, CardinalDirection Direction = CardinalDirection.North, bool ActivatedThisFrame = false);

    private static readonly Shaders.LineStrip.EntityParameters ValidGhostParameters = new(UOpacity: 0.5f);
    private static readonly Shaders.LineStrip.EntityParameters InvalidGhostParameters = new(
        UOpacity: 0.7f,
        UColorOverride: new Vector3D<float>(1, 0, 0),
        UColorMix: 1.0f);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Tracks");

    private Id<ArrowIndicator> _arrowIndicatorId;
    private readonly Id<Track> _ghostTrackId = Id.New<Track>();
    private bool _ghostRegistered;

    public override Result Load()
    {
        if (arrowIndicatorService.AddArrowIndicator().TryPickProblems(out var problems, out _arrowIndicatorId))
        {
            return problems;
        }

        return base.Load();
    }

    public override Result Unload()
    {
        UnregisterGhost();

        if (arrowIndicatorService.RemoveArrowIndicator(_arrowIndicatorId).TryPickProblems(out var problems))
        {
            return problems;
        }

        return base.Unload();
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

        if (ToolState.From is not { } f)
        {
            logger.LogDebug("Set start of track placement to '{TrackEndpoint}'", trackEndpoint);
            ToolState = ToolState with { From = trackEndpoint };
            return Result.Success();
        }

        var fromNegated = f with { Tangent = -f.Tangent };
        if (!trackValidationService.IsValid(fromNegated, trackEndpoint))
        {
            return Result.Success();
        }

        UnregisterGhost();
        ToolState = ToolState with { From = null };
        return trackPlacingService.PlaceTrack(f, trackEndpoint).ToEmptyResult();
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

        var isValid = trackValidationService.IsValid(f, currentEndpoint);
        var ghostParams = isValid ? ValidGhostParameters : InvalidGhostParameters;

        if (_ghostRegistered)
        {
            trackRenderingService.Update(_ghostTrackId, data, ghostParams);
        }
        else
        {
            trackRenderingService.Register(_ghostTrackId, data, ghostParams);
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
