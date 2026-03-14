using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class IndustryPlacingToolService(
    MouseRaycastService mouseRaycastService,
    ToolManagementService toolManagementService,
    BuildingRenderingService buildingRenderingService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingValidationService buildingValidationService,
    BuildingService buildingService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager,
    TerrainHighlightSettings terrainHighlightSettings) : BaseToolService<IndustryPlacingToolService.State>(toolManagementService, new State())
{
    private static readonly Id<BuildingBlueprint>[] IndustryBlueprints =
    [
        BuildingBlueprintCatalog.Forest,
        BuildingBlueprintCatalog.Mine,
        BuildingBlueprintCatalog.Sawmill,
    ];

    public record State(bool ActivatedThisFrame = false, CardinalDirection CardinalDirection = CardinalDirection.North, int BlueprintIndex = 0);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Industries");

    private readonly Id<Building> _ghostBuildingId = Id.New<Building>();
    private bool _ghostRegistered;

    public override Result Unload()
    {
        UnregisterGhost();
        return Result.Success();
    }

    protected override State OnToolSelected(State toolState)
    {
        terrainHighlightSettings.ShowGrid = true;
        return toolState;
    }

    protected override State OnToolDeselected(State toolState)
    {
        terrainHighlightSettings.ShowGrid = false;
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

        if (keyboardManager.State.IsKeyPressed(Key.Tab))
        {
            var nextIndex = (ToolState.BlueprintIndex + 1) % IndustryBlueprints.Length;
            ToolState = ToolState with { BlueprintIndex = nextIndex };
            UnregisterGhost();
        }

        return Pass.Pass;
    }

    private Id<BuildingBlueprint> CurrentBlueprintId => IndustryBlueprints[ToolState.BlueprintIndex];

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (mouseRaycastService.TerrainIntersectionTile is not { } tilePosition)
        {
            UnregisterGhost();
            return Result.Success();
        }

        BuildingPosition position = new(tilePosition, ToolState.CardinalDirection);

        if (!buildingBlueprintService.TryGetBlueprint(CurrentBlueprintId, out var blueprint))
        {
            return new ResultProblem("Industry blueprint not found");
        }

        var isValid = buildingValidationService.IsValid(position, blueprint.Footprint);

        UpdateGhost(position, blueprint.Footprint, isValid);

        if (!ToolState.ActivatedThisFrame || !isValid)
        {
            return Result.Success();
        }

        if (buildingService.AddBuilding(CurrentBlueprintId, position).TryPickProblems(out var addProblems))
        {
            return addProblems;
        }

        return Result.Success();
    }

    private void UpdateGhost(BuildingPosition position, TileFootprint footprint, bool isValid)
    {
        if (isValid)
        {
            buildingRenderingService.SetGhostAppearance(0.5f);
        }
        else
        {
            buildingRenderingService.SetGhostAppearance(0.7f, new Vector3D<float>(1, 0, 0), 1.0f);
        }

        if (_ghostRegistered)
        {
            buildingRenderingService.UpdateGhost(_ghostBuildingId, position, footprint);
        }
        else
        {
            buildingRenderingService.RegisterGhost(_ghostBuildingId, position, footprint);
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
