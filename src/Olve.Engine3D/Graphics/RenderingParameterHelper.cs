using Olve.Engine3D.Rendering.OpenGL.Types;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Graphics;

public static class RenderingParameterHelper
{
    public static Result ApplyRenderingParameter(this AnyRenderingParameter renderingParameter, ShaderProgram shaderProgram)
    {
        // TODO: cache location
        var location = GameManager.GL.GetUniformLocation(shaderProgram.Handle, renderingParameter.Name);
        if (location == -1)
        {
            return new ResultProblem("Could not find location for rendering parameter '{0}'", renderingParameter.Name);
        }

        // TODO: investigate potential performance issues - boxing?
        return renderingParameter.Match(
            x => ApplyMatrix4X4(x, location),
            x => ApplyVector3D(x, location),
            x => ApplyFloat(x, location));
    }

    private static Result ApplyMatrix4X4(RenderingParameter.Matrix4X4 matrix, int location)
    {
        Span<float> buffer = stackalloc float[16];
        BufferHelper.CopyTo(matrix.Value, buffer);
        GameManager.GL.UniformMatrix4(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result ApplyVector3D(RenderingParameter.Vector3D vector, int location)
    {
        GameManager.GL.Uniform3(location, vector.Value.X, vector.Value.Y, vector.Value.Z);
        return Result.Success();
    }

    private static Result ApplyFloat(RenderingParameter.Float f, int location)
    {
        GameManager.GL.Uniform1(location, f.Value);
        return Result.Success();
    }
}