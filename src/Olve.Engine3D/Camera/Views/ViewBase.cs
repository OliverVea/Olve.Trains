namespace Olve.Engine3D.Camera.Views;

public abstract class ViewBase : IView
{
    public Vector3D<float> Position { get; set; }
    public Matrix4X4<float> Rotation { get; set; } = Matrix4X4<float>.Identity;

    public abstract Matrix4X4<float> GetViewMatrix();
}