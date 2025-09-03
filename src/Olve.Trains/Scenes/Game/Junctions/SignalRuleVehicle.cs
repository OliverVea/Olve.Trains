using Olve.Trains.Scenes.Game.Vehicles;
using Olve.Utilities.Types;
using OneOf;

namespace Olve.Trains.Scenes.Game.Junctions;

[GenerateOneOf]
public partial class SignalRuleVehicle : OneOfBase<Any, Id<VehicleGroup>, Id<Vehicle>>
{
    public override string ToString()
    {
        return Match(
            x => x.ToString(),
            x => x.ToString(),
            x => x.ToString());
    }
}