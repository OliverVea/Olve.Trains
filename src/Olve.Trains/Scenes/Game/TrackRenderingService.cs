using Olve.CodeGen;
using Olve.Engine3D;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Utilities.Ids;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Game;

public class TrackRenderingService(TrackService trackService, Provider<GL> glProvider, LineStripEntityManager lineStripEntityManager, CameraSceneService cameraSceneService, ShaderEntityManager shaderEntityManager, OpenGLModelRenderingManager openGLModelRenderingManager, TrackSplineService trackSplineService) : SceneService
{
    private const int TrackVertexCount = 100;
    
    private readonly Dictionary<Id<Track>, RenderingId<LineStripData>> _trackInstanceIds = new();
    private readonly Queue<Id<Track>> _tracksToLoad = new();
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
    
    private void OnTrackAdded(Track track)
    {
        _tracksToLoad.Enqueue(track.Id);
    }

    public override Result Update(TimeSpan deltaTime)
    {
        while (_tracksToLoad.TryDequeue(out var trackId))
        {
            if (GetLineStripData(trackId).TryPickProblems(out var problems, out var lineStripData))
            {
                return problems.Prepend("Failed to get line strip data");
            }
            
            if (lineStripEntityManager.Register(lineStripData).TryPickProblems(out problems, out var instanceId))
            {
                return problems.Prepend("Failed to register track");
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
        
        Array.Fill(colors, Vector3D<float>.One);
        
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