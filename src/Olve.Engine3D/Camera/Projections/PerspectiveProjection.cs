namespace Olve.Engine3D.Camera.Projections;

public class PerspectiveProjection : ProjectionBase
{
    public float FieldOfView { get; set; } = PiOver4;

    public override Matrix4X4<float> GetProjectionMatrix()
    {
        return Matrix4X4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }
}