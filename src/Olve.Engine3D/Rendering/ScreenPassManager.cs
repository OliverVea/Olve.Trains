using Olve.Engine3D.Rendering.Textures;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class ScreenPassManager(FramebufferManager framebufferManager)
{
    public UntypedTextureId? Source { get; private set; }
    public Id? FramebufferId { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void SetSource<TFormat, TPixel>(
        Id<Framebuffer<TFormat>> framebufferId,
        TextureId<TPixel> colorTexture,
        int width,
        int height)
        where TFormat : IFrameFormat
    {
        Source = colorTexture;
        FramebufferId = framebufferId.Value;
        Width = width;
        Height = height;
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Blit(GL gl)
    {
        if (Source is null || FramebufferId is not { } fbId)
            return;

        if (!framebufferManager.TryGetFboHandle(fbId, out var readFbo))
            return;

        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFbo);
        gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

        gl.BlitFramebuffer(
            0, 0, Width, Height,
            0, 0, Width, Height,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);

        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
    }
}
