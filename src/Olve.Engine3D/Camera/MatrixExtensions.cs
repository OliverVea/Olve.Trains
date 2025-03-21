namespace Olve.Engine3D.Camera;

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
}