namespace Olve.Trains.Scenes.GameLogic.Ordering;

/// <summary>
/// Hands out increasing numbers that order game events: entity creation (<see cref="IHasCreatedSequence"/>),
/// a train arriving on a track, and so on. Numbers only mean something relative to each other; a loaded game
/// restores its entities in saved order and numbers them afresh. Main thread only.
/// </summary>
public class SequenceService
{
    private long _last;

    public long Next() => ++_last;
}
