using Microsoft.Xna.Framework;

namespace Engine.Camera.Views;

public class FirstPersonView: ViewBase
{
    public override Matrix GetViewMatrix()
    {
        var forward = Vector3.Transform(Vector3.Forward, Rotation);
        var up = Vector3.Transform(Vector3.Up, Rotation);

        return Matrix.CreateLookAt(Position, Position + forward, up);
    }
}