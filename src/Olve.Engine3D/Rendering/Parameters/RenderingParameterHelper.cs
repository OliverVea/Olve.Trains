using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Parameters;

public static class RenderingParameterHelper
{
    public static Result SetUniforms(this AnyRenderingParameter renderingParameter, GL gl, ShaderProgram shaderProgram)
    {
        // TODO: cache location
        var location = gl.GetUniformLocation(shaderProgram.Handle, renderingParameter.Name);
        if (location == -1)
        {
            return new ResultProblem("Could not find location for rendering parameter '{0}'", renderingParameter.Name);
        }

        // TODO: investigate potential performance issues - boxing?
        return renderingParameter.Match(
            x => SetMatrix3X3(x, gl, location),
            x => SetMatrix4X4(x, gl, location),
            x => SetVector2D(x, gl, location),
            x => SetVector3D(x, gl, location),
            x => SetFloat(x, gl, location),
            x => SetBool(x, gl, location),
            x => SetTexture(x, gl, location));
    }

    private static Result SetBool(RenderingParameter.Bool b, GL gl, int location)
    {
        gl.Uniform1(location, b.Value ? 1 : 0);
        return Result.Success();
    }

    private static Result SetMatrix3X3(RenderingParameter.Matrix3X3 matrix, GL gl, int location)
    {
        Span<float> buffer = stackalloc float[9];
        matrix.Value.CopyTo(buffer);
        gl.UniformMatrix3(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result SetMatrix4X4(RenderingParameter.Matrix4X4 matrix, GL gl, int location)
    {
        Span<float> buffer = stackalloc float[16];
        matrix.Value.CopyTo(buffer);
        gl.UniformMatrix4(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result SetVector2D(RenderingParameter.Vector2D vector, GL gl, int location)
    {
        gl.Uniform2(location, vector.Value.X, vector.Value.Y);
        return Result.Success();
    }

    private static Result SetVector3D(RenderingParameter.Vector3D vector, GL gl, int location)
    {
        gl.Uniform3(location, vector.Value.X, vector.Value.Y, vector.Value.Z);
        return Result.Success();
    }

    private static Result SetFloat(RenderingParameter.Float f, GL gl, int location)
    {
        gl.Uniform1(location, f.Value);
        return Result.Success();
    }

    private static Result SetTexture(RenderingParameter.Texture texture, GL gl, int location)
    {
        int textureUnitIndex = 0; // Adjust based on usage
        gl.ActiveTexture(TextureUnit.Texture0 + textureUnitIndex);
        gl.BindTexture(TextureTarget.Texture2D, texture.Value.Handle);
        gl.Uniform1(location, textureUnitIndex);
        return Result.Success();
    }
}