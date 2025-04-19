using Olve.CodeGen;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Game;

public class TrackPlacingService(TerrainRaycastService terrainRaycastService, MouseManager mouseManager, KeyboardManager keyboardManager, ILoggingManager loggingManager, TrackService trackService) : SceneService
{
    public TrackPoint? PreviousPoint { get; private set; }
    public TrackPoint? CurrentPoint { get; private set; }
    
    public Direction Direction { get; private set; } = Direction.North;

    public override int Priority => GetPriorityFromDependencies([terrainRaycastService]);

    public override Result<Pass> Input(TimeSpan deltaTime)
    {
        if (mouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            if (PreviousPoint is null)
            {
                PreviousPoint = CurrentPoint;
            }
            else
            {
                if (CurrentPoint is null)
                {
                    return new ResultProblem("Current point is null");
                }
                
                if (trackService.AddTrack(PreviousPoint.Value, CurrentPoint.Value).TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to add track");
                }
                
                loggingManager.Log(LogLevel.Info, $"Placed track from {PreviousPoint.Value} to {CurrentPoint.Value}");
                
                PreviousPoint = null;
            }
        }
        
        if (keyboardManager.State.IsKeyPressed(Key.R)) 
        {
            Direction = Direction switch
            {
                Direction.North => Direction.East,
                Direction.East => Direction.South,
                Direction.South => Direction.West,
                Direction.West => Direction.North,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        return Pass.Pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        CurrentPoint = terrainRaycastService.TerrainIntersectionTileCenter is { } tileCenter
            ? new TrackPoint(tileCenter, Direction)
            : null;
        
        return Result.Success();
    }
}

public readonly record struct TrackPoint(Vector3D<float> Point, Direction Direction);

public class TrackService
{
    private readonly List<Hermite3> _tracks = [];
    
    public IReadOnlyList<Hermite3> Tracks => _tracks;
    
    public Action<Hermite3>? OnTrackAdded { get; set; }
    public Action<Hermite3>? OnTrackRemoved { get; set; }
    
    public Result AddTrack(TrackPoint start, TrackPoint end)
    {
        var distance = Vector3D.Distance(start.Point, end.Point);
        
        var startTangent = start.Direction.ToVector3D() * distance;
        var endTangent = end.Direction.ToVector3D() * distance;
        
        var track = new Hermite3([
                new(0, new(start.Point, startTangent, startTangent)),
                new(1, new(end.Point, endTangent, endTangent))
        ]);
        
        _tracks.Add(track);
        
        OnTrackAdded?.Invoke(track);
        
        return Result.Success();
    }
}

public class TrackRenderingService(TrackService trackService, Provider<GL> glProvider, LineStripEntityManager lineStripEntityManager, CameraSceneService cameraSceneService, ShaderEntityManager shaderEntityManager, OpenGLModelRenderingManager openGLModelRenderingManager) : SceneService
{
    private const int TrackVertexCount = 100;
    
    private readonly Dictionary<Hermite3, RenderingId<LineStripData>> _trackInstanceIds = new();
    private readonly Queue<Hermite3> _tracksToLoad = new();
    private readonly Shaders.LineStrip _shader = new();
    
    private RenderingId<ShaderData>? _shaderId;

    public override Result Load()
    {
        if (shaderEntityManager.Register(_shader.ShaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }
        
        _shaderId = shaderId;
        
        trackService.OnTrackAdded += OnTrackAdded;

        return Result.Success();
    }
    
    private void OnTrackAdded(Hermite3 track) => 
        _tracksToLoad.Enqueue(track);

    public override Result Update(TimeSpan deltaTime)
    {
        while (_tracksToLoad.TryDequeue(out var track))
        {
            var lineStripData = GetLineStripData(track);
            
            if (lineStripEntityManager.Register(lineStripData).TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to register track");
            }

            _trackInstanceIds[track] = instanceId;
        }
        
        return Result.Success();
    }
    
    private LineStripData GetLineStripData(Hermite3 track)
    {
        var positions = new Vector3D<float>[TrackVertexCount];
        var colors = new Vector3D<float>[TrackVertexCount];
        
        Array.Fill(colors, Vector3D<float>.One);
        
        for (var i = 0; i < TrackVertexCount; i++){
            var t = (float)i / TrackVertexCount;
            positions[i] = track.Sample(t);
        }
        
        return new LineStripData
        {
            Positions = positions,
            Colors = colors,
        };
    }

    public override Result Render(TimeSpan deltaTime)
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
        
        _shader.World = Matrix4X4<float>.Identity;
        _shader.View = cameraSceneService.ViewMatrix;
        _shader.Projection = cameraSceneService.ProjectionMatrix;

        var parameters = _shader.MakeParameters();
        
        if (openGLModelRenderingManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
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
        if (lineStripEntityManager.GetRegistration(trackRenderingId).TryPickProblems(out var problems, out var lineStripRegistration))
        {
            return problems.Prepend("Failed to get track registration");
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