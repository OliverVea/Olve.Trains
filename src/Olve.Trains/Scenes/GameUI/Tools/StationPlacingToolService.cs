using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Industries;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class StationPlacingToolService(
    ILogger<StationPlacingToolService> logger,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    BuildingBlueprintLibraryService libraryService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingService buildingService,
    BuildingRenderingService buildingRenderingService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager) : BaseToolService<StationPlacingToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false, CardinalDirection CardinalDirection = CardinalDirection.North);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Stations");

    private static readonly Shaders.Building.EntityParameters GhostParameters = new(UOpacity: 0.5f);

    private readonly Id<Building> _ghostBuildingId = Id.New<Building>();
    private bool _ghostRegistered;

    public override Result Unload()
    {
        UnregisterGhost();
        return Result.Success();
    }

    protected override State OnToolDeselected(State toolState)
    {
        UnregisterGhost();
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };

        if (keyboardManager.State.IsKeyPressed(Key.R))
        {
            ToolState = ToolState with { CardinalDirection = ToolState.CardinalDirection.RotateCounterClockwise() };
        }

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (terrainRaycastService.TerrainIntersectionTile is not { } tilePosition)
        {
            logger.LogDebug("Station placement click missed terrain");
            return Result.Success();
        }

        var stationBlueprint = libraryService.StationBlueprint;
        BuildingPosition position = new(tilePosition, ToolState.CardinalDirection);

        if (!buildingBlueprintService.TryGetBlueprint(stationBlueprint, out var blueprint))
        {
            return new ResultProblem("Station blueprint not found");
        }

        UpdateGhost(position, blueprint.Footprint);

        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        buildingService.AddBuilding(stationBlueprint, position);

        return Result.Success();
    }

    private void UpdateGhost(BuildingPosition position, TileFootprint footprint)
    {
        if (_ghostRegistered)
        {
            buildingRenderingService.UpdateDirect(_ghostBuildingId, position, footprint, GhostParameters);
        }
        else
        {
            var color = BuildingRenderingService.GetBuildingColor(BuildingType.Station);
            var ghostParams = GhostParameters with { UColor = color.ToVector() };
            buildingRenderingService.RegisterDirect(_ghostBuildingId, position, footprint, ghostParams);
            _ghostRegistered = true;
        }
    }

    private void UnregisterGhost()
    {
        if (!_ghostRegistered)
        {
            return;
        }

        buildingRenderingService.Unregister(_ghostBuildingId);
        _ghostRegistered = false;
    }
}
