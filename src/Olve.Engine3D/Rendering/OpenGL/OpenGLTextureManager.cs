using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLTextureManager : IOpenGLEntityManager<TextureData, Texture2D>
{
    private const int BytesPerPixel = 4;

    public Result<Texture2D> Register(TextureData textureData)
    {
        if (textureData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var textureLength = textureData.Pixels.Length * BytesPerPixel;

        var texture = GameManager.GL.GenTexture();
        GameManager.GL.ActiveTexture(TextureUnit.Texture0);
        GameManager.GL.BindTexture(TextureTarget.Texture2D, texture);

        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);

        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);

        BufferHelper.UsingSpan<byte>(textureLength, pixelData =>
        {
            BufferHelper.CopyTo(textureData.Pixels, pixelData);
            GameManager.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)textureData.Width, (uint)textureData.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (ReadOnlySpan<byte>)pixelData);
        });

        GameManager.GL.GenerateMipmap(TextureTarget.Texture2D);

        GameManager.GL.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(texture, (uint)textureData.Width, (uint)textureData.Height);
    }

    public Result Unregister(Texture2D registration)
    {
        GameManager.GL.DeleteTexture(registration.Handle);

        return Result.Success();
    }
}