using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Rendering;

public class TrackRenderingService(ILoggingManager loggingManager,
    Provider<GL> glProvider,
    LineStripEntityManager lineStripEntityManager,
    CameraSceneService cameraSceneService,
    ShaderEntityManager shaderEntityManager,
    TerrainRenderingService terrainRenderingService,
    OpenGLShaderManager openGLShaderManager) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([terrainRenderingService]);

    private readonly record struct TrackEntry(
        RenderingId<LineStripData> RenderingId,
        Shaders.LineStrip.EntityParameters? ShaderParameters);

    private readonly Dictionary<Id<Track>, TrackEntry> _trackInstanceIds = new();
    private readonly Shaders.LineStrip _shader = new()
    {
        UOpacity = 1.0f,
        UColorMix = 0.0f,
        BlendState = RenderState.AlphaBlend,
    };

    private RenderingId<ShaderData>? _shaderId;

    protected override Result OnLoad()
    {
        if (shaderEntityManager.Register(_shader.ShaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        _shaderId = shaderId;

        return Result.Success();
    }

    public Result<RenderingId<LineStripData>> Register(
        Id<Track> trackId,
        LineStripData data,
        Shaders.LineStrip.EntityParameters? shaderParameters = null)
    {
        if (lineStripEntityManager.Register(data).TryPickProblems(out var problems, out var renderingId))
        {
            return problems.Prepend("Failed to register line strip");
        }

        _trackInstanceIds[trackId] = new TrackEntry(renderingId, shaderParameters);
        return renderingId;
    }

    public Result Update(
        Id<Track> trackId,
        LineStripData? data = null,
        Shaders.LineStrip.EntityParameters? shaderParameters = null)
    {
        if (!_trackInstanceIds.TryGetValue(trackId, out var entry))
        {
            return new ResultProblem("Track '{0}' is not registered", trackId);
        }

        if (data != null)
        {
            if (lineStripEntityManager.Update(entry.RenderingId, data).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to update line strip data");
            }
        }

        _trackInstanceIds[trackId] = entry with { ShaderParameters = shaderParameters };
        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_trackInstanceIds.Remove(trackId, out var entry))
        {
            return Result.Success();
        }

        if (lineStripEntityManager.Unregister(entry.RenderingId).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to unregister line strip");
        }

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        if (glProvider.Value is not { } gl)
        {
            return new ResultProblem("GL provider is null");
        }

        if (_shaderId is null)
        {
            return new ResultProblem("Shader ID is null");
        }

        if (shaderEntityManager.GetRegistration(_shaderId.Value).TryPickProblems(out var problems, out var shaderRegistration))
        {
            return problems.Prepend("Failed to get shader registration");
        }

        cameraSceneService.ApplyCameraPositionParameters(_shader);
        _shader.World = Matrix4X4<float>.Identity;

        switch (_shader.BlendState.Blend)
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        gl.DepthMask(_shader.BlendState.DepthWrite);

        var parameters = _shader.MakeParameters();

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader into OpenGL");
        }

        foreach (var trackEntry in _trackInstanceIds.Values)
        {
            if (trackEntry.ShaderParameters is { } entityParams)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, entityParams.ToRenderingParameters())
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to apply entity shader parameters");
                }
            }

            if (DrawTrack(gl, trackEntry.RenderingId).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to draw track");
            }

            if (trackEntry.ShaderParameters is not null)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, parameters)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to restore shader-level parameters");
                }
            }
        }

        // Restore default blend state
        gl.DepthMask(true);
        gl.Disable(GLEnum.Blend);

        return Result.Success();
    }

    private Result DrawTrack(GL gl, RenderingId<LineStripData> trackRenderingId)
    {
        if (!lineStripEntityManager.TryGetRegistration(trackRenderingId, out var lineStripRegistration))
        {
            return new ResultProblem("Failed to get track registration");
        }

        var vao = lineStripRegistration.VAO.Handle;
        var vbo = lineStripRegistration.VBO.Handle;

        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        gl.Disable(GLEnum.DepthTest);
        gl.DrawArrays(PrimitiveType.LineStrip, 0, lineStripRegistration.VBO.VertexCount);
        gl.Enable(GLEnum.DepthTest);


        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return Result.Success();
    }
}
