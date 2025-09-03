using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.Game.Junctions;

[GenerateOneOf]
public partial class RuleEvaluationResult : OneOfBase<None, TransferredTracks>
{
    public static RuleEvaluationResult None = new None();
}