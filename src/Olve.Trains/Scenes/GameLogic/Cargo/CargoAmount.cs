namespace Olve.Trains.Scenes.GameLogic.Cargo;

public readonly record struct CargoAmount(Id<CargoType> CargoTypeId, int Amount);
