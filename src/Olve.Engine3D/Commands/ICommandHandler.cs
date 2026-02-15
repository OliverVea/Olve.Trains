namespace Olve.Engine3D.Commands;

public interface ICommandHandler
{
    string Verb { get; }
    string HelpString { get; }
    IReadOnlyList<CommandArgument> Arguments { get; }
    Result<CommandOutput> Handle(CommandContext commandContext);
}
