namespace Olve.Engine3D.Commands;

public record CommandArgument(string Key, string HelpString, bool Required = false)
{
    public string Name { get; } = $"{Key}";
}
