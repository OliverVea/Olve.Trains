namespace Olve.Engine3D.Utilities;

public class RotatingIndex(int max)
{
    public int Current { get; private set; }

    public int GetNext()
    {
        var value = Current;
        IncrementCurrent();
        return value;
    }

    public IEnumerable<int> GetMultiple(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return Current;
            IncrementCurrent();
        }
    }

    private void IncrementCurrent() => Current = (Current + 1) % max;
}