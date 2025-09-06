namespace Olve.Engine3D.Rendering;

public static class MatrixExtensions
{
    public static Result<Matrix4X4<T>> Invert<T>(this Matrix4X4<T> matrix)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        var result = Matrix4X4.Invert(matrix, out var output);

        return result
            ? Result<Matrix4X4<T>>.Success(output)
            : new ResultProblem("Failed to invert matrix '{0}'", matrix);
    }
    
    public static void CopyTo<T>(this Matrix3X3<T> matrix, Span<T> buffer) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                buffer[i * 3 + j] = matrix[i, j];
            }
        }
    }

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

    public static Matrix3X3<T> Extract3X3<T>(this Matrix4X4<T> matrix) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        return new Matrix3X3<T>(
            matrix.M11, matrix.M12, matrix.M13,
            matrix.M21, matrix.M22, matrix.M23,
            matrix.M31, matrix.M32, matrix.M33);
    }

    public static Matrix4X4<T> ExtractRotation<T>(this Matrix4X4<T> matrix) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        return new Matrix4X4<T>(
            matrix.M11, matrix.M12, matrix.M13, default,
            matrix.M21, matrix.M22, matrix.M23, default,
            matrix.M31, matrix.M32, matrix.M33, default,
            default, default, default, matrix.M44);
    }
}