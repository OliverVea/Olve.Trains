using Olve.Engine3D.Rendering.Textures;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Blits a color texture to the default framebuffer (FBO 0).
/// Not a render pass — fixed-function final output.
/// The game wires this once during setup; the engine calls it after all render passes complete.
/// </summary>
public class ScreenPass
{
    private UntypedTextureId? _source;

    /// <summary>
    /// The color texture that will be blitted to the screen, or null if not yet wired.
    /// </summary>
    public UntypedTextureId? Source => _source;

    public void SetSource<TPixel>(TextureId<TPixel> colorTexture)
    {
        _source = colorTexture;
    }
}
