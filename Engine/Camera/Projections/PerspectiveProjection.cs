using Microsoft.Xna.Framework;

namespace Engine.Camera.Projections;

public class PerspectiveProjection : ProjectionBase
{
    public float FieldOfView { get; set; } = MathHelper.PiOver4;

    public override Matrix GetProjectionMatrix()
    {
        return Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }
}