using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;
using static Olve.Engine3D.GameManager;

namespace Olve.Engine3D.Graphics;

public class GameObjectRenderer
{
    private readonly List<Entity> _gameObjects = new();

    public void Deregister(Entity entity)
    {
        _gameObjects.Remove(entity);
    }

    public void Register(Entity entity)
    {
        _gameObjects.Add(entity);
    }

    public void Render(Matrix4X4<float> view, Matrix4X4<float> projection)
    {
        foreach (var gameObject in _gameObjects)
        {
            if (!gameObject.Enabled || !gameObject.Visible)
            {
                continue;
            }

            var world = gameObject.Transform.World;

            foreach (var mesh in gameObject.Meshes)
            {
                SetUniforms(mesh.Material.Shader.ShaderProgram, world, view, projection);

                Gl.BindVertexArray(mesh.Geometry.VAO.Handle);
                Gl.DrawElements(PrimitiveType.Triangles, mesh.Geometry.IndicesCount, DrawElementsType.UnsignedInt, in Unsafe.NullRef<uint>());
            }
        }
    }

    private static void SetUniforms(ShaderProgram shaderProgram, Matrix4X4<float> world, Matrix4X4<float> view, Matrix4X4<float> projection)
    {
        Span<float> buffer = stackalloc float[16];
        Gl.UseProgram(shaderProgram.Handle);

        CopyTo(world, buffer);
        var worldLocation = Gl.GetUniformLocation(shaderProgram.Handle, "world");
        Gl.UniformMatrix4(worldLocation, 1, false, buffer);

        CopyTo(view, buffer);
        var viewLocation = Gl.GetUniformLocation(shaderProgram.Handle, "view");
        Gl.UniformMatrix4(viewLocation, 1, false, buffer);

        CopyTo(projection, buffer);
        var projectionLocation = Gl.GetUniformLocation(shaderProgram.Handle, "projection");
        Gl.UniformMatrix4(projectionLocation, 1, false, buffer);
    }

    private static void CopyTo(Matrix4X4<float> matrix, Span<float> buffer)
    {
        buffer[0] = matrix.M11;
        buffer[1] = matrix.M12;
        buffer[2] = matrix.M13;
        buffer[3] = matrix.M14;
        buffer[4] = matrix.M21;
        buffer[5] = matrix.M22;
        buffer[6] = matrix.M23;
        buffer[7] = matrix.M24;
        buffer[8] = matrix.M31;
        buffer[9] = matrix.M32;
        buffer[10] = matrix.M33;
        buffer[11] = matrix.M34;
        buffer[12] = matrix.M41;
        buffer[13] = matrix.M42;
        buffer[14] = matrix.M43;
        buffer[15] = matrix.M44;
    }
}