using Olve.Engine3D.Physics3D.Collisions;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

internal static class TrackSegmentHelper
{
    public const int SegmentCount = 10;
    public const float TrackWidth = 0.16f * 0.6f;
    public const float TrackHeight = 0.02f * 0.6f;

    public static readonly BoxColliderShape HalfUnitBox = new(new(0.5f, 0.5f, 0.5f));

    public static Matrix4X4<float> ComputeSegmentOBBMatrix(Vector3D<float> from, Vector3D<float> to)
    {
        var midpoint = (from + to) * 0.5f;
        var direction = to - from;
        var length = direction.Length;

        if (length < 1e-6f)
        {
            return Matrix4X4.CreateTranslation(midpoint);
        }

        var forward = Vector3D.Normalize(direction);
        var up = new Vector3D<float>(0, 1, 0);
        var right = Vector3D.Normalize(Vector3D.Cross(up, forward));

        // Re-orthogonalize up in case forward is near-vertical
        up = Vector3D.Cross(forward, right);

        // Scale: X = width, Y = height, Z = segment length
        var scale = Matrix4X4.CreateScale(TrackWidth, TrackHeight, length);

        // Rotation matrix from basis vectors (row-major: rows are axes)
        var rotation = new Matrix4X4<float>(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);

        var translation = Matrix4X4.CreateTranslation(midpoint);

        return scale * rotation * translation;
    }
}
