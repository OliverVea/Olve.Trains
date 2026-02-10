using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLTextureManager(Provider<GL> glProvider)
{
    public Result<Texture2D> Register<T, TPixelFormat>(TextureData<T> textureData, TextureUploadOptions options)
        where T : unmanaged
        where TPixelFormat : IPixelFormat<T>
    {
        var textureLength = textureData.Pixels.Length * TPixelFormat.BytesPerPixel;

        var texture = glProvider.Value.GenTexture();
        glProvider.Value.ActiveTexture(TextureUnit.Texture0);
        glProvider.Value.BindTexture(TextureTarget.Texture2D, texture);

        var filter = (int)options.Filter;
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, in filter);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, in filter);

        var wrap = (int)options.Wrap;
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, in wrap);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, in wrap);

        BufferHelper.WithSpan<byte>(textureLength, pixelData =>
        {
            TPixelFormat.WriteBytes(textureData.Pixels, pixelData);

            glProvider.Value.TexImage2D(
                TextureTarget.Texture2D,
                0,
                TPixelFormat.InternalFormat,
                (uint)textureData.Width,
                (uint)textureData.Height,
                0,
                TPixelFormat.PixelFormat,
                TPixelFormat.PixelType,
                pixelData);
        });

        if (options.GenerateMipmaps)
        {
            glProvider.Value.GenerateMipmap(TextureTarget.Texture2D);
        }

        glProvider.Value.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(texture, (uint)textureData.Width, (uint)textureData.Height);
    }

    public Result Unregister(Texture2D registration)
    {
        glProvider.Value.DeleteTexture(registration.Handle);
        return Result.Success();
    }
}
