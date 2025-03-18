namespace Olve.Engine3D.Camera.Projections;

public interface IProjection
{
    float AspectRatio { get; set; }
    float NearPlane { get; set; }
    float FarPlane { get; set; }

    Matrix4X4<float> GetProjectionMatrix();
}