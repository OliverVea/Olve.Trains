using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Money;

namespace Olve.Trains.Commands.GameLogic;

public class QueryMoneyHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    MoneyService moneyService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "query-money";
    public override string HelpString => "Queries the current money balance";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var json = JsonSerializer.Serialize(new
        {
            balance = moneyService.Balance,
        });

        return new CommandOutput(json);
    }
}
