using System.Text;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Logging;

namespace Olve.Engine3D.Commands;

public class HelpCommandHandler(CommandHandlerServiceCollection commandHandlers, ILogger<HelpCommandHandler> logger) : ICommandHandler
{
    public static readonly CommandArgument CommandArgument = new("command", "the command to provide help with");

    private readonly StringBuilder _builder = new();

    public string Verb => "help";
    public string HelpString => "Prints the help message of the application or a specific command";

    public IReadOnlyList<CommandArgument> Arguments { get; } = [CommandArgument];

    public Result<CommandOutput> Handle(CommandContext context)
    {
        if (context.GetArgument(CommandArgument) is {} command)
        {
            HandleCommand(command);
        }
        else
        {
            HandleNoCommand();
        }

        var messageText = _builder.ToString();
        logger.LogInformation("{Message}", messageText);

        _builder.Clear();

        return new CommandOutput(messageText);
    }

    private void HandleNoCommand()
    {
        _builder.AppendLine("Available commands:");
        foreach (var h in commandHandlers.OrderBy(x => x.Verb))
        {
            var argsUsage = string.Join(" ", h.Arguments.Select(a => a.Required ? $"<{a.Name}>" : $"[{a.Name}]"));
            _builder.AppendLine($"  {h.Verb} {argsUsage}".TrimEnd());
        }
    }

    private void HandleCommand(string command)
    {
        var handler = commandHandlers.FirstOrDefault(h => string.Equals(h.Verb, command, StringComparison.OrdinalIgnoreCase));

        if (handler is null)
        {
            _builder.AppendLine($"Unknown command '{command}'.");
            return;
        }

        _builder.AppendLine($"Usage: {handler.Verb} " +
                            string.Join(" ",
                                    handler.Arguments.Select(a => a.Required ? $"<{a.Name}>" : $"[{a.Name}]"))
                                .TrimEnd());
        _builder.Append("Description: ");
        _builder.AppendLine(handler.HelpString);

        if (handler.Arguments.Any())
        {
            _builder.AppendLine();
            _builder.AppendLine("Arguments:");
            foreach (var arg in handler.Arguments)
            {
                var req = arg.Required ? "(required)" : "(optional)";
                _builder.AppendLine($"  {arg.Name} {req}: {arg.HelpString}");
            }
        }
    }
}
