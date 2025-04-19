namespace Olve.Trains.Scenes.Console;

public readonly record struct UIDayTime(int Hour, int Minute)
{
    public override string ToString() => $"{Hour:D2}:{Minute:D2}";
}