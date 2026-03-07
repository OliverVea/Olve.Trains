using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Industries;

public readonly record struct Industry(
    Id<Industry> Id,
    Id<Building> BuildingId,
    Id<IndustryRecipe> RecipeId,
    Id<CargoInventory> InventoryId) : IHasId<Id<Industry>>;
