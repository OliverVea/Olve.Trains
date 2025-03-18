namespace Olve.Engine3D.Camera.Views;

public class FirstPersonView: ViewBase
{
    public override Matrix4X4<float> GetViewMatrix()
    {
        var forward = Vector3D.Transform(Vector3D<float>.UnitZ, Rotation);
        var up = Vector3D.Transform(Vector3D<float>.UnitY, Rotation);

        return Matrix4X4.CreateLookAt(Position, Position + forward, up);
    }
}