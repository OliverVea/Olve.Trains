using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameRendering;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class BuildingPlacementToolService(
    BuildingGhostPreviewService ghostPreviewService,
    BuildingValidationService buildingValidationService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    TerrainHighlightSettings terrainHighlightSettings)
{
    public record PreviewState(
        bool Show = false,
        BuildingPosition Position = default,
        bool Valid = true);

    private readonly Dictionary<Id<BuildingPreview>, Id<BuildingBlueprint>> _previews = new();
    private readonly Dictionary<Id<BuildingPreview>, PreviewState> _states = new();

    public Id<BuildingPreview> Register(Id<BuildingBlueprint> blueprintId)
    {
        var previewId = Id.New<BuildingPreview>();
        _previews[previewId] = blueprintId;
        _states[previewId] = new PreviewState();
        return previewId;
    }

    public void Update(Id<BuildingPreview> previewId, Func<PreviewState, PreviewState> update)
    {
        if (!_previews.TryGetValue(previewId, out var blueprintId)) return;

        var oldState = _states.GetValueOrDefault(previewId, new PreviewState());
        var newState = update(oldState);

        // Validate placement when position changes and preview is shown
        if (newState.Show && newState.Position != oldState.Position)
        {
            if (buildingBlueprintService.TryGetBlueprint(blueprintId, out var blueprint))
            {
                newState = newState with
                {
                    Valid = buildingValidationService.IsValid(newState.Position, blueprint.Footprint),
                };
            }
        }

        _states[previewId] = newState;

        // Toggle terrain grid
        if (oldState.Show != newState.Show)
        {
            terrainHighlightSettings.ShowGrid = newState.Show;
        }

        // Delegate to ghost preview service
        ghostPreviewService.Update(blueprintId, _ => new BuildingGhostPreviewService.GhostPreviewState(
            Show: newState.Show,
            Valid: newState.Valid,
            Position: newState.Position));
    }

    public Result TryPlace(Id<BuildingPreview> previewId)
    {
        if (!_previews.TryGetValue(previewId, out var blueprintId))
        {
            return new ResultProblem("Preview not found: '{0}'", previewId);
        }

        var state = _states.GetValueOrDefault(previewId, new PreviewState());

        if (!state.Show || !state.Valid)
        {
            return Result.Success();
        }

        if (buildingService.AddBuilding(blueprintId, state.Position).TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}
