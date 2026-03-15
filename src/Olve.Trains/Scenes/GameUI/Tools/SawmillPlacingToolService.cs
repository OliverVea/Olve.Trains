using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class SawmillPlacingToolService(
    ToolManagementService toolManagementService,
    BuildingPlacementToolService buildingPlacementToolService,
    MouseRaycastService mouseRaycastService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager)
    : BaseToolService<SawmillPlacingToolService.PlacingState>(toolManagementService, new PlacingState())
{
    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Sawmills");

    public record PlacingState(CardinalDirection CardinalDirection = CardinalDirection.North);

    private Id<BuildingPreview> _previewId;

    public override Result Load()
    {
        _previewId = buildingPlacementToolService.Register(BuildingBlueprintCatalog.Sawmill);
        return base.Load();
    }

    protected override PlacingState OnToolSelected(PlacingState toolState)
    {
        buildingPlacementToolService.Update(_previewId, s => s with { Show = true });
        return toolState;
    }

    protected override PlacingState OnToolDeselected(PlacingState toolState)
    {
        buildingPlacementToolService.Update(_previewId, s => s with { Show = false });
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput()
    {
        if (mouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            buildingPlacementToolService.TryPlace(_previewId);
        }

        if (keyboardManager.State.IsKeyPressed(Key.R))
        {
            base.ToolState = base.ToolState with
            {
                CardinalDirection = base.ToolState.CardinalDirection.RotateCounterClockwise(),
            };
        }

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate()
    {
        if (mouseRaycastService.TerrainIntersectionTile is { } tilePosition)
        {
            buildingPlacementToolService.Update(_previewId, s => s with
            {
                Show = true,
                Position = new BuildingPosition(tilePosition, base.ToolState.CardinalDirection),
            });
        }
        else
        {
            buildingPlacementToolService.Update(_previewId, s => s with { Show = false });
        }

        return Result.Success();
    }
}
