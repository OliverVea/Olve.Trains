using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Stations;

public readonly record struct Station(Id<Station> Id, string Name, Vector3D<float> Center) : IHasId<Id<Station>>;