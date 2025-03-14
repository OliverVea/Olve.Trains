using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Graphics;

public interface IDrawable
{
    void Draw(Matrix world, Matrix view, Matrix projection);
}