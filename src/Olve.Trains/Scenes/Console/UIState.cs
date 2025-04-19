namespace Olve.Trains.Scenes.Console;

public readonly record struct UIState(UIDayTime DayTime, IReadOnlyList<string> Console, string? ConsoleCommand = null);