using Olve.Trains.Scenes.Game.Junctions;
using Olve.Trains.Scenes.Game.Tracks;
using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.Game.Vehicles;

[GenerateOneOf]
public partial class VehicleJunction : OneOfBase<None, Id<Junction>>
{
    public static readonly VehicleJunction None = new None();
}

[GenerateOneOf]
public partial class VehicleTrackPoint : OneOfBase<None, TrackPoint>
{
    public static readonly VehicleTrackPoint None = new None();
}