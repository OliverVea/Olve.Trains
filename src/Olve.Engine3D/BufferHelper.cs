using System.Buffers;

namespace Olve.Engine3D;

public static class BufferHelper
{
    private const int MaxStackAllocationSize = 2048;

    public static void UsingSpan<T>(int spanSize, Action<Span<T>> action, int maxAllocationSize = MaxStackAllocationSize) where T : unmanaged
    {
        T[]? array = null;

        try
        {
            array = spanSize > maxAllocationSize ? ArrayPool<T>.Shared.Rent(spanSize) : null;
            var span = array is not null ? array.AsSpan(0, spanSize) : stackalloc T[spanSize];

            action(span);
        }
        finally
        {
            if (array is not null)
            {
                ArrayPool<T>.Shared.Return(array);
            }
        }
    }
}