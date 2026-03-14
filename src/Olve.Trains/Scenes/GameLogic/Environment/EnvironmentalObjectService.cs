using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Math;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Environment;

/*
public class EnvironmentalObjectService(EnvironmentalObjectBlueprintService blueprintService)
{

}


public static class EnvironmentalObjects
{
    public static Id<EnvironmentalObjectBlueprint> Tree { get; } = Id.FromName<EnvironmentalObjectBlueprint>("Tree");
}

public class EnvironmentalObjectBlueprintService
{

}

public class EnvironmentalObjectBlueprintLibraryService
{

}

public readonly record struct EnvironmentalObjectBlueprint(Id<EnvironmentalObjectBlueprint> Id, Id<Mesh> MeshId) : IHasId<Id<EnvironmentalObjectBlueprint>>;
public readonly record struct EnvironmentalObject(Id<EnvironmentalObject> Id, Id<EnvironmentalObjectBlueprint> BlueprintId, Position3D Position) : IHasId<Id<EnvironmentalObject>>;





/*

Environment

All envionmental objects have colliders and meshes.

- Trees!
    - Is obstacle
    - Used by forester
    - Generated with terrain map layer + seed (1.0)
    - Can be planted (1.0)
    - Also wind shader
- Rocks
    - Is obstacle
- Grass
    -
    - Wind shader






*/