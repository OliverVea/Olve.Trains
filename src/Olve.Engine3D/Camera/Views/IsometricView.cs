namespace Olve.Engine3D.Camera.Views;

    public class IsometricView : ViewBase
    {
        public float Angle { get; set; } = 0;

        public override Matrix4X4<float> GetViewMatrix()
        {
            var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, Angle);

            var forward = Vector3D.Transform(Vector3D<float>.UnitY, Rotation);
            var rotatedForward = Vector3D.Transform(forward, rotation);

            return Matrix4X4.CreateLookAt<float>(Position, Position + rotatedForward, Vector3D<float>.UnitY);
        }
    }