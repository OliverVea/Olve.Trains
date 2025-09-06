using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLHeightmapManager(Provider<GL> glProvider) : IOpenGLEntityManager<HeightmapData, OpenGLHeightmapManager.Registration>
{
    private const int PositionFields = 2;
    private const int VertexFields = PositionFields;

    private const int PositionSize = PositionFields * sizeof(float);
    private const int VertexSize = PositionSize;
    
    private static readonly int ClampToEdge = (int)GLEnum.ClampToEdge;
    private static readonly int Nearest = (int)GLEnum.Nearest;

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

    private VAO BindVAO()
    {
        var vao = glProvider.Value.CreateVertexArray();
        glProvider.Value.BindVertexArray(vao);

        return new VAO(vao);
    }

    private VBO BindVBO(HeightmapData heightmapData)
    {
        var vbo = glProvider.Value.CreateBuffer();
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        var vertexCount = heightmapData.Width * heightmapData.Length;

        BufferHelper.WithSpan<float>(vertexCount * VertexFields, vertices =>
        {
            for (var i = 0; i < vertexCount; i++)
            {
                var x = i % heightmapData.Width;
                var z = i / heightmapData.Width;
                vertices[i * VertexFields + 0] = x;
                vertices[i * VertexFields + 1] = z;
            }
            glProvider.Value.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        });


        return new VBO(vbo, (uint)vertexCount);
    }

    private EBO BindEBO(HeightmapData heightmapData)
    {
        var ebo = glProvider.Value.CreateBuffer();
        glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        var quadsX = heightmapData.Width  - 1;
        var quadsZ = heightmapData.Length - 1;
        var indexCount = quadsX * quadsZ * 6;

        BufferHelper.WithSpan<uint>(indexCount, indices =>
        {
            var k = 0; // write cursor
            for (var z = 0; z < quadsZ; z++)
            {
                for (var x = 0; x < quadsX; x++)
                {
                    var a = z * heightmapData.Width + x;
                    var b = a + 1;
                    var c = a + heightmapData.Width;
                    var d = c + 1;

                    // Pick a diagonal deterministically; using height average is common:
                    var diagAd = (heightmapData.Heights[a] + heightmapData.Heights[d])
                                 <= (heightmapData.Heights[b] + heightmapData.Heights[c]);

                    if (diagAd)
                    {
                        indices[k++] = (uint)a; indices[k++] = (uint)b; indices[k++] = (uint)d;
                        indices[k++] = (uint)d; indices[k++] = (uint)c; indices[k++] = (uint)a;
                    }
                    else
                    {
                        indices[k++] = (uint)c; indices[k++] = (uint)a; indices[k++] = (uint)b;
                        indices[k++] = (uint)b; indices[k++] = (uint)d; indices[k++] = (uint)c;
                    }
                }
            }
            glProvider.Value.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        });


        return new EBO(ebo, (uint)indexCount);
    }

    private Texture2D BindTexture(HeightmapData heightmapData)
    {
        var textureLength = heightmapData.Heights.Length;

        var texture = glProvider.Value.GenTexture();
        glProvider.Value.ActiveTexture(TextureUnit.Texture0);
        glProvider.Value.BindTexture(TextureTarget.Texture2D, texture);

        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, in Nearest);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, in Nearest);

        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, in ClampToEdge);
        glProvider.Value.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, in ClampToEdge);

        BufferHelper.WithSpan<float>(textureLength, pixelData =>
        {
            for (var i = 0; i < textureLength; i++)
            {
                pixelData[i] = heightmapData.Heights[i] * heightmapData.Step;
            }

            glProvider.Value.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)heightmapData.Width,
                (uint)heightmapData.Length, 0, PixelFormat.Red, PixelType.Float, (ReadOnlySpan<float>)pixelData);
        });

        glProvider.Value.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(texture, (uint)heightmapData.Width, (uint)heightmapData.Length);
    }

    private void SetVertexAttributes()
    {
        glProvider.Value.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize,
            IntPtr.Zero);
        glProvider.Value.EnableVertexAttribArray(0);
    }

    private void Cleanup()
    {
        glProvider.Value.BindVertexArray(0); // Unbind VAO
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind VBO
        glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0); // Unbind EBO
    }

    public Result Unregister(Registration registration)
    {
        glProvider.Value.DeleteVertexArray(registration.VAO.Handle);
        glProvider.Value.DeleteBuffer(registration.VBO.Handle);
        glProvider.Value.DeleteBuffer(registration.EBO.Handle);
        glProvider.Value.DeleteTexture(registration.Texture2D.Handle);

        return Result.Success();
    }
}