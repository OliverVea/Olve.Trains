using System.Buffers;

namespace Olve.Engine3D;

/// <summary>
/// Utility for performing high‑performance work on a <see cref="Span{T}"/>,
/// using <c>stackalloc</c> up to a configurable threshold, then falling back
/// to renting from the shared <see cref="ArrayPool{T}"/>.
/// </summary>
public static class BufferHelper
{
    private const int MaxStackAllocationSize = 2048;

    /// <summary>
    /// Executes <paramref name="action"/> on a <see cref="Span{T}"/> of length <paramref name="spanSize"/>.
    /// If <paramref name="spanSize"/> is less than or equal to <paramref name="maxStackAllocationSize"/>,
    /// the span is allocated on the stack. Otherwise, a pooled array is rented (and returned) after use.
    /// </summary>
    /// <typeparam name="T">The element type of the span. Must be an unmanaged type.</typeparam>
    /// <param name="spanSize">The number of elements in the span. Must be non-negative.</param>
    /// <param name="action">The delegate to execute against the span.</param>
    /// <param name="ensureClearedArray">
    /// If <c>true</c> and a pooled array is used, the contents
    /// of the rented buffer will be cleared before invoking <paramref name="action"/>.
    /// </param>
    /// <param name="maxStackAllocationSize">
    /// The threshold at which the helper switches from <c>stackalloc</c> to array pooling.
    /// Defaults to <c>MaxStackAllocationSize</c> (2048).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="spanSize"/> is negative.</exception>
    public static void WithSpan<T>(
        int spanSize,
        Action<Span<T>> action,
        bool ensureClearedArray = false,
        int maxStackAllocationSize = MaxStackAllocationSize) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spanSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxStackAllocationSize);
        
        T[]? array = null;

        try
        {
            array = spanSize > maxStackAllocationSize ? ArrayPool<T>.Shared.Rent(spanSize) : null;
            if (ensureClearedArray && array is not null)
            {
                Array.Clear(array, 0, spanSize);
            }
            
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


    /// <summary>
    /// Executes <paramref name="func"/> on a <see cref="Span{TIn}"/> of length <paramref name="spanSize"/>
    /// and returns its result. Uses <c>stackalloc</c> up to <paramref name="maxStackAllocationSize"/>, otherwise
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
    /// <param name="maxStackAllocationSize">
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
        int maxStackAllocationSize = MaxStackAllocationSize) where TIn : unmanaged
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spanSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxStackAllocationSize);
        
        TIn[]? array = null;

        try
        {
            array = spanSize > maxStackAllocationSize ? ArrayPool<TIn>.Shared.Rent(spanSize) : null;
            if (ensureClearedArray && array is not null)
            {
                Array.Clear(array, 0, spanSize);
            }
            
            var span = array is not null ? array.AsSpan(0, spanSize) : stackalloc TIn[spanSize];

            return func(span);
        }
        finally
        {
            if (array is not null)
            {
                ArrayPool<TIn>.Shared.Return(array);
            }
        }
    }
}