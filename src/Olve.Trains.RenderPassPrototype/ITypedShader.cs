using Olve.Engine3D.Rendering.Shaders;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Shader parameterized by frame format. Extends the existing IShader with
/// a TFormat type parameter that links it to compatible render passes.
/// The compiler enforces that a shader can only be registered into a pass with a matching format.
/// </summary>
public interface IShader<TFormat> : IShader where TFormat : IFrameFormat;
