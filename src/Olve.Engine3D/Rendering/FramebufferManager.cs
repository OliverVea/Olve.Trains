using System.Runtime.CompilerServices;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class FramebufferManager(Provider<GL> glProvider)
{
    private sealed class FramebufferEntry
    {
        public uint FboHandle { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<AttachmentEntry> ColorAttachments { get; } = new();
        public AttachmentEntry? DepthAttachment { get; set; }
    }

    private sealed class AttachmentEntry
    {
        public UntypedTextureId TextureId { get; init; } = null!;
        public uint GlHandle { get; set; }
        public InternalFormat InternalFormat { get; init; }
        public PixelFormat PixelFormat { get; init; }
        public PixelType PixelType { get; init; }
    }

    private readonly Dictionary<Id, FramebufferEntry> _framebuffers = new();
    private readonly Dictionary<Id, uint> _textureHandles = new();

    public Id<Framebuffer<TFormat>> DefaultFramebufferId<TFormat>()
        where TFormat : IFrameFormat
        => new(default);

    public Result Bind<TFormat>(Id<Framebuffer<TFormat>> framebufferId)
        where TFormat : IFrameFormat
    {
        if (framebufferId.Value == default)
        {
            glProvider.Value.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return Result.Success();
        }

        if (!_framebuffers.TryGetValue(framebufferId.Value, out var entry))
        {
            return new ResultProblem("Framebuffer '{0}' not found", framebufferId);
        }

        glProvider.Value.BindFramebuffer(FramebufferTarget.Framebuffer, entry.FboHandle);
        return Result.Success();
    }

    public Result Bind(Id framebufferId)
    {
        if (framebufferId == default)
        {
            glProvider.Value.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return Result.Success();
        }

        if (!_framebuffers.TryGetValue(framebufferId, out var entry))
        {
            return new ResultProblem("Framebuffer '{0}' not found", framebufferId);
        }

        glProvider.Value.BindFramebuffer(FramebufferTarget.Framebuffer, entry.FboHandle);
        return Result.Success();
    }

    public Result BindDefault()
    {
        glProvider.Value.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return Result.Success();
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat>(int width, int height)
        where TFormat : IFrameFormat
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var depthId = TextureId<Depth>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.DepthAttachment = MakeDepthAttachment(depthId);

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create depth-only framebuffer");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, depthId);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0)>
        Create<TFormat, T0>(int width, int height)
        where TFormat : IFrameFormat<T0>
        where T0 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0>(int width, int height)
        where TFormat : IFrameFormat<T0>
        where T0 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();
        var depthId = TextureId<Depth>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));
        entry.DepthAttachment = MakeDepthAttachment(depthId);

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer with depth");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id, depthId);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1)>
        Create<TFormat, T0, T1>(int width, int height)
        where TFormat : IFrameFormat<T0, T1>
        where T0 : unmanaged
        where T1 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();
        var color1Id = TextureId<T1>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T1>(color1Id));

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id, color1Id);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0, T1>(int width, int height)
        where TFormat : IFrameFormat<T0, T1>
        where T0 : unmanaged
        where T1 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();
        var color1Id = TextureId<T1>.New();
        var depthId = TextureId<Depth>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T1>(color1Id));
        entry.DepthAttachment = MakeDepthAttachment(depthId);

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer with depth");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id, color1Id, depthId);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<T2> Color2)>
        Create<TFormat, T0, T1, T2>(int width, int height)
        where TFormat : IFrameFormat<T0, T1, T2>
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();
        var color1Id = TextureId<T1>.New();
        var color2Id = TextureId<T2>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T1>(color1Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T2>(color2Id));

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id, color1Id, color2Id);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<T2> Color2, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0, T1, T2>(int width, int height)
        where TFormat : IFrameFormat<T0, T1, T2>
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0Id = TextureId<T0>.New();
        var color1Id = TextureId<T1>.New();
        var color2Id = TextureId<T2>.New();
        var depthId = TextureId<Depth>.New();

        var entry = new FramebufferEntry { Width = width, Height = height };
        entry.ColorAttachments.Add(MakeColorAttachment<T0>(color0Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T1>(color1Id));
        entry.ColorAttachments.Add(MakeColorAttachment<T2>(color2Id));
        entry.DepthAttachment = MakeDepthAttachment(depthId);

        if (AllocateFbo(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to create framebuffer with depth");
        }

        _framebuffers[fbId.Value] = entry;

        return (fbId, color0Id, color1Id, color2Id, depthId);
    }

    public Result Resize<TFormat>(Id<Framebuffer<TFormat>> framebufferId, int width, int height)
        where TFormat : IFrameFormat
    {
        if (!_framebuffers.TryGetValue(framebufferId.Value, out var entry))
        {
            return new ResultProblem("Framebuffer '{0}' not found for resize", framebufferId);
        }

        entry.Width = width;
        entry.Height = height;

        FreeAttachmentTextures(entry);

        if (AllocateAttachmentTextures(entry).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to reallocate textures for framebuffer '{0}'", framebufferId);
        }

        if (AttachTexturesToFbo(entry).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to reattach textures for framebuffer '{0}'", framebufferId);
        }

        return Result.Success();
    }

    public DeletionResult Delete<TFormat>(Id<Framebuffer<TFormat>> framebufferId)
        where TFormat : IFrameFormat
    {
        if (!_framebuffers.TryGetValue(framebufferId.Value, out var entry))
        {
            return DeletionResult.NotFound();
        }

        FreeAttachmentTextures(entry);
        glProvider.Value.DeleteFramebuffer(entry.FboHandle);

        _framebuffers.Remove(framebufferId.Value);

        return DeletionResult.Success();
    }

    public bool TryGetTextureHandle(UntypedTextureId textureId, out uint glHandle)
    {
        return _textureHandles.TryGetValue(textureId.Value, out glHandle);
    }

    public bool TryGetFboHandle(Id framebufferId, out uint fboHandle)
    {
        if (_framebuffers.TryGetValue(framebufferId, out var entry))
        {
            fboHandle = entry.FboHandle;
            return true;
        }

        fboHandle = 0;
        return false;
    }

    private Result AllocateFbo(FramebufferEntry entry)
    {
        entry.FboHandle = glProvider.Value.GenFramebuffer();

        if (AllocateAttachmentTextures(entry).TryPickProblems(out var problems))
        {
            glProvider.Value.DeleteFramebuffer(entry.FboHandle);
            return problems;
        }

        if (AttachTexturesToFbo(entry).TryPickProblems(out problems))
        {
            FreeAttachmentTextures(entry);
            glProvider.Value.DeleteFramebuffer(entry.FboHandle);
            return problems;
        }

        return Result.Success();
    }

    private Result AllocateAttachmentTextures(FramebufferEntry entry)
    {
        var gl = glProvider.Value;

        foreach (var attachment in entry.ColorAttachments)
        {
            var handle = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, handle);
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                attachment.InternalFormat,
                (uint)entry.Width,
                (uint)entry.Height,
                0,
                attachment.PixelFormat,
                attachment.PixelType,
                in Unsafe.NullRef<byte>());
            var clampToEdge = (int)GLEnum.ClampToEdge;
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, in clampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, in clampToEdge);
            var linear = (int)GLEnum.Linear;
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, in linear);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, in linear);
            gl.BindTexture(TextureTarget.Texture2D, 0);

            attachment.GlHandle = handle;
            _textureHandles[attachment.TextureId.Value] = handle;
        }

        if (entry.DepthAttachment is { } depthAttachment)
        {
            var handle = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, handle);
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                depthAttachment.InternalFormat,
                (uint)entry.Width,
                (uint)entry.Height,
                0,
                depthAttachment.PixelFormat,
                depthAttachment.PixelType,
                in Unsafe.NullRef<byte>());
            var clampToEdge = (int)GLEnum.ClampToEdge;
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, in clampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, in clampToEdge);
            var nearest = (int)GLEnum.Nearest;
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, in nearest);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, in nearest);
            gl.BindTexture(TextureTarget.Texture2D, 0);

            depthAttachment.GlHandle = handle;
            _textureHandles[depthAttachment.TextureId.Value] = handle;
        }

        return Result.Success();
    }

    private Result AttachTexturesToFbo(FramebufferEntry entry)
    {
        var gl = glProvider.Value;

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, entry.FboHandle);

        for (var i = 0; i < entry.ColorAttachments.Count; i++)
        {
            var attachment = entry.ColorAttachments[i];
            gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                (FramebufferAttachment)((int)FramebufferAttachment.ColorAttachment0 + i),
                TextureTarget.Texture2D,
                attachment.GlHandle,
                0);
        }

        if (entry.DepthAttachment is { } depthAttachment)
        {
            gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D,
                depthAttachment.GlHandle,
                0);
        }

        if (entry.ColorAttachments.Count > 0)
        {
            var drawBuffers = new DrawBufferMode[entry.ColorAttachments.Count];
            for (var i = 0; i < drawBuffers.Length; i++)
            {
                drawBuffers[i] = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
            }
            gl.DrawBuffers(drawBuffers);
        }
        else
        {
            gl.DrawBuffer(DrawBufferMode.None);
            gl.ReadBuffer(ReadBufferMode.None);
        }

        var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        if (status != GLEnum.FramebufferComplete)
        {
            return new ResultProblem("Framebuffer is not complete: {0}", status);
        }

        return Result.Success();
    }

    private void FreeAttachmentTextures(FramebufferEntry entry)
    {
        var gl = glProvider.Value;

        foreach (var attachment in entry.ColorAttachments)
        {
            gl.DeleteTexture(attachment.GlHandle);
            _textureHandles.Remove(attachment.TextureId.Value);
        }

        if (entry.DepthAttachment is { } depthAttachment)
        {
            gl.DeleteTexture(depthAttachment.GlHandle);
            _textureHandles.Remove(depthAttachment.TextureId.Value);
        }
    }

    private static AttachmentEntry MakeColorAttachment<T>(UntypedTextureId textureId) where T : unmanaged
    {
        var (internalFormat, pixelFormat, pixelType) = GetColorFormats<T>();
        return new AttachmentEntry
        {
            TextureId = textureId,
            InternalFormat = internalFormat,
            PixelFormat = pixelFormat,
            PixelType = pixelType,
        };
    }

    private static AttachmentEntry MakeDepthAttachment(UntypedTextureId textureId)
    {
        return new AttachmentEntry
        {
            TextureId = textureId,
            InternalFormat = InternalFormat.DepthComponent32f,
            PixelFormat = PixelFormat.DepthComponent,
            PixelType = PixelType.Float,
        };
    }

    // TODO: This is dumb we need to resolve based on T.
    // For example, put static abstract method on T and use that. Alternatively, use TMapper or some other second type.
    private static (InternalFormat, PixelFormat, PixelType) GetColorFormats<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(RGBA))
            return (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte);

        if (typeof(T) == typeof(float))
            return (InternalFormat.R32f, PixelFormat.Red, PixelType.Float);

        return (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte);
    }
}
