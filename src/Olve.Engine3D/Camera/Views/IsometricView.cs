namespace Olve.Engine3D.Camera.Views;

    public class IsometricView : ViewBase
    {
        public override Matrix4X4<float> GetViewMatrix()
        {
            var translation = Matrix4X4.CreateTranslation(-Position);
            var transform = translation * Rotation;


            return transform;
        }
    }