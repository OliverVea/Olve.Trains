using System.Runtime.CompilerServices;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager(
    Provider<GL> glProvider,
    OpenGLShaderManager openGLShaderManager,
    ShaderEntityManager shaderEntityManager,
    GeometryManager geometryManager,
    RenderingGroupManager groupManager)
{
    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    public Result RenderAll()
    {
        var gl = glProvider.Value;
        var sortedGroups = groupManager.SortedGroups;

        foreach (var group in sortedGroups)
        {
            if (group.IsDirty)
            {
                RebuildInstanceBuffer(gl, group);
                group.ClearDirty();
            }

            var instanceCount = group.Instances.Count;
            if (instanceCount == 0) continue;

            var shader = group.Shader;
            if (shader.RenderingId == default) continue;

            if (shaderEntityManager.GetRegistration(shader.RenderingId)
                .TryPickProblems(out var problems, out var shaderRegistration))
            {
                return problems.Prepend("Failed to get shader registration for '{0}'", shader.ShaderData.Name);
            }

            ApplyRenderState(gl, group.RenderState);

            var shaderParameters = shader.MakeParameters();
            if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, shaderParameters)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to load shader '{0}'", shader.ShaderData.Name);
            }

            if (group.GroupParameters is { } groupParams)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, groupParams.ToRenderingParameters())
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to apply group parameters for shader '{0}'", shader.ShaderData.Name);
                }
            }

            try
            {
                gl.BindVertexArray(group.VAO.Handle);

                if (!geometryManager.TryGet(group.GeometryId, out var geoData))
                {
                    return new ResultProblem("Geometry '{0}' not found for group", group.GeometryId);
                }

                if (geoData.MeshEBO is { } ebo)
                {
                    gl.DrawElementsInstanced(
                        group.PrimitiveType,
                        ebo.IndexCount,
                        DrawElementsType.UnsignedInt,
                        in Unsafe.NullRef<int>(),
                        (uint)instanceCount);
                }
                else
                {
                    gl.DrawArraysInstanced(
                        group.PrimitiveType,
                        0,
                        geoData.VertexCount,
                        (uint)instanceCount);
                }

                gl.BindVertexArray(0);
            }
            catch (Exception e)
            {
                return new ResultProblem(e, "Failed to render group");
            }

            if (group.GroupParameters is not null)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, shaderParameters)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to restore shader parameters for '{0}'", shader.ShaderData.Name);
                }
            }
        }

        gl.DepthMask(true);
        gl.Disable(GLEnum.Blend);

        _errorCounter++;
        if (_errorCounter >= ErrorCounterThreshold)
        {
            var error = gl.GetError();
            if (error != GLEnum.NoError)
            {
                return new ResultProblem("OpenGL error: {0}", error);
            }

            _errorCounter = 0;
        }

        return Result.Success();
    }

    private static void ApplyRenderState(GL gl, RenderState state)
    {
        switch (state.Blend)
        {
            case BlendMode.None:
                gl.Disable(GLEnum.Blend);
                break;
            case BlendMode.Alpha:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Premultiplied:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                break;
        }

        gl.DepthMask(state.DepthWrite);

        if (state.DepthTest)
            gl.Enable(GLEnum.DepthTest);
        else
            gl.Disable(GLEnum.DepthTest);
    }

    private static void RebuildInstanceBuffer(GL gl, GroupData group)
    {
        var floats = group.Instances.MarshalToFloats();

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, group.InstanceVBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, floats, BufferUsageARB.DynamicDraw);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
}
