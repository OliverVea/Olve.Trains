using Olve.Engine3D;

namespace Olve.Trains.RenderPassPrototype;

// ── Engine-level: frame format hierarchy (0–3 color attachments) ──

/// <summary>
/// Base frame format — no color attachments (depth-only passes).
/// </summary>
public interface IFrameFormat;

/// <summary>
/// Frame format with 1 color attachment.
/// </summary>
public interface IFrameFormat<T0> : IFrameFormat where T0 : unmanaged;

/// <summary>
/// Frame format with 2 color attachments.
/// </summary>
public interface IFrameFormat<T0, T1> : IFrameFormat where T0 : unmanaged where T1 : unmanaged;

/// <summary>
/// Frame format with 3 color attachments.
/// </summary>
public interface IFrameFormat<T0, T1, T2> : IFrameFormat where T0 : unmanaged where T1 : unmanaged where T2 : unmanaged;

// ── Game-defined formats ──

/// <summary>
/// Standard single-RGBA-output framebuffer format. Used by most shaders.
/// </summary>
public interface IDefaultFrameFormat : IFrameFormat<RGBA>;

/// <summary>
/// Depth-only framebuffer format. Used for shadow map passes.
/// </summary>
public interface IDepthFrameFormat : IFrameFormat;
