using System.Collections.Immutable;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Resources;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Industries;

public readonly record struct IndustryProperties(
    Id<IndustryRecipe> RecipeId,
    int Capacity,
    ImmutableDictionary<Id<CargoType>, int> AllowedTypes,
    float? HarvestRange = null,
    Id<ResourceType>? RequiredResourceType = null);