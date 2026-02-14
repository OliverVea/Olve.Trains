using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackRenderingService(
    RenderingManager3D renderingManager3D,
    CameraSceneService cameraSceneService,
    ShaderEntityManager shaderEntityManager,
    TerrainRenderingService terrainRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private readonly record struct TrackEntry(
        GeometryId GeometryId,
        RenderingInstanceId InstanceId);

    private readonly Dictionary<Id<Track>, TrackEntry> _trackEntries = new();
    private readonly Shaders.LineStrip _shader = new()
    {
        UOpacity = 1.0f,
        UColorMix = 0.0f,
        BlendState = RenderState.AlphaBlendNoDepth,
    };

    public Result Load()
    {
        if (shaderEntityManager.Register(_shader.ShaderData).TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        _shader.RenderingId = shaderId;

        return Result.Success();
    }

    public Result Register(
        Id<Track> trackId,
        LineStripData data,
        Shaders.LineStrip.EntityParameters? shaderParameters = null)
    {
        if (data.Validate().TryPickProblems(out var problems))
        {
            return problems.Prepend("Invalid line strip data");
        }

        var vertices = MarshalVertices(data);

        if (renderingManager3D.RegisterGeometry(
                vertices, PrimitiveType.LineStrip, BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register line strip geometry");
        }

        if (renderingManager3D.RegisterInstance(geometryId, _shader.RenderingId, Matrix4X4<float>.Identity)
            .TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register line strip instance");
        }

        if (shaderParameters is { } entityParams)
        {
            renderingManager3D.SetInstanceParameters(instanceId, entityParams);
        }

        _trackEntries[trackId] = new TrackEntry(geometryId, instanceId);
        return Result.Success();
    }

    public Result Update(
        Id<Track> trackId,
        LineStripData? data = null,
        Shaders.LineStrip.EntityParameters? shaderParameters = null)
    {
        if (!_trackEntries.TryGetValue(trackId, out var entry))
        {
            return new ResultProblem("Track '{0}' is not registered", trackId);
        }

        if (data != null)
        {
            if (data.Validate().TryPickProblems(out var problems))
            {
                return problems.Prepend("Invalid line strip data");
            }

            var vertices = MarshalVertices(data);
            renderingManager3D.UpdateGeometry(entry.GeometryId, vertices);
        }

        renderingManager3D.SetInstanceParameters(entry.InstanceId, shaderParameters);
        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_trackEntries.Remove(trackId, out var entry))
        {
            return Result.Success();
        }

        renderingManager3D.DeregisterInstance(entry.InstanceId);
        renderingManager3D.DeregisterGeometry(entry.GeometryId);

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        _shader.World = Matrix4X4<float>.Identity;

        return renderingManager3D.Render(_shader);
    }

    private static Shaders.LineStrip.Vertex[] MarshalVertices(LineStripData data)
    {
        var vertices = new Shaders.LineStrip.Vertex[data.VertexCount];
        for (var i = 0; i < data.VertexCount; i++)
        {
            vertices[i] = new Shaders.LineStrip.Vertex(data.Positions[i], data.Colors[i]);
        }
        return vertices;
    }
}
