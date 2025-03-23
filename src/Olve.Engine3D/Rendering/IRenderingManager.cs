using Olve.Engine3D.Graphics;

namespace Olve.Engine3D.Rendering;

public interface IRenderingManager
{
    Result Render(RenderingParameters parameters);
}