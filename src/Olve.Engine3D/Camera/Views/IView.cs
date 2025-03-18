namespace Olve.Engine3D.Camera.Views;

public interface IView
{
    Vector3D<float> Position { get; set; }
    Quaternion<float> Rotation { get; set; }

    Matrix4X4<float> GetViewMatrix();
}