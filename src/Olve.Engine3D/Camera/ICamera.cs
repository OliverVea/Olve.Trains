using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Camera;

public interface ICamera
{
    Matrix GetViewMatrix();
    Matrix GetProjectionMatrix();
}