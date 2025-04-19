using System.Drawing;
using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes;

public class GLService(Provider<GL> glProvider) : SceneService
{
    public override int Priority => -10;

    public override Result Load()
    {
        glProvider.Value.Enable(EnableCap.Multisample);
        glProvider.Value.Disable(EnableCap.CullFace);
        glProvider.Value.Enable(EnableCap.DepthTest);
        
        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        glProvider.Value.ClearColor(Color.CornflowerBlue);
        glProvider.Value.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        return Result.Success();
    }
}