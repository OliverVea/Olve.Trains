using System.Text;
using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Commands;

public class EchoCommandHandler(ILogger<EchoCommandHandler> logger) : ICommandHandler
{
    private const string MessageKey = "message";
    private readonly StringBuilder _builder = new();

    public string Verb => "echo";
    public string HelpString => "Echoes the provided message back to the user";
    public IReadOnlyList<CommandArgument> Arguments { get; } = [
        new(MessageKey, "the message to echo", true)
    ];

    public Result Handle(CommandContext context)
    {
        if (context.Arguments.TryGetValue(MessageKey, out var message))
        {
            _builder.Append(message);
        }

        var output = _builder.ToString();
        logger.LogInformation("{Output}", output);
        _builder.Clear();

        return Result.Success();
    }
}
