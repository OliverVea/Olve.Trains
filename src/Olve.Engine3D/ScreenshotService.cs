using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Silk.NET.Input;

namespace Olve.Engine3D;

public class ScreenshotService(KeyboardManager keyboardManager, ScreenshotManager screenshotManager) : ISceneService
{
    private static readonly IPath ScreenshotsFolder = Path.Create("screenshots");

    public int Priority => int.MaxValue;

    public Result<Pass> Input()
    {
        if (keyboardManager.State.IsKeyPressed(Key.F12))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var folder = ScreenshotsFolder / timestamp;
            screenshotManager.RequestFramebufferDump(folder);
        }

        return Pass.Pass;
    }
}
