using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Textures;

public record TextureUploadOptions(
    GLEnum Wrap = GLEnum.Repeat,
    GLEnum Filter = GLEnum.Nearest,
    bool GenerateMipmaps = false
);
