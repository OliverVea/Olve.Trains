using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Parameters;

public static class RenderingParameterHelper
{
    public static Result SetUniforms(this AnyRenderingParameter renderingParameter, ShaderProgram shaderProgram)
    {
        // TODO: cache location
        var location = GameManager.GL.GetUniformLocation(shaderProgram.Handle, renderingParameter.Name);
        if (location == -1)
        {
            return new ResultProblem("Could not find location for rendering parameter '{0}'", renderingParameter.Name);
        }

        // TODO: investigate potential performance issues - boxing?
        return renderingParameter.Match(
            x => SetMatrix3X3(x, location),
            x => SetMatrix4X4(x, location),
            x => SetVector2D(x, location),
            x => SetVector3D(x, location),
            x => SetFloat(x, location),
            x => SetTexture(x, location));
    }

    private static Result SetMatrix3X3(RenderingParameter.Matrix3X3 matrix, int location)
    {
        Span<float> buffer = stackalloc float[9];
        matrix.Value.CopyTo(buffer);
        GameManager.GL.UniformMatrix3(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result SetMatrix4X4(RenderingParameter.Matrix4X4 matrix, int location)
    {
        Span<float> buffer = stackalloc float[16];
        matrix.Value.CopyTo(buffer);
        GameManager.GL.UniformMatrix4(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result SetVector2D(RenderingParameter.Vector2D vector, int location)
    {
        GameManager.GL.Uniform2(location, vector.Value.X, vector.Value.Y);
        return Result.Success();
    }

    private static Result SetVector3D(RenderingParameter.Vector3D vector, int location)
    {
        GameManager.GL.Uniform3(location, vector.Value.X, vector.Value.Y, vector.Value.Z);
        return Result.Success();
    }

    private static Result SetFloat(RenderingParameter.Float f, int location)
    {
        GameManager.GL.Uniform1(location, f.Value);
        return Result.Success();
    }

    private static Result SetTexture(RenderingParameter.Texture texture, int location)
    {
        int textureUnitIndex = 0; // Adjust based on usage
        GameManager.GL.ActiveTexture(TextureUnit.Texture0 + textureUnitIndex);
        GameManager.GL.BindTexture(TextureTarget.Texture2D, texture.Value.Handle);
        GameManager.GL.Uniform1(location, textureUnitIndex);
        return Result.Success();
    }
}