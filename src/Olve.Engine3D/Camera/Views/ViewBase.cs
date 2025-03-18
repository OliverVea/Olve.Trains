namespace Olve.Engine3D.Camera.Views;

public abstract class ViewBase : IView
{
    public Vector3D<float> Position { get; set; }
    public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;

    public abstract Matrix4X4<float> GetViewMatrix();
}