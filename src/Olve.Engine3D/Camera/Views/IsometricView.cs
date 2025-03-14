using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Camera.Views;

    public class IsometricView : ViewBase
    {
        public float Angle { get; set; } = 0;

        public override Matrix GetViewMatrix()
        {
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, Angle);

            var forward = Vector3.Transform(Vector3.Forward, Rotation);
            var rotatedForward = Vector3.Transform(forward, rotation);

            return Matrix.CreateLookAt(Position, Position + rotatedForward, Vector3.Up);
        }
    }