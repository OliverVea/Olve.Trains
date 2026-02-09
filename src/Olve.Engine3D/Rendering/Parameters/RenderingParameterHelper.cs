using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Parameters;

public static class RenderingParameterHelper
{
    private static readonly Dictionary<(uint, string), int> UniformLocationCache = new();

    public static Result SetUniforms(this AnyRenderingParameter renderingParameter, GL gl, ShaderProgram shaderProgram, TextureSlotManager? textureSlotManager = null)
    {
        if (!UniformLocationCache.TryGetValue((shaderProgram.Handle, renderingParameter.Name), out var location))
        {
            location = gl.GetUniformLocation(shaderProgram.Handle, renderingParameter.Name);
            if (location == -1)
            {
                return new ResultProblem("Could not find location for rendering parameter '{0}'", renderingParameter.Name);
            }

            UniformLocationCache.Add((shaderProgram.Handle, renderingParameter.Name), location);
        }

        if (!ApplyUniform(renderingParameter, gl, location, textureSlotManager))
        {
            return new ResultProblem("Could not apply uniform '{0}' of type '{1}' with value: {2}", renderingParameter.Name, renderingParameter.Value.GetType().Name, renderingParameter.Value);
        }

        return Result.Success();
    }
    private static bool ApplyUniform(in AnyRenderingParameter renderingParameter, GL gl, int location, TextureSlotManager? textureSlotManager)
    {
        if (renderingParameter.IsT0)
        {
            SetMatrix3X3(renderingParameter.AsT0, gl, location);
            return true;
        }

        if (renderingParameter.IsT1)
        {
            SetMatrix4X4(renderingParameter.AsT1, gl, location);
            return true;
        }

        if (renderingParameter.IsT2)
        {
            SetVector2D(renderingParameter.AsT2, gl, location);
            return true;
        }

        if (renderingParameter.IsT3)
        {
            SetVector3D(renderingParameter.AsT3, gl, location);
            return true;
        }

        if (renderingParameter.IsT4)
        {
            SetVector4D(renderingParameter.AsT4, gl, location);
            return true;
        }

        if (renderingParameter.IsT5)
        {
            SetFloat(renderingParameter.AsT5, gl, location);
            return true;
        }

        if (renderingParameter.IsT6)
        {
            SetBool(renderingParameter.AsT6, gl, location);
            return true;
        }

        if (renderingParameter.IsT7)
        {
            return SetTexture(renderingParameter.AsT7, gl, location, textureSlotManager);
        }

        return false;
    }
    private static void SetBool(RenderingParameter.Bool b, GL gl, int location)
    {
        gl.Uniform1(location, b.Value ? 1 : 0);
    }

    private static void SetMatrix3X3(RenderingParameter.Matrix3X3 matrix, GL gl, int location)
    {
        Span<float> buffer = stackalloc float[9];
        matrix.Value.CopyTo(buffer);
        gl.UniformMatrix3(location, 1, false, buffer);
    }

    private static void SetMatrix4X4(RenderingParameter.Matrix4X4 matrix, GL gl, int location)
    {
        Span<float> buffer = stackalloc float[16];
        matrix.Value.CopyTo(buffer);
        gl.UniformMatrix4(location, 1, false, buffer);
    }

    private static void SetVector2D(RenderingParameter.Vector2D vector, GL gl, int location)
    {
        gl.Uniform2(location, vector.Value.X, vector.Value.Y);
    }

    private static void SetVector3D(RenderingParameter.Vector3D vector, GL gl, int location)
    {
        gl.Uniform3(location, vector.Value.X, vector.Value.Y, vector.Value.Z);
    }

    private static void SetVector4D(RenderingParameter.Vector4D vector, GL gl, int location)
    {
        gl.Uniform4(location, vector.Value.X, vector.Value.Y, vector.Value.Z, vector.Value.W);
    }

    private static void SetFloat(RenderingParameter.Float f, GL gl, int location)
    {
        gl.Uniform1(location, f.Value);
    }

    private static bool SetTexture(RenderingParameter.Texture texture, GL gl, int location, TextureSlotManager? textureSlotManager)
    {
        if (textureSlotManager == null)
        {
            return false;
        }

        if (textureSlotManager.BindTexture(texture.Value).TryPickProblems(out _, out var slot))
        {
            return false;
        }

        gl.Uniform1(location, slot.SlotIntex);
        return true;
    }
}