using Olve.Generated;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Tracks;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Rendering;

public class TrackRenderingService(ILoggingManager loggingManager,
    TrackService trackService,
    Provider<GL> glProvider,
    LineStripEntityManager lineStripEntityManager,
    CameraSceneService cameraSceneService,
    ShaderEntityManager shaderEntityManager,
    OpenGLShaderManager openGLShaderManager,
    StationPlatformService stationPlatformService,
    TrackSplineService trackSplineService) : SceneService(loggingManager)
{
    private const int TrackVertexCount = 100;

    private readonly record struct TrackEntry(
        RenderingId<LineStripData> RenderingId,
        Shaders.LineStrip.EntityParameters? ShaderParameters);

    private readonly Dictionary<Id<Track>, TrackEntry> _trackInstanceIds = new();
    private readonly Queue<Id<Track>> _tracksToLoad = new();
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

        trackService.OnAdded.Subscribe(OnTrackAdded);
        stationPlatformService.OnAdded.Subscribe(OnPlatformAdded);

        return Result.Success();
    }

    protected override Result OnUnload()
    {
        trackService.OnAdded.Unsubscribe(OnTrackAdded);
        stationPlatformService.OnAdded.Unsubscribe(OnPlatformAdded);

        return Result.Success();
    }

    private void OnTrackAdded(Id<Track> trackId)
    {
        _tracksToLoad.Enqueue(trackId);
    }

    private void OnPlatformAdded(Id<StationPlatform> platformId)
    {
        if (!stationPlatformService.TryGet(platformId, out var platform))
        {
            return;
        }

        _tracksToLoad.Enqueue(platform.TrackId);
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

        if (data is { } newData)
        {
            if (lineStripEntityManager.Update(entry.RenderingId, newData).TryPickProblems(out var problems))
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
            return new ResultProblem("Track '{0}' is not registered", trackId);
        }

        if (lineStripEntityManager.Unregister(entry.RenderingId).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to unregister line strip");
        }

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        while (_tracksToLoad.TryDequeue(out var trackId))
        {
            if (_trackInstanceIds.Remove(trackId, out var existingEntry))
            {
                if (lineStripEntityManager.Unregister(existingEntry.RenderingId).TryPickProblems(out var unregisterProblems))
                {
                    return unregisterProblems.Prepend("Failed to unregister track");
                }
            }

            if (Result.Chain(
                    () => GetLineStripData(trackId),
                    lineStripEntityManager.Register
                ).TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to load track");
            }

            _trackInstanceIds[trackId] = new TrackEntry(instanceId, null);
        }

        return Result.Success();
    }

    private Result<LineStripData> GetLineStripData(Id<Track> trackId)
    {
        if (trackSplineService.GetPoints(trackId, TrackVertexCount).TryPickProblems(out var problems, out var positions))
        {
            return problems.Prepend("Failed to get track points");
        }

        if (positions.Length != TrackVertexCount)
        {
            return new ResultProblem("Track length must be equal to vertex count");
        }

        var colors = new Vector3D<float>[TrackVertexCount];
        var color = stationPlatformService.TryGetPlatform(trackId, out _)
            ? new Vector3D<float>(1, 0, 0)
            : new Vector3D<float>(1, 1, 1);

        Array.Fill(colors, color);

        return new LineStripData
        {
            Positions = positions,
            Colors = colors,
        };
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

        // Apply blend state
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

        gl.DrawArrays(PrimitiveType.LineStrip, 0, TrackVertexCount);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return Result.Success();
    }
}