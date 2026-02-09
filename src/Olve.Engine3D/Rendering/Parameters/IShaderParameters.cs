using Olve.Engine3D.Rendering.Textures;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Parameters;

/// <summary>
/// Interface for per-entity shader parameters.
/// Implementations are generated per-shader and contain nullable overrides for all uniforms.
/// </summary>
public interface IShaderParameters
{
    /// <summary>
    /// Creates RenderingParameters from this entity's parameter values.
    /// Only non-null properties are included.
    /// </summary>
    RenderingParameters ToRenderingParameters();

    /// <summary>
    /// Gets the texture Ids that need to be bound before rendering.
    /// </summary>
    IReadOnlyList<UntypedTextureId> GetTextureIds();
}
