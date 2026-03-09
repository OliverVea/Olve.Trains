using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Utilities.Types;
using OneOf;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

[GenerateOneOf]
public partial class SignalRuleTrain : OneOfBase<Any, Id<TrainGroup>, Id<Train>>
{
    public override string ToString()
    {
        return Match(
            x => x.ToString(),
            x => x.ToString(),
            x => x.ToString());
    }
}