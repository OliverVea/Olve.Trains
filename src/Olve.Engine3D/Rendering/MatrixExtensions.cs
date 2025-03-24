namespace Olve.Engine3D.Rendering;

public static class MatrixExtensions
{
    public static void CopyTo<T>(this Matrix4X4<T> matrix, Span<T> buffer) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                buffer[i * 4 + j] = matrix[i, j];
            }
        }
    }
}