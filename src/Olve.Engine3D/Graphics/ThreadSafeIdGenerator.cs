namespace Olve.Engine3D.Graphics;

public class ThreadSafeIdGenerator
{
    private uint _nextId;

    public uint Next()
    {
        return Interlocked.Increment(ref _nextId);
    }
}