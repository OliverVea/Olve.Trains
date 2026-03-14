using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public readonly record struct EnvironmentalObject(
    Id<EnvironmentalObject> Id,
    Id<EnvironmentalObjectBlueprint> BlueprintId,
    Position3D Position,
    AssetPath<MeshData>? MeshPath = null,
    AssetPath<TextureData<RGBA>>? TexturePath = null) : IHasId<Id<EnvironmentalObject>>;
