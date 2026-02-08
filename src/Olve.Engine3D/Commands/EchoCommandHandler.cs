using System.Text;
using Olve.Logging;

namespace Olve.Engine3D.Commands;

public class EchoCommandHandler(ILoggingManager loggingManager) : ICommandHandler
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
        var logMessage = new LogMessage(
            LogLevel.Info,
            output,
            null,
            null,
            DateTime.Now,
            [Verb]
        );

        loggingManager.Log(logMessage);
        _builder.Clear();

        return Result.Success();
    }
}
