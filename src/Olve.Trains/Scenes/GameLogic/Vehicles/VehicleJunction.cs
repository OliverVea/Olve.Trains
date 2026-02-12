using Olve.Trains.Scenes.Game.Junctions;
using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.Game.Vehicles;

[GenerateOneOf]
public partial class VehicleJunction : OneOfBase<None, Id<Junction>>
{
    public static readonly VehicleJunction None = new None();
}