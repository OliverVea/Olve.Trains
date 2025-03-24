namespace Olve.Engine3D.Rendering;

public static class VectorExtensions
{
    public static void CopyTo<T>(this Vector3D<T>[] vertices, Span<T> buffer, int stride = 3, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
            buffer[j + 2] = vertices[i].Z;
        }
    }

    public static void CopyTo<T>(this Vector4D<T>[] vertices, Span<T> buffer, int stride = 4, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
            buffer[j + 2] = vertices[i].Z;
            buffer[j + 3] = vertices[i].Z;
        }
    }

    public static void CopyTo<T>(this Vector2D<T>[] vertices, Span<T> buffer, int stride, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
        }
    }
}