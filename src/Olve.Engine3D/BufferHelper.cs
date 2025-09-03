using System.Buffers;
using System.Runtime.CompilerServices;

namespace Olve.Engine3D;

/// <summary>
/// Utility for performing high‑performance work on a <see cref="Span{T}"/>,
/// using <c>stackalloc</c> up to a configurable threshold, then falling back
/// to renting from the shared <see cref="ArrayPool{T}"/>.
/// </summary>
public static class BufferHelper
{
    private const int MaxStackBytes = 8 * 1024;

    /// <summary>
    /// Executes <paramref name="action"/> on a <see cref="Span{T}"/> of length <paramref name="spanSize"/>.
    /// If <paramref name="spanSize"/> is less than or equal to <paramref name="maxStackBytes"/> in bytes (depends on the size of the element type),
    /// the span is allocated on the stack. Otherwise, a pooled array is rented (and returned) after use.
    /// </summary>
    /// <typeparam name="T">The element type of the span. Must be an unmanaged type.</typeparam>
    /// <param name="spanSize">The number of elements in the span. Must be non-negative.</param>
    /// <param name="action">The delegate to execute against the span.</param>
    /// <param name="ensureClearedArray">
    /// If <c>true</c> and a pooled array is used, the contents
    /// of the rented buffer will be cleared before invoking <paramref name="action"/>.
    /// </param>
    /// <param name="maxStackBytes">
    /// The threshold at which the helper switches from <c>stackalloc</c> to array pooling.
    /// Defaults to <c>MaxStackBytes</c> (8096).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="spanSize"/> is negative.</exception>
    public static void WithSpan<T>(
        int spanSize,
        Action<Span<T>> action,
        bool ensureClearedArray = false,
        int maxStackBytes = MaxStackBytes) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spanSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxStackBytes);
        
        var elemSize = Unsafe.SizeOf<T>();
        var neededBytes = checked(spanSize * elemSize);
        T[]? array = null;

        try
        {
            if (neededBytes > maxStackBytes)
            {
                array = ArrayPool<T>.Shared.Rent(spanSize);
                if (ensureClearedArray) { Array.Clear(array, 0, spanSize); }
                action(array.AsSpan(0, spanSize));
            }
            else
            {
                Span<T> span = stackalloc T[spanSize];
                if (ensureClearedArray) { span.Clear(); }
                action(span);
            }
        }
        finally
        {
            if (array is not null)
            {
                ArrayPool<T>.Shared.Return(array, clearArray: ensureClearedArray);
            }
        }
    }


    /// <summary>
    /// Executes <paramref name="func"/> on a <see cref="Span{TIn}"/> of length <paramref name="spanSize"/>
    /// and returns its result. Uses <c>stackalloc</c> up to <paramref name="maxStackBytes"/> (depends on element size), otherwise
    /// a pooled array is rented and returned after use.
    /// </summary>
    /// <typeparam name="TIn">The element type of the span. Must be an unmanaged type.</typeparam>
    /// <typeparam name="TOut">The return type of the delegate.</typeparam>
    /// <param name="spanSize">The number of elements in the span. Must be non-negative.</param>
    /// <param name="func">The delegate that processes the span and returns a value.</param>
    /// <param name="ensureClearedArray">
    /// If <c>true</c> and a pooled array is used, the contents
    /// of the rented buffer will be cleared before invoking <paramref name="func"/>.
    /// </param>
    /// <param name="maxStackBytes">
    /// The threshold at which the helper switches from <c>stackalloc</c> to array pooling.
    /// Defaults to <c>MaxStackAllocationSize</c> (2048).
    /// </param>
    /// <returns>The result produced by <paramref name="func"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="spanSize"/> is negative.</exception>
    public static TOut WithSpan<TIn, TOut>(
        int spanSize,
        Func<Span<TIn>, TOut> func,
        bool ensureClearedArray = false,
        int maxStackBytes = MaxStackBytes) where TIn : unmanaged
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spanSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxStackBytes);
        
        var elemSize = Unsafe.SizeOf<TIn>();
        var neededBytes = checked(spanSize * elemSize);
        TIn[]? array = null;

        try
        {
            if (neededBytes > maxStackBytes)
            {
                array = ArrayPool<TIn>.Shared.Rent(spanSize);
                if (ensureClearedArray) Array.Clear(array, 0, spanSize);
                return func(array.AsSpan(0, spanSize));
            }
            else
            {
                Span<TIn> span = stackalloc TIn[spanSize];
                if (ensureClearedArray) span.Clear();
                return func(span);
            }
        }
        finally
        {
            if (array is not null)
            {
                ArrayPool<TIn>.Shared.Return(array, clearArray: ensureClearedArray);
            }
        }
    }
}