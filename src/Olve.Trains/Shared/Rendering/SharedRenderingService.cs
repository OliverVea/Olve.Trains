using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Utilities.Ids;

namespace Olve.Trains.Shared.Rendering;

public class SharedRenderingService(
    FramebufferManager framebufferManager,
    RenderPassManager renderPassManager,
    ScreenPass screenPass) : ISceneService
{
    public int Priority => -5;

    public Id<RenderPass<IDefaultFrameFormat>> MainPass { get; private set; }
    public Id<RenderPass<IDefaultFrameFormat>> GuiPass { get; private set; }

    public Result Load()
    {
        var fb = screenPass.FramebufferId is { } offscreenId
            ? new Id<Framebuffer<IDefaultFrameFormat>>(offscreenId)
            : framebufferManager.DefaultFramebufferId<IDefaultFrameFormat>();

        if (renderPassManager.Create(fb, priority: 0, ClearFlags.None)
            .TryPickProblems(out var problems, out var mainPass))
        {
            return problems.Prepend("Failed to create main render pass");
        }

        if (renderPassManager.Create(fb, priority: 100, ClearFlags.None)
            .TryPickProblems(out problems, out var guiPass))
        {
            return problems.Prepend("Failed to create GUI render pass");
        }

        MainPass = mainPass;
        GuiPass = guiPass;

        return Result.Success();
    }

    public Result Unload()
    {
        renderPassManager.Destroy(MainPass);
        renderPassManager.Destroy(GuiPass);

        return Result.Success();
    }
}
