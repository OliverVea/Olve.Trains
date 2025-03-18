using Silk.NET.Maths;

namespace Olve.Engine3D.Camera.Views;

public class OrbitalView : IView
{
    public Vector3D<float> Target { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public Vector3D<float> Position
    {
        get => CalculatePosition();
        set => SetPosition(value);
    }

    public Quaternion<float> Rotation
    {
        get => CalculateRotation();
        set => SetRotation(value);
    }

    private Vector3D<float> CalculatePosition()
    {
        throw new NotImplementedException();
    }

    private void SetPosition(Vector3D<float> value)
    {
        throw new NotImplementedException();
    }

    private Quaternion<float> CalculateRotation()
    {
        throw new NotImplementedException();
    }

    private void SetRotation(Quaternion<float> value)
    {
        throw new NotImplementedException();
    }

    public Matrix4X4<float> GetViewMatrix()
    {
        return Matrix4X4.CreateLookAt(Position, Target, new Vector3D<float>(0, 1, 0));
    }
}