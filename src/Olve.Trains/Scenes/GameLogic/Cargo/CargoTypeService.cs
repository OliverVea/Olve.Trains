using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoTypeService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<CargoType> _cargoTypes = entityStoreFactory.Create<CargoType>();

    public Id<CargoType> AddCargoType(Id<CargoType> id, string name)
    {
        _cargoTypes.TryAdd(new CargoType(id, name));
        return id;
    }

    public bool TryGetCargoType(Id<CargoType> id, out CargoType cargoType) => _cargoTypes.TryGet(id, out cargoType);

    public IEnumerable<CargoType> CargoTypes => _cargoTypes.Values;
}
