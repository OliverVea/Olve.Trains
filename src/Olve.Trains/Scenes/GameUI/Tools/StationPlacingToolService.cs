using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Industries;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class StationPlacingToolService(
    ILogger<StationPlacingToolService> logger,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    BuildingBlueprintLibraryService libraryService,
    BuildingService buildingService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager) : BaseToolService<StationPlacingToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false, CardinalDirection CardinalDirection = CardinalDirection.North);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Stations");


    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };

        if (keyboardManager.State.IsKeyPressed(Key.R))
        {
            ToolState = ToolState with { CardinalDirection = ToolState.CardinalDirection.RotateClockwise() };
        }

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        if (terrainRaycastService.TerrainIntersectionTile is not { } tilePosition)
        {
            logger.LogDebug("Station placement click missed terrain");
            return Result.Success();
        }

        var stationBlueprint = libraryService.StationBlueprint;
        BuildingPosition position = new(tilePosition, ToolState.CardinalDirection);
        buildingService.AddBuilding(stationBlueprint, position);

        return Result.Success();
    }
}
