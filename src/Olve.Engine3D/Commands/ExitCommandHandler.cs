namespace Olve.Engine3D.Commands;

public class ExitCommandHandler(GameManager gameManager) : ICommandHandler
{
    public string Verb => "exit";
    public string HelpString => "Exits the game";
    public IReadOnlyList<CommandArgument> Arguments => [];

    public Result<CommandOutput> Handle(CommandContext commandContext)
    {
        gameManager.Stop();
        return new CommandOutput("Exiting game");
    }
}
