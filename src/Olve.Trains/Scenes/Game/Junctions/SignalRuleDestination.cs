using Olve.Engine3D;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.Types;
using OneOf;

namespace Olve.Trains.Scenes.Game.Junctions;

[GenerateOneOf]
public partial class SignalRuleDestination : OneOfBase<Any, CardinalDirection, Id<Track>>
{
    public override string ToString()
    {
        return Match(
            x => x.ToString(),
            x => x.ToString(),
            x => x.ToString());
    }
}