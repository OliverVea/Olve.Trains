using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera;

public static class CameraRayExtensions
{
    public static Result<Ray3D<float>> GetRay(this Camera<FirstPersonView, PerspectiveProjection> camera, Vector2D<float> screenPosition)
    {
        var viewMatrix = camera.GetViewMatrix();
        var projectionMatrix = camera.GetProjectionMatrix();

        var invertResults = Result.Concat(
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

        var direction = Vector3D.Normalize(farWorld - camera.View.Position);

        return new Ray3D<float>(camera.View.Position, direction);
    }


    public static Result<Ray3D<float>> GetRay(this Camera<IsometricView, OrthographicProjection> camera, Vector2D<float> screenPosition)
    {
        var viewMatrix = camera.GetViewMatrix();
        var projectionMatrix = camera.GetProjectionMatrix();

        var invertResults = Result.Concat(
            () => viewMatrix.Invert(),
            () => projectionMatrix.Invert()
        );

        if (invertResults.TryPickProblems(out var problems, out var invertedMatrices))
        {
            return problems.Prepend("Failed to get ray from screen position '{0}'", screenPosition);
        }
        var (iViewMatrix, iProjectionMatrix) = invertedMatrices;

        var ndcNear = new Vector4D<float>(screenPosition.X, screenPosition.Y, -1f, 1f);

        var viewSpace = Vector4D.Transform(ndcNear, iProjectionMatrix);
        viewSpace /= viewSpace.W;

        var worldSpace = Vector4D.Transform(viewSpace, iViewMatrix);
        worldSpace /= worldSpace.W;
        var origin = new Vector3D<float>(worldSpace.X, worldSpace.Y, worldSpace.Z);

        var forward4 = Vector4D.Transform(new Vector4D<float>(0, 0, -1, 0), iViewMatrix);
        var direction = Vector3D.Normalize(new Vector3D<float>(forward4.X, forward4.Y, forward4.Z));

        return new Ray3D<float>(origin, direction);
    }
}