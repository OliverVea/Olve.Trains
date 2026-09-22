using Microsoft.Extensions.Logging;

namespace Olve.Trains.Scenes.GameLogic.Money;

public class MoneyService(ILogger<MoneyService> logger, GameSceneArguments arguments)
{
    public int Balance { get; private set; } = arguments.StartingMoney;

    public void Add(int amount, string reason)
    {
        if (amount == 0) return;
        Balance += amount;
        logger.LogInformation("Balance {Sign}{Amount} ({Reason}) → {Balance}",
            amount >= 0 ? "+" : "", amount, reason, Balance);
    }

    public bool CanAfford(int amount) => Balance >= amount;

    public bool TryCharge(int amount, string reason)
    {
        if (amount < 0)
        {
            logger.LogWarning("TryCharge called with negative amount {Amount} for '{Reason}'", amount, reason);
            return false;
        }

        if (!CanAfford(amount))
        {
            logger.LogInformation("Charge of {Amount} for '{Reason}' denied (balance: {Balance})",
                amount, reason, Balance);
            return false;
        }

        Balance -= amount;
        logger.LogInformation("Balance -{Amount} ({Reason}) → {Balance}", amount, reason, Balance);
        return true;
    }
}
