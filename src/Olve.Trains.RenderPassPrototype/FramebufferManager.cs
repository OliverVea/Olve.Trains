using Olve.Engine3D;
using Olve.Engine3D.Rendering.Textures;
using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Phantom type for framebuffer IDs. The TFormat parameter encodes the color attachment layout.
/// Used as <c>Id&lt;Framebuffer&lt;TFormat&gt;&gt;</c>.
/// </summary>
public sealed class Framebuffer<TFormat> where TFormat : IFrameFormat;

/// <summary>
/// Creates framebuffers with typed color + depth attachments.
/// Returns stable TextureId handles that survive resize.
/// </summary>
public class FramebufferManager
{
    private readonly Dictionary<Id, FramebufferEntry> _framebuffers = new();

    private record FramebufferEntry(int Width, int Height, List<UntypedTextureId> ColorAttachments, UntypedTextureId? DepthAttachment);

    // ── Validation ──

    private static Result ValidateDimensions(int width, int height)
    {
        Result[] results =
        [
            width > 0 ? Result.Success() : new ResultProblem("Width must be positive, got {0}", width),
            height > 0 ? Result.Success() : new ResultProblem("Height must be positive, got {0}", height),
        ];

        return results.TryPickProblems(out var problems)
            ? problems.Prepend("Invalid framebuffer dimensions")
            : Result.Success();
    }

    // ── 0 color attachments + depth (shadow maps, depth pre-pass) ──

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat>(int width, int height)
        where TFormat : IFrameFormat
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var depth = TextureId<Depth>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [], depth);

        return (fbId, depth);
    }

    // ── 1 color attachment ──

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0)>
        Create<TFormat, T0>(int width, int height)
        where TFormat : IFrameFormat<T0>
        where T0 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0], null);

        return (fbId, color0);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0>(int width, int height)
        where TFormat : IFrameFormat<T0>
        where T0 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();
        var depth = TextureId<Depth>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0], depth);

        return (fbId, color0, depth);
    }

    // ── 2 color attachments ──

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1)>
        Create<TFormat, T0, T1>(int width, int height)
        where TFormat : IFrameFormat<T0, T1>
        where T0 : unmanaged
        where T1 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();
        var color1 = TextureId<T1>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0, color1], null);

        return (fbId, color0, color1);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0, T1>(int width, int height)
        where TFormat : IFrameFormat<T0, T1>
        where T0 : unmanaged
        where T1 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();
        var color1 = TextureId<T1>.New();
        var depth = TextureId<Depth>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0, color1], depth);

        return (fbId, color0, color1, depth);
    }

    // ── 3 color attachments ──

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<T2> Color2)>
        Create<TFormat, T0, T1, T2>(int width, int height)
        where TFormat : IFrameFormat<T0, T1, T2>
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();
        var color1 = TextureId<T1>.New();
        var color2 = TextureId<T2>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0, color1, color2], null);

        return (fbId, color0, color1, color2);
    }

    public Result<(Id<Framebuffer<TFormat>> Fb, TextureId<T0> Color0, TextureId<T1> Color1, TextureId<T2> Color2, TextureId<Depth> Depth)>
        CreateWithDepth<TFormat, T0, T1, T2>(int width, int height)
        where TFormat : IFrameFormat<T0, T1, T2>
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        var fbId = Id.New<Framebuffer<TFormat>>();
        var color0 = TextureId<T0>.New();
        var color1 = TextureId<T1>.New();
        var color2 = TextureId<T2>.New();
        var depth = TextureId<Depth>.New();

        _framebuffers[fbId.Value] = new FramebufferEntry(width, height, [color0, color1, color2], depth);

        return (fbId, color0, color1, color2, depth);
    }

    // ── Resize ──

    /// <summary>
    /// Recreates backing GPU textures at the new size.
    /// TextureId handles remain stable — shaders referencing them pick up the new size automatically.
    /// </summary>
    public Result Resize<TFormat>(Id<Framebuffer<TFormat>> fb, int width, int height)
        where TFormat : IFrameFormat
    {
        if (ValidateDimensions(width, height).TryPickProblems(out var problems))
            return problems;

        if (!_framebuffers.TryGetValue(fb.Value, out var entry))
            return new ResultProblem("Framebuffer '{0}' not found", fb);

        _framebuffers[fb.Value] = entry with { Width = width, Height = height };
        return Result.Success();
    }

    // ── Destroy ──

    public DeletionResult Destroy<TFormat>(Id<Framebuffer<TFormat>> fb)
        where TFormat : IFrameFormat
    {
        return _framebuffers.Remove(fb.Value)
            ? DeletionResult.Success()
            : DeletionResult.NotFound();
    }

    // ── Query ──

    public bool TryGetSize<TFormat>(Id<Framebuffer<TFormat>> fb, out int width, out int height)
        where TFormat : IFrameFormat
    {
        if (_framebuffers.TryGetValue(fb.Value, out var entry))
        {
            width = entry.Width;
            height = entry.Height;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    public bool Exists<TFormat>(Id<Framebuffer<TFormat>> fb)
        where TFormat : IFrameFormat
        => _framebuffers.ContainsKey(fb.Value);
}
