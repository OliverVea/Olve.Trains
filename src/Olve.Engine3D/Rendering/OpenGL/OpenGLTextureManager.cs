using System.Buffers;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;
using Silk.NET.OpenGL;
using Texture = Olve.Engine3D.Rendering.OpenGL.Types.Texture;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLTextureManager : IOpenGLEntityManager<TextureData, Texture>
{
    private const int MaxStackAllocationSize = 2048;

    public Result<Texture> Register(TextureData textureData)
    {
        if (textureData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var textureLength = textureData.Pixels.Length * 3;

        // Create texture
        var texture = GameManager.GL.GenTexture();
        GameManager.GL.BindTexture(TextureTarget.Texture2D, texture);

        // Bind texture data
        var array3 = textureLength > MaxStackAllocationSize ? ArrayPool<byte>.Shared.Rent(textureLength) : null;
        var pixelData = textureLength > MaxStackAllocationSize ? array3![..textureLength] : stackalloc byte[textureData.Pixels.Length * 3];
        BufferHelper.CopyTo(textureData.Pixels, pixelData);
        GameManager.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgb, (uint)textureData.Width, (uint)textureData.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, (ReadOnlySpan<byte>)pixelData);
        GameManager.GL.GenerateMipmap(TextureTarget.Texture2D);
        GameManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GameManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GameManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        GameManager.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        return new Texture(texture);
    }

    public Result Unregister(Texture registration)
    {
        GameManager.GL.DeleteTexture(registration.Handle);

        return Result.Success();
    }
}