using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public class EnvironmentalObjectBlueprintLibraryService(
    EnvironmentalObjectBlueprintService blueprintService) : ISceneService
{
    public Result Load()
    {
        return Result.Concat(
            LoadTree(),
            LoadRock(),
            LoadGrass());
    }

    private Result LoadTree()
    {
        if (blueprintService.AddBlueprint(EnvironmentalObjectBlueprintCatalog.Tree, "Tree", Meshes.SM_Env_Tree_01)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result LoadRock()
    {
        if (blueprintService.AddBlueprint(EnvironmentalObjectBlueprintCatalog.Rock, "Rock")
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result LoadGrass()
    {
        if (blueprintService.AddBlueprint(EnvironmentalObjectBlueprintCatalog.Grass, "Grass")
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result Unload()
    {
        blueprintService.DeleteBlueprint(EnvironmentalObjectBlueprintCatalog.Tree);
        blueprintService.DeleteBlueprint(EnvironmentalObjectBlueprintCatalog.Rock);
        blueprintService.DeleteBlueprint(EnvironmentalObjectBlueprintCatalog.Grass);

        return Result.Success();
    }
}
