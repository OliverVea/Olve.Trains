using OneOf;
using OneOf.Types;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

[GenerateOneOf]
public partial class RuleEvaluationResult : OneOfBase<None, TransferredTracks>
{
    public static readonly RuleEvaluationResult None = new None();
}