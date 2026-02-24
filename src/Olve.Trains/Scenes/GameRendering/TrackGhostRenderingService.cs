using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackGhostRenderingService(
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    TerrainRenderingService terrainRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private readonly record struct GhostEntry(
        OpenGLInstancedBufferManager.MeshInstancedRegistration Registration,
        uint VertexCount,
        Shaders.LineStrip.EntityParameters? GroupParameters);

    private readonly Dictionary<Id<Track>, GhostEntry> _entries = new();
    private readonly Shaders.LineStrip _shader = new()
    {
        UOpacity = 1.0f,
        UColorMix = 0.0f,
        BlendState = RenderState.AlphaBlendNoDepth,
    };

    private static readonly Shaders.LineStrip.Instance IdentityInstance = new(Matrix4X4<float>.Identity);

    public Result Load()
    {
        return renderingServiceHelper.LoadShader(_shader);
    }

    public Result Register(
        Id<Track> trackId,
        LineStripData data,
        Shaders.LineStrip.EntityParameters? groupParameters = null)
    {
        if (data.Validate().TryPickProblems(out var problems))
        {
            return problems.Prepend("Invalid line strip data");
        }

        var vertexFloats = MarshalVertexFloats(data);
        var instanceFloats = MarshalInstanceFloats();

        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                (uint)data.VertexCount,
                ReadOnlySpan<uint>.Empty,
                Shaders.LineStrip.Vertex.ConfigureAttributes,
                instanceFloats,
                1,
                Shaders.LineStrip.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create ghost instanced buffers");
        }

        _entries[trackId] = new GhostEntry(registration, (uint)data.VertexCount, groupParameters);

        return Result.Success();
    }

    public Result Update(
        Id<Track> trackId,
        LineStripData? data = null,
        Shaders.LineStrip.EntityParameters? groupParameters = null)
    {
        if (!_entries.TryGetValue(trackId, out var entry))
        {
            return new ResultProblem("Ghost track '{0}' is not registered", trackId);
        }

        if (data != null)
        {
            if (data.Validate().TryPickProblems(out var problems))
            {
                return problems.Prepend("Invalid line strip data");
            }

            var vertexFloats = MarshalVertexFloats(data);
            instancedBufferManager.UpdateMeshVertexBuffer(
                entry.Registration,
                vertexFloats,
                (uint)data.VertexCount,
                BufferUsageARB.DynamicDraw);

            entry = entry with { VertexCount = (uint)data.VertexCount };
        }

        if (groupParameters is not null)
        {
            entry = entry with { GroupParameters = groupParameters };
        }

        _entries[trackId] = entry;

        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_entries.Remove(trackId, out var entry))
        {
            return Result.Success();
        }

        instancedBufferManager.DeleteMeshInstanceBuffers(entry.Registration);

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        foreach (var entry in _entries.Values)
        {
            if (renderingManager3D.RenderInstanced(
                    _shader, entry.Registration, 1, PrimitiveType.LineStrip,
                    entry.VertexCount, entry.GroupParameters)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var entry in _entries.Values)
        {
            instancedBufferManager.DeleteMeshInstanceBuffers(entry.Registration);
        }
        _entries.Clear();

        return renderingServiceHelper.UnloadShader(_shader);
    }

    private static float[] MarshalVertexFloats(LineStripData data)
    {
        var floats = new float[data.VertexCount * Shaders.LineStrip.Vertex.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        for (var i = 0; i < data.VertexCount; i++)
        {
            var vertex = new Shaders.LineStrip.Vertex(data.Positions[i], data.Colors[i]);
            vertex.WriteTo(span.Slice(offset, Shaders.LineStrip.Vertex.FloatCount));
            offset += Shaders.LineStrip.Vertex.FloatCount;
        }
        return floats;
    }

    private static float[] MarshalInstanceFloats()
    {
        var floats = new float[Shaders.LineStrip.Instance.FloatCount];
        IdentityInstance.WriteTo(floats);
        return floats;
    }
}
