using Microsoft.Extensions.Logging;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class StationPlacingToolService(
    ILogger<StationPlacingToolService> logger,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    MouseManager mouseManager) : BaseToolService<StationPlacingToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Stations");


    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = new State(ActivatedThisFrame: activatedThisFrame);

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (!ToolState.ActivatedThisFrame
            || terrainRaycastService.TerrainIntersectionTileCenter is not { } tileCenter)
        {
            return Result.Success();
        }

        logger.LogInformation("Placing stations at {Center}", tileCenter);
        return Result.Success();
    }
}
