namespace Olve.Engine3D.Camera;

public interface ICamera
{
    Matrix4X4<float> GetViewMatrix();
    Matrix4X4<float> GetProjectionMatrix();
}