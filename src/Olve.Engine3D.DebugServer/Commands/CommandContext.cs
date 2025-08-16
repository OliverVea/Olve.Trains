namespace Olve.Engine3D.DebugServer.Commands;

public readonly record struct CommandContext(IReadOnlyDictionary<string, string> Arguments)
{
    public string? GetArgument(CommandArgument argument)
    {
        return Arguments.TryGetValue(argument.Key, out var value) 
            ? value
            : argument.Required
                ? throw new InvalidOperationException("Required argument not found in arguments")
                : null;
    }
}