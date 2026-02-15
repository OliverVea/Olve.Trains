namespace Olve.Engine3D.Commands;

public readonly record struct CommandOutput(string Text)
{
    public static CommandOutput Empty => new(string.Empty);
}
