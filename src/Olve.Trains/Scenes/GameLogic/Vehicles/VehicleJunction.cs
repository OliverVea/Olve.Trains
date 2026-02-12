using Olve.Trains.Scenes.GameLogic.Junctions;
using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

[GenerateOneOf]
public partial class VehicleJunction : OneOfBase<None, Id<Junction>>
{
    public static readonly VehicleJunction None = new None();
}