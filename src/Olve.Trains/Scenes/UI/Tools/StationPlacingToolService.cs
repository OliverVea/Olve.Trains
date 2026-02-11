using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Microsoft.Extensions.Logging;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI.Indicators;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.UI.Tools;

public sealed class StationPlacingToolService(
    ILogger<StationPlacingToolService> logger,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    TrackArrowIndicatorService arrowIndicatorService,
    TrackService trackService,
    StationService stationService,
    StationPlatformService stationPlatformService,
    StationNameGenerator stationNameGenerator,
    MouseManager mouseManager) : BaseToolService<StationPlacingToolService.State>(toolManagementService, new State())
{
    public record State(Vector3D<float>? From = null, bool ActivatedThisFrame = false);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Stations");

    private Id<ArrowIndicator> _arrowIndicatorId;

    public new Result Load()
    {
        if (arrowIndicatorService.AddArrowIndicator().TryPickProblems(out var problems, out _arrowIndicatorId))
        {
            return problems;
        }

        return base.Load();
    }

    public new Result Unload()
    {
        if (arrowIndicatorService.RemoveArrowIndicator(_arrowIndicatorId).TryPickProblems(out var problems))
        {
            return problems;
        }

        return base.Unload();
    }

    protected override State OnToolSelected(State toolState)
    {
        arrowIndicatorService.Show(_arrowIndicatorId);
        return new State();
    }

    protected override State OnToolDeselected(State toolState)
    {
        arrowIndicatorService.Hide(_arrowIndicatorId);
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (terrainRaycastService.TerrainIntersectionTileCenter is not { } tileCenter)
        {
            return Result.Success();
        }

        if (ToolState.From is { } from)
        {
            var delta = tileCenter - from;
            var direction = float.Abs(delta.X) > float.Abs(delta.Z)
                ? new Vector3D<float>(float.Sign(delta.X), 0, 0)
                : new Vector3D<float>(0, 0, float.Sign(delta.Z));

            arrowIndicatorService.SetPosition(_arrowIndicatorId, from, direction);
        }
        else
        {
            arrowIndicatorService.SetPosition(_arrowIndicatorId, tileCenter, Vector3D<float>.UnitZ);
        }

        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        if (ToolState.From is not { } fromPoint)
        {
            logger.LogDebug("Set start of station placement to '{TileCenter}'", tileCenter);
            ToolState = ToolState with { From = tileCenter };
            return Result.Success();
        }

        ToolState = ToolState with { From = null };

        if ((tileCenter - fromPoint).LengthSquared < MathConstants.Epsilon)
        {
            logger.LogDebug("Rejected station placement: same tile");
            return Result.Success();
        }

        return PlaceStation(fromPoint, tileCenter);
    }

    private Result PlaceStation(Vector3D<float> from, Vector3D<float> to)
    {
        var delta = to - from;
        if (float.Abs(delta.X) > MathConstants.Epsilon && float.Abs(delta.Z) > MathConstants.Epsilon)
        {
            return new ResultProblem("Stations cannot be placed diagonally");
        }

        var center = (from + to) * 0.5f;
        var name = stationNameGenerator.CreateStationName();

        if (stationService.CreateStation(name, center).TryPickProblems(out var problems, out var stationId))
        {
            return problems;
        }

        logger.LogDebug("Created station '{Name}' at {Center}", name, center);

        var direction = Vector3D.Normalize(delta);
        var tileCount = (int)float.Round(delta.Length);

        for (var i = 0; i < tileCount; i++)
        {
            var start = from + direction * i;
            var end = from + direction * (i + 1);

            var startEndpoint = new TrackEndpoint(start, -direction);
            var endEndpoint = new TrackEndpoint(end, direction);

            if (trackService.AddTrack(startEndpoint, endEndpoint).TryPickProblems(out problems, out var trackId))
            {
                return problems;
            }

            if (stationPlatformService.AddPlatform(trackId, stationId).TryPickProblems(out problems, out var platformId))
            {
                return problems;
            }

            logger.LogDebug("Added platform track '{TrackId}' (platform '{PlatformId}') to station '{Name}'", trackId, platformId, name);
        }

        return Result.Success();
    }
}
