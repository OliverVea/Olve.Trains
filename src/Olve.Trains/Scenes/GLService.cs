using System.Drawing;
using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes;

public class GLService(ILoggingManager loggingManager, Provider<GL> glProvider) : SceneService(loggingManager)
{
    public override int Priority => -10;

    protected override Result OnLoad()
    {
        glProvider.Value.Enable(EnableCap.Multisample);
        glProvider.Value.Disable(EnableCap.CullFace);
        glProvider.Value.Enable(EnableCap.DepthTest);
        
        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        glProvider.Value.ClearColor(Color.CornflowerBlue);
        glProvider.Value.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        return Result.Success();
    }
}