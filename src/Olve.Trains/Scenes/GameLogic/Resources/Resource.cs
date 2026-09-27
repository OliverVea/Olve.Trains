using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Utilities.Lookup;
using Silk.NET.Maths;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Resources;

public readonly record struct Resource(
    Id<Resource> Id,
    Id<ResourceType> ResourceTypeId,
    Id<EnvironmentalObject> EnvironmentalObjectId,
    Vector3D<float> Position,
    long CreatedSequence) : IHasId<Id<Resource>>, IHasCreatedSequence;
