using Olve.Generated;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
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

    private readonly Dictionary<Id<Track>, RenderingId<LineStripData>> _trackInstanceIds = new();
    private readonly Queue<Id<Track>> _tracksToLoad = new();
    private readonly Shaders.LineStrip _shader = new();

    private RenderingId<ShaderData>? _shaderId;

    protected override Result OnLoad()
    {
        if (shaderEntityManager.Register(_shader.ShaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        _shaderId = shaderId;

        trackService.OnAdded.Subscribe(OnTrackAdded);

        return Result.Success();
    }

    protected override Result OnUnload()
    {
        trackService.OnAdded.Unsubscribe(OnTrackAdded);

        return Result.Success();
    }

    private void OnTrackAdded(Id<Track> trackId)
    {
        _tracksToLoad.Enqueue(trackId);
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        while (_tracksToLoad.TryDequeue(out var trackId))
        {
            if (Result.Chain(
                    () => GetLineStripData(trackId),
                    lineStripEntityManager.Register
                ).TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to load track");
            }

            _trackInstanceIds[trackId] = instanceId;
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

        var parameters = _shader.MakeParameters();

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader into OpenGL");
        }

        foreach (var trackRenderingId in _trackInstanceIds.Values)
        {
            if (DrawTrack(gl, trackRenderingId).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to draw track");
            }
        }

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