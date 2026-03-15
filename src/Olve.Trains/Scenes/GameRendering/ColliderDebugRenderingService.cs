using Olve.Engine3D.Input;
using Olve.Engine3D.Math;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Shared.Rendering;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class ColliderDebugRenderingService(
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    EventQueueFactory eventQueueFactory,
    CollisionSystem collisionSystem,
    ColliderDebugSettings settings,
    KeyboardManager keyboardManager,
    SharedRenderingService sharedRenderingService)
    : ISceneService
{
    public int Priority => 5000;

    private readonly record struct ColliderDebugEntry(
        Id<Shaders.LineStrip.Instance> AabbInstanceId,
        Id<Shaders.LineStrip.Instance> ObbInstanceId);

    // Tracked collider IDs (box colliders only) — always maintained regardless of IsEnabled
    private readonly HashSet<Id<Collider>> _trackedColliders = new();

    // Rendering instances — only populated when enabled
    private readonly Dictionary<Id<Collider>, ColliderDebugEntry> _entries = new();

    private readonly EventQueue<Id<Collider>> _registerQueue =
        eventQueueFactory.Create(collisionSystem.OnColliderRegistered);

    private readonly EventQueue<Id<Collider>> _unregisterQueue =
        eventQueueFactory.Create(collisionSystem.OnColliderUnregistered);

    private readonly EventQueue<Id<Collider>> _transformQueue =
        eventQueueFactory.Create(collisionSystem.OnColliderTransformUpdated);

    private readonly Shaders.LineStrip _shader = new()
    {
        UOpacity = 1.0f,
        UColorMix = 0.0f,
        BlendState = RenderState.AlphaBlendNoDepth,
    };

    private GeometryId<Shaders.LineStrip.Vertex> _cubeGeometryId = null!;
    private GroupId<Shaders.LineStrip.Instance> _whiteGroupId = null!;
    private GroupId<Shaders.LineStrip.Instance> _greenGroupId = null!;
    private bool _wasEnabled;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load collider debug shader");
        }

        var vertices = BuildWireframeCubeVertices();

        if (geometryManager.Register<Shaders.LineStrip.Vertex>(
                vertices, ReadOnlySpan<uint>.Empty, PrimitiveType.Lines)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register wireframe cube geometry");
        }

        _cubeGeometryId = geometryId;

        // White group for AABBs
        if (renderingGroupManager.Register<Shaders.LineStrip.Vertex, Shaders.LineStrip.Instance, IDefaultFrameFormat>(
                _cubeGeometryId, _shader, sharedRenderingService.MainPass, RenderState.AlphaBlendNoDepth,
                PrimitiveType.Lines,
                groupParameters: new Shaders.LineStrip.EntityParameters(
                    UColorOverride: new Vector3D<float>(1f, 1f, 1f),
                    UColorMix: 1f,
                    UOpacity: 0.5f))
            .TryPickProblems(out problems, out var whiteGroup))
        {
            return problems.Prepend("Failed to register AABB debug group");
        }

        _whiteGroupId = whiteGroup;

        // Green group for OBBs
        if (renderingGroupManager.Register<Shaders.LineStrip.Vertex, Shaders.LineStrip.Instance, IDefaultFrameFormat>(
                _cubeGeometryId, _shader, sharedRenderingService.MainPass, RenderState.AlphaBlendNoDepth,
                PrimitiveType.Lines,
                groupParameters: new Shaders.LineStrip.EntityParameters(
                    UColorOverride: new Vector3D<float>(0f, 1f, 0f),
                    UColorMix: 1f,
                    UOpacity: 0.8f))
            .TryPickProblems(out problems, out var greenGroup))
        {
            return problems.Prepend("Failed to register OBB debug group");
        }

        _greenGroupId = greenGroup;

        _registerQueue.SetHandler(TrackCollider).Init(collisionSystem.ColliderIds);
        _unregisterQueue.SetHandler(UntrackCollider).Init();
        _transformQueue.SetHandler(UpdateCollider).Init();

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        if (keyboardManager.State.IsKeyPressed(Key.F3))
        {
            settings.IsEnabled = !settings.IsEnabled;
        }

        return Pass.Pass;
    }

    public Result Update()
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        _registerQueue.Update();
        _unregisterQueue.Update();
        _transformQueue.Update();

        var isEnabled = settings.IsEnabled;

        if (isEnabled && !_wasEnabled)
        {
            EnableAll();
        }
        else if (!isEnabled && _wasEnabled)
        {
            DisableAll();
        }

        _wasEnabled = isEnabled;

        return Result.Success();
    }

    public Result Unload()
    {
        _registerQueue.Cleanup();
        _unregisterQueue.Cleanup();
        _transformQueue.Cleanup();

        DisableAll();
        _trackedColliders.Clear();

        renderingGroupManager.Deregister(_whiteGroupId);
        renderingGroupManager.Deregister(_greenGroupId);
        geometryManager.Deregister(_cubeGeometryId);

        return Result.Success();
    }

    private Result TrackCollider(Id<Collider> colliderId)
    {
        if (!collisionSystem.TryGetColliderInfo(colliderId, out var shape, out _, out _))
        {
            return Result.Success();
        }

        if (shape is not BoxColliderShape)
        {
            return Result.Success();
        }

        _trackedColliders.Add(colliderId);

        if (settings.IsEnabled)
        {
            return AddRenderingInstances(colliderId);
        }

        return Result.Success();
    }

    private Result UntrackCollider(Id<Collider> colliderId)
    {
        _trackedColliders.Remove(colliderId);
        return RemoveRenderingInstances(colliderId);
    }

    private Result UpdateCollider(Id<Collider> colliderId)
    {
        if (!_entries.TryGetValue(colliderId, out var entry))
        {
            return Result.Success();
        }

        if (!collisionSystem.TryGetColliderInfo(colliderId, out var shape, out var worldMatrix, out var worldAABB))
        {
            return new ResultProblem("Collider '{0}' not found for transform update", colliderId);
        }

        if (shape is not BoxColliderShape box)
        {
            return Result.Success();
        }

        var aabbMatrix = ComputeAabbMatrix(worldAABB);
        var obbMatrix = ComputeObbMatrix(box, worldMatrix);

        return Result.Concat(
            renderingInstanceManager.Update(
                _whiteGroupId, entry.AabbInstanceId, new Shaders.LineStrip.Instance(aabbMatrix)),
            renderingInstanceManager.Update(
                _greenGroupId, entry.ObbInstanceId, new Shaders.LineStrip.Instance(obbMatrix)));
    }

    private void EnableAll()
    {
        foreach (var colliderId in _trackedColliders)
        {
            AddRenderingInstances(colliderId);
        }
    }

    private void DisableAll()
    {
        foreach (var colliderId in _entries.Keys.ToList())
        {
            RemoveRenderingInstances(colliderId);
        }
    }

    private Result AddRenderingInstances(Id<Collider> colliderId)
    {
        if (_entries.ContainsKey(colliderId))
        {
            return Result.Success();
        }

        if (!collisionSystem.TryGetColliderInfo(colliderId, out var shape, out var worldMatrix, out var worldAABB))
        {
            return new ResultProblem("Collider '{0}' not found", colliderId);
        }

        if (shape is not BoxColliderShape box)
        {
            return Result.Success();
        }

        var aabbMatrix = ComputeAabbMatrix(worldAABB);
        var obbMatrix = ComputeObbMatrix(box, worldMatrix);

        if (renderingInstanceManager.Add(_whiteGroupId, new Shaders.LineStrip.Instance(aabbMatrix))
            .TryPickProblems(out var problems, out var aabbInstanceId))
        {
            return problems.Prepend("Failed to add AABB instance for collider '{0}'", colliderId);
        }

        if (renderingInstanceManager.Add(_greenGroupId, new Shaders.LineStrip.Instance(obbMatrix))
            .TryPickProblems(out problems, out var obbInstanceId))
        {
            return problems.Prepend("Failed to add OBB instance for collider '{0}'", colliderId);
        }

        _entries[colliderId] = new ColliderDebugEntry(aabbInstanceId, obbInstanceId);
        return Result.Success();
    }

    private Result RemoveRenderingInstances(Id<Collider> colliderId)
    {
        if (!_entries.Remove(colliderId, out var entry))
        {
            return Result.Success();
        }

        renderingInstanceManager.Remove(_whiteGroupId, entry.AabbInstanceId);
        renderingInstanceManager.Remove(_greenGroupId, entry.ObbInstanceId);

        return Result.Success();
    }

    private static Matrix4X4<float> ComputeAabbMatrix(AABB aabb)
    {
        var size = aabb.Max - aabb.Min;
        var center = (aabb.Min + aabb.Max) * 0.5f;
        return Matrix4X4.CreateScale(size) * Matrix4X4.CreateTranslation(center);
    }

    private static Matrix4X4<float> ComputeObbMatrix(BoxColliderShape box, Matrix4X4<float> worldMatrix)
    {
        var fullExtents = box.HalfExtents * 2f;
        return Matrix4X4.CreateScale(fullExtents) * worldMatrix;
    }

    private static Shaders.LineStrip.Vertex[] BuildWireframeCubeVertices()
    {
        // Unit cube edges at ±0.5, 12 edges = 24 line-endpoint vertices
        var h = 0.5f;
        Vector3D<float>[] corners =
        [
            new(-h, -h, -h), // 0
            new(+h, -h, -h), // 1
            new(+h, +h, -h), // 2
            new(-h, +h, -h), // 3
            new(-h, -h, +h), // 4
            new(+h, -h, +h), // 5
            new(+h, +h, +h), // 6
            new(-h, +h, +h), // 7
        ];

        // 12 edges as pairs of corner indices
        (int a, int b)[] edges =
        [
            // Bottom face
            (0, 1), (1, 2), (2, 3), (3, 0),
            // Top face
            (4, 5), (5, 6), (6, 7), (7, 4),
            // Vertical edges
            (0, 4), (1, 5), (2, 6), (3, 7),
        ];

        var white = new Vector3D<float>(1f, 1f, 1f);
        var vertices = new Shaders.LineStrip.Vertex[edges.Length * 2];
        for (var i = 0; i < edges.Length; i++)
        {
            var (a, b) = edges[i];
            vertices[i * 2] = new Shaders.LineStrip.Vertex(corners[a], white);
            vertices[i * 2 + 1] = new Shaders.LineStrip.Vertex(corners[b], white);
        }

        return vertices;
    }
}
