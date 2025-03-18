using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera.Cameras;

public class Camera<TView, TProjection>(TView view, TProjection projection) : ICamera
    where TView : IView
    where TProjection : IProjection
{
    public TView View { get; set; } = view;
    public TProjection Projection { get; set; } = projection;

    public Matrix4X4<float> GetViewMatrix() => View.GetViewMatrix();
    public Matrix4X4<float> GetProjectionMatrix() => Projection.GetProjectionMatrix();

    public Result<Ray3D<float>> GetRay(Vector2D<float> screenPosition)
    {
        var viewMatrix = GetViewMatrix();
        var projectionMatrix = GetProjectionMatrix();

        var invertResults = Result.Chain(
            () => viewMatrix.Invert(),
            () => projectionMatrix.Invert()
        );

        if (invertResults.TryPickProblems(out var problems, out var invertedMatrices))
        {
            return problems.Prepend("Failed to get ray from screen position '{0}'", screenPosition);
        }

        var (iViewMatrix, iProjectionMatrix) = invertedMatrices;

        var ndcX = screenPosition.X;
        var ndcY = screenPosition.Y;
        var farClip = new Vector4D<float>(ndcX, ndcY, 1f, 1f);

        var farView = Vector4D.Transform(farClip, iProjectionMatrix);
        farView /= farView.W;

        var farWorld4 = Vector4D.Transform(farView, iViewMatrix);
        farWorld4 /= farWorld4.W;
        var farWorld = new Vector3D<float>(farWorld4.X, farWorld4.Y, farWorld4.Z);

        var direction = Vector3D.Normalize(farWorld - view.Position);

        return new Ray3D<float>(view.Position, direction);
    }
}

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