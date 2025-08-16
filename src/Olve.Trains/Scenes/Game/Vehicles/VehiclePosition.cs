using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.Game.Vehicles;

[GenerateOneOf]
public partial class VehiclePosition : OneOfBase<None, TrackPosition>
{
    public static VehiclePosition None { get; } = new None();
}