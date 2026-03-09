using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Rendering;

namespace Olve.Engine3D.Camera;

public static class CameraRayExtensions
{
    public static Result<Ray3D<float>> GetRay(this Camera<FirstPersonView, PerspectiveProjection> camera, Vector2D<float> ncdScreenPosition)
    {
        var viewMatrix = camera.GetViewMatrix();
        var projectionMatrix = camera.GetProjectionMatrix();

        var invertResults = Result.Concat(
            () => viewMatrix.Invert().IfProblem(p => p.Prepend("Failed to invert view matrix")),
            () => projectionMatrix.Invert().IfProblem(p => p.Prepend("Failed to invert projection matrix"))
        );

        if (invertResults.TryPickProblems(out var problems, out var invertedMatrices))
        {
            return problems.Prepend("Failed to get ray from screen position '{0}'", ncdScreenPosition);
        }

        var (iViewMatrix, iProjectionMatrix) = invertedMatrices;

        var ndcX = ncdScreenPosition.X;
        var ndcY = ncdScreenPosition.Y;
        var farClip = new Vector4D<float>(ndcX, ndcY, 1f, 1f);

        var farView = Vector4D.Transform(farClip, iProjectionMatrix);
        farView /= farView.W;

        var farWorld4 = Vector4D.Transform(farView, iViewMatrix);
        farWorld4 /= farWorld4.W;
        var farWorld = new Vector3D<float>(farWorld4.X, farWorld4.Y, farWorld4.Z);

        var direction = Vector3D.Normalize(farWorld - camera.View.Position);

        return new Ray3D<float>(camera.View.Position, direction);
    }


    public static Result<Ray3D<float>> GetRay(this Camera<IsometricView, OrthographicProjection> camera, Vector2D<float> ncdScreenPosition)
    {
        if (camera.GetViewMatrix().Invert().TryPickProblems(out var problems, out var invertedViewMatrix))
        {
            return problems.Prepend("Failed to get ray from screen position '{0}'", ncdScreenPosition);
        }

        var invertedRotation = invertedViewMatrix.ExtractRotation();

        var right = Vector3D<float>.UnitX;
        var up = -Vector3D<float>.UnitY;
        var forward = Vector3D<float>.UnitZ;

        right = Vector3D.Transform(right, invertedRotation);
        up = Vector3D.Transform(up, invertedRotation);
        forward = Vector3D.Transform(forward, invertedRotation);

        var halfWidth = camera.Projection.OrthographicSize * camera.Projection.AspectRatio;
        var halfHeight = camera.Projection.OrthographicSize;

        var x = ncdScreenPosition.X * halfWidth;
        var y = ncdScreenPosition.Y * halfHeight;

        var worldPosition = camera.View.Position + right * x + up * y;

        // Slide origin along the ray direction so its Y matches the camera Y.
        // This prevents XZ overshoot in heightmap raycasting when the origin
        // is far above terrain (e.g. clicking near the top of the screen).
        if (float.Abs(forward.Y) > 1e-6f)
        {
            var t = (camera.View.Position.Y - worldPosition.Y) / forward.Y;
            worldPosition += forward * t;
        }

        return new Ray3D<float>(worldPosition, forward);
    }
}