using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Camera.Controllers;

public interface ICameraController
{
    ICamera Camera { get; }

    void Update(GameTime gameTime);
}