using Olve.Trains.Scenes.GameLogic.Junctions;
using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.GameLogic.Trains;

[GenerateOneOf]
public partial class TrainJunction : OneOfBase<None, Id<Junction>>
{
    public static readonly TrainJunction None = new None();
}