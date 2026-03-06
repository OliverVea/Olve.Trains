using System.Collections.Immutable;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public readonly record struct IndustryRecipe(
    Id<IndustryRecipe> Id,
    string Name,
    ImmutableArray<CargoAmount> Inputs,
    ImmutableArray<CargoAmount> Outputs) : IHasId<Id<IndustryRecipe>>;
