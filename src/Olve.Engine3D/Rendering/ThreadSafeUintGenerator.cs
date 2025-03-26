namespace Olve.Engine3D.Rendering;

public class ThreadSafeUintGenerator
{
    private uint _nextId;

    public uint Next()
    {
        return Interlocked.Increment(ref _nextId);
    }

    public static readonly ThreadSafeUintGenerator Shared = new();
}