using System.Drawing;
using Olve.Engine3D;
using Olve.Engine3D.Events;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.GameRendering;

public class GLService(
    Provider<GL> glProvider,
    Provider<IWindow> windowProvider,
    FramebufferManager framebufferManager,
    RenderPassManager renderPassManager,
    ScreenPassManager screenPassManager,
    ScreenResizedEvent screenResizedEvent) : ISceneService
{
    public int Priority => -10;

    private Id<RenderPass<IDefaultFrameFormat>> _clearPass;
    private Id<Framebuffer<IDefaultFrameFormat>> _framebufferId;

    public Id<Framebuffer<IDefaultFrameFormat>> FramebufferId => _framebufferId;

    public Result Load()
    {
        var gl = glProvider.Value;
        gl.Enable(EnableCap.Multisample);
        gl.Disable(EnableCap.CullFace);
        gl.Enable(EnableCap.DepthTest);
        gl.ClearColor(Color.CornflowerBlue);

        var window = windowProvider.Value;
        var width = window.FramebufferSize.X;
        var height = window.FramebufferSize.Y;

        if (framebufferManager.CreateWithDepth<IDefaultFrameFormat, RGBA>(width, height)
            .TryPickProblems(out var problems, out var fbResult))
        {
            return problems.Prepend("Failed to create off-screen framebuffer");
        }

        var (fb, colorTexture, _) = fbResult;
        _framebufferId = fb;

        screenPassManager.SetSource(fb, colorTexture, width, height);

        if (renderPassManager.Create(fb, priority: -100, ClearFlags.ColorDepth)
            .TryPickProblems(out problems, out var clearPass))
        {
            return problems.Prepend("Failed to create clear pass");
        }

        _clearPass = clearPass;

        screenResizedEvent.OnWindowResize.Subscribe(OnWindowResize);

        return Result.Success();
    }

    public Result Unload()
    {
        screenResizedEvent.OnWindowResize.Unsubscribe(OnWindowResize);

        renderPassManager.Destroy(_clearPass);
        framebufferManager.Delete(_framebufferId);

        return Result.Success();
    }

    private void OnWindowResize(Vector2D<int> size)
    {
        framebufferManager.Resize(_framebufferId, size.X, size.Y);
        screenPassManager.Resize(size.X, size.Y);
    }
}
