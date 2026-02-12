using OneOf;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

[GenerateOneOf]
public partial class SignalRuleDistribution : OneOfBase<RoundRobin>
{
    public override string ToString()
    {
        return Match(x => x.ToString()) ?? "null";
    }
}