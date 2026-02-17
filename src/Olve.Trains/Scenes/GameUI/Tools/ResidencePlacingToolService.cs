using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Industries;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class ResidencePlacingToolService(
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    BuildingBlueprintLibraryService libraryService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingService buildingService,
    BuildingValidationService buildingValidationService,
    BuildingRenderingService buildingRenderingService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager) : BaseToolService<ResidencePlacingToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false, CardinalDirection CardinalDirection = CardinalDirection.North);

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Residences");

    private static readonly Shaders.Building.EntityParameters ValidGhostParameters = new(UOpacity: 0.5f, UColorMix: 0.0f);

    private static readonly Shaders.Building.EntityParameters InvalidGhostParameters = new(
        UOpacity: 0.7f,
        UColorOverride: new Vector3D<float>(1, 0, 0),
        UColorMix: 1.0f);

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
            UnregisterGhost();
            return Result.Success();
        }

        var residentialBlueprint = libraryService.ResidentialBlueprint;
        BuildingPosition position = new(tilePosition, ToolState.CardinalDirection);

        if (!buildingBlueprintService.TryGetBlueprint(residentialBlueprint, out var blueprint))
        {
            return new ResultProblem("Residential blueprint not found");
        }

        var isValid = buildingValidationService.IsValid(position, blueprint.Footprint);

        UpdateGhost(position, blueprint.Footprint, isValid);

        if (!ToolState.ActivatedThisFrame || !isValid)
        {
            return Result.Success();
        }

        buildingService.AddBuilding(residentialBlueprint, position);

        return Result.Success();
    }

    private void UpdateGhost(BuildingPosition position, TileFootprint footprint, bool isValid)
    {
        var ghostParams = isValid ? ValidGhostParameters : InvalidGhostParameters;

        if (_ghostRegistered)
        {
            buildingRenderingService.UpdateDirect(_ghostBuildingId, position, footprint, ghostParams);
        }
        else
        {
            var color = BuildingRenderingService.GetBuildingColor(BuildingType.Residential);
            ghostParams = ghostParams with { UColor = color.ToVector() };
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
