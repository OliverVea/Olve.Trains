using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public class ScreenshotService(Provider<GL> glProvider, Provider<IWindow> windowProvider, KeyboardManager keyboardManager) : ISceneService
{
    public int Priority => int.MaxValue;

    private bool _takeScreenshot;

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        _takeScreenshot = keyboardManager.State.IsKeyPressed(Key.F12);
        return Pass.Pass;
    }

    public Result Render(TimeSpan deltaTime)
    {
        if (_takeScreenshot)
        {
            TakeScreenshot();
        }

        return Result.Success();
    }

    public void TakeScreenshot()
    {
        var width = windowProvider.Value.FramebufferSize.X;
        var height = windowProvider.Value.FramebufferSize.Y;
        BufferHelper.WithSpan<byte>(width * height,
            span =>
            {
                glProvider.Value.ReadPixels(0,
                    0,
                    (uint)width,
                    (uint)height,
                    GLEnum.Rgba,
                    GLEnum.UnsignedByte,
                    span);

                Console.WriteLine("Test");
            });
    }
}