using Microsoft.Xna.Framework;

namespace Engine.Camera;

public interface ICamera
{
    Matrix GetViewMatrix();
    Matrix GetProjectionMatrix();
}