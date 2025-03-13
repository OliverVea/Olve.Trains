using Microsoft.Xna.Framework;

namespace Engine.Camera.Views;

public class OrbitalView : IView
{
    public Vector3 Target { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public Vector3 Position
    {
        get => CalculatePosition();
        set => SetPosition(value);
    }

    public Quaternion Rotation
    {
        get => CalculateRotation();
        set => SetRotation(value);
    }

    private Vector3 CalculatePosition()
    {
        throw new NotImplementedException();
    }

    private void SetPosition(Vector3 value)
    {
        throw new NotImplementedException();
    }

    private Quaternion CalculateRotation()
    {
        throw new NotImplementedException();
    }

    private void SetRotation(Quaternion value)
    {
        throw new NotImplementedException();
    }

    public Matrix GetViewMatrix()
    {
        return Matrix.CreateLookAt(Position, Target, Vector3.Up);
    }
}