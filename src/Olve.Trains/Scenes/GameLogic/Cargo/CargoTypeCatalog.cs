namespace Olve.Trains.Scenes.GameLogic.Cargo;

public static class CargoTypeCatalog
{
    public static Id<CargoType> Wood { get; } = Id.FromName<CargoType>("cargo/wood");
    public static Id<CargoType> Coal { get; } = Id.FromName<CargoType>("cargo/coal");
    public static Id<CargoType> Planks { get; } = Id.FromName<CargoType>("cargo/planks");
}
