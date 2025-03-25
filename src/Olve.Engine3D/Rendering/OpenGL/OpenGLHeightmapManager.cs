using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLHeightmapManager : IOpenGLEntityManager<HeightmapData, OpenGLHeightmapManager.Registration>
{
    private const int PositionFields = 2;
    private const int VertexFields = PositionFields;

    private const int PositionSize = PositionFields * sizeof(float);
    private const int VertexSize = PositionSize;

    public readonly record struct Registration(VAO VAO, VBO VBO, EBO EBO, Texture2D Texture2D);

    public Result<Registration> Register(HeightmapData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var vao = BindVAO();
        var vbo = BindVBO(entityData);
        var ebo = BindEBO(entityData);
        var texture2D = BindTexture(entityData);

        SetVertexAttributes();
        Cleanup();

        return new Registration(vao, vbo, ebo, texture2D);
    }

    private static VAO BindVAO()
    {
        var vao = GameManager.GL.CreateVertexArray();
        GameManager.GL.BindVertexArray(vao);

        return new VAO(vao);
    }

    private static VBO BindVBO(HeightmapData heightmapData)
    {
        var vbo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        /*  Each pixel in the heightmap is a tile - ie. fence post problem
            +---+---+---+
            | 1 | 1 | 2 |
            +---+---+---+
            | 2 | 2 | 2 |
            +---+---+---+
         */
        var vertexCount = (heightmapData.Width + 1) * (heightmapData.Length + 1);

        BufferHelper.UsingSpan<float>(vertexCount * VertexFields, vertices =>
        {
            for (var i = 0; i < heightmapData.Heights.Length; i++)
            {
                int x = i % heightmapData.Width, z = i / heightmapData.Width;

                vertices[i * VertexFields] = x;
                vertices[i * VertexFields + 1] = z;
            }

            GameManager.GL.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        });

        return new VBO(vbo, (uint)vertexCount);
    }

    private static EBO BindEBO(HeightmapData heightmapData)
    {
        var ebo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        var indexCount = heightmapData.Width * heightmapData.Length * 2;

        BufferHelper.UsingSpan<uint>(indexCount, indices =>
        {
            for (var i = 0; i < heightmapData.Length - 1; i++)
            {
                for (var j = 0; j < heightmapData.Width - 1; j++)
                {
                    var index = i * heightmapData.Width + j;
                    indices[index * 6] = (uint)index;
                    indices[index * 6 + 1] = (uint)(index + 1);
                    indices[index * 6 + 2] = (uint)(index + heightmapData.Width);
                    indices[index * 6 + 3] = (uint)(index + 1);
                    indices[index * 6 + 4] = (uint)(index + heightmapData.Width + 1);
                    indices[index * 6 + 5] = (uint)(index + heightmapData.Width);
                }
            }

            GameManager.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        });

        return new EBO(ebo, (uint)indexCount);
    }

    private static Texture2D BindTexture(HeightmapData heightmapData)
    {
        var textureLength = heightmapData.Heights.Length * 4;

        var texture = GameManager.GL.GenTexture();
        GameManager.GL.ActiveTexture(TextureUnit.Texture0);
        GameManager.GL.BindTexture(TextureTarget.Texture2D, texture);

        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);

        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        GameManager.GL.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);

        BufferHelper.UsingSpan<float>(textureLength, pixelData =>
        {
            heightmapData.Heights.CopyTo(pixelData);

            GameManager.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)heightmapData.Width, (uint)heightmapData.Length, 0, PixelFormat.Red, PixelType.Float, (ReadOnlySpan<float>)pixelData);
        });

        GameManager.GL.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(texture, (uint)heightmapData.Width, (uint)heightmapData.Length);
    }

    private static void SetVertexAttributes()
    {
        GameManager.GL.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero);
        GameManager.GL.EnableVertexAttribArray(0);
    }

    private static void Cleanup()
    {
        GameManager.GL.BindVertexArray(0); // Unbind VAO
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind VBO
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0); // Unbind EBO

    }

    public Result Unregister(Registration registration)
    {
        GameManager.GL.DeleteVertexArray(registration.VAO.Handle);
        GameManager.GL.DeleteBuffer(registration.VBO.Handle);
        GameManager.GL.DeleteBuffer(registration.EBO.Handle);
        GameManager.GL.DeleteTexture(registration.Texture2D.Handle);

        return Result.Success();
    }
}