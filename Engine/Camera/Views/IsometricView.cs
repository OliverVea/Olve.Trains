using Microsoft.Xna.Framework;

namespace Engine.Camera.Views;

    public class IsometricView : ViewBase
    {
        public override Matrix GetViewMatrix()
        {
            var forward = Vector3.Transform(Vector3.Forward, Rotation);

            return Matrix.CreateLookAt(Position, Position + forward, Vector3.Up);
        }
    }