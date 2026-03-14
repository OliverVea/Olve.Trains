using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public readonly record struct EnvironmentalObjectBlueprint(
    Id<EnvironmentalObjectBlueprint> Id,
    string Description,
    AssetPath<MeshData>? MeshPath = null) : IHasId<Id<EnvironmentalObjectBlueprint>>;
