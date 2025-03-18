namespace Olve.Engine3D.Camera.Projections;

public class OrthographicProjection : ProjectionBase
{
    public float OrthographicSize { get; set; } = 10f;

    public override Matrix4X4<float> GetProjectionMatrix()
    {
        var halfWidth = OrthographicSize * AspectRatio;
        var halfHeight = OrthographicSize;

        return Matrix4X4.CreateOrthographic(halfWidth * 2, halfHeight * 2, NearPlane, FarPlane);
    }
}