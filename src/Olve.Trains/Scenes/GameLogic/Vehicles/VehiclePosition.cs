using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

[GenerateOneOf]
public partial class VehiclePosition : OneOfBase<None, VehicleTrackPosition>
{
    public static VehiclePosition None { get; } = new None();
}