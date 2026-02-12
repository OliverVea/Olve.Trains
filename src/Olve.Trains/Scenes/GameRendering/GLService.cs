using System.Drawing;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class GLService(Provider<GL> glProvider) : ISceneService
{
    public int Priority => -10;

    public Result Load()
    {
        glProvider.Value.Enable(EnableCap.Multisample);
        glProvider.Value.Disable(EnableCap.CullFace);
        glProvider.Value.Enable(EnableCap.DepthTest);

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        glProvider.Value.ClearColor(Color.CornflowerBlue);
        glProvider.Value.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        return Result.Success();
    }
}