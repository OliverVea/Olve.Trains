using Olve.Trains.Scenes.GameLogic.Vehicles;
using Olve.Utilities.Types;
using OneOf;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

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