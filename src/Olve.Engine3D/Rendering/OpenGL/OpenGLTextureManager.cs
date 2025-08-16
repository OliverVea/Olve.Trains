using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;
using TextureData = Olve.Engine3D.Rendering.Entities.TextureData;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLTextureManager(Provider<GL> glProvider) : IOpenGLEntityManager<TextureData, Texture2D>
{
    private const int BytesPerPixel = 4;
    
    private static readonly int Nearest = (int)GLEnum.Nearest;
    private static readonly int Repeat = (int)GLEnum.Repeat;

    public Result<Texture2D> Register(TextureData textureData)
    {
        if (textureData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var textureLength = textureData.Pixels.Length * BytesPerPixel;

        var texture = glProvider.Value.GenTexture();
        glProvider.Value.ActiveTexture(TextureUnit.Texture0);
        glProvider.Value.BindTexture(TextureTarget.Texture2D, texture);

        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, in Nearest);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, in Nearest);

        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, in Repeat);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, in Repeat);

        BufferHelper.WithSpan<byte>(textureLength, pixelData =>
        {
            textureData.Pixels.CopyTo(pixelData);

            glProvider.Value.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)textureData.Width, (uint)textureData.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (ReadOnlySpan<byte>)pixelData);
        });

        glProvider.Value.GenerateMipmap(TextureTarget.Texture2D);

        glProvider.Value.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(texture, (uint)textureData.Width, (uint)textureData.Height);
    }

    public Result Unregister(Texture2D registration)
    {
        glProvider.Value.DeleteTexture(registration.Handle);

        return Result.Success();
    }
}