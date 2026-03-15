namespace Olve.Engine3D.Time;

public class DeltaTimeService
{
    public TimeSpan RawDeltaTime { get; private set; }
    public TimeSpan ScaledDeltaTime { get; private set; }
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// Called by GameManager each frame before services run.
    /// </summary>
    public void SetFrameDelta(TimeSpan rawDelta)
    {
        RawDeltaTime = rawDelta;
        ScaledDeltaTime = rawDelta * TimeScale;
    }
}
