using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.GameLogic.Trains;

[GenerateOneOf]
public partial class TrainPosition : OneOfBase<None, TrainTrackPosition>
{
    public static TrainPosition None { get; } = new None();
}