using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Light;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    ILogger<BuildingRenderingService> logger,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
    RenderingServiceHelper renderingServiceHelper,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    GridService gridService,
    SceneLightService sceneLightService,
    TerrainRenderingService terrainRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    // Main buildings
    private readonly Shaders.Building _shader = new()
    {
        BlendState = RenderState.Opaque,
        UColor = new Vector3D<float>(0.7f, 0.7f, 0.7f),
        UOpacity = 1.0f,
        UColorOverride = new Vector3D<float>(0, 0, 0),
        UColorMix = 0.0f,
    };

    private readonly Dictionary<Id<Building>, Shaders.Building.Instance> _instances = new();
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;
    private bool _dirty;

    // Ghost buildings (placement previews)
    private readonly Shaders.Building _ghostShader = new()
    {
        BlendState = RenderState.AlphaBlend,
        UColor = new Vector3D<float>(0.7f, 0.7f, 0.7f),
        UOpacity = 0.5f,
        UColorOverride = new Vector3D<float>(0, 0, 0),
        UColorMix = 0.0f,
    };

    private readonly Dictionary<Id<Building>, Shaders.Building.Instance> _ghostInstances = new();
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _ghostRegistration;
    private bool _ghostDirty;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load building shader");
        }

        if (renderingServiceHelper.LoadShader(_ghostShader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load ghost building shader");
        }

        // Build unit cube vertex data
        var vertices = new Shaders.Building.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(vertices);

        var vertexFloats = new float[UnitCube.VertexCount * Shaders.Building.Vertex.FloatCount];
        var span = vertexFloats.AsSpan();
        var offset = 0;
        for (var i = 0; i < UnitCube.VertexCount; i++)
        {
            vertices[i].WriteTo(span.Slice(offset, Shaders.Building.Vertex.FloatCount));
            offset += Shaders.Building.Vertex.FloatCount;
        }

        Span<uint> indices = stackalloc uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);
        var indexArray = indices.ToArray();

        // Create main building instance buffer
        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                UnitCube.VertexCount,
                indexArray,
                Shaders.Building.Vertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Building.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create building instanced buffers");
        }

        _registration = registration;

        // Create ghost building instance buffer
        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                UnitCube.VertexCount,
                indexArray,
                Shaders.Building.Vertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Building.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var ghostRegistration))
        {
            return problems.Prepend("Failed to create ghost building instanced buffers");
        }

        _ghostRegistration = ghostRegistration;

        return Result.Success();
    }

    public Result Unload()
    {
        instancedBufferManager.DeleteMeshInstanceBuffers(_registration);
        instancedBufferManager.DeleteMeshInstanceBuffers(_ghostRegistration);

        var result = Result.Concat(
            renderingServiceHelper.UnloadShader(_shader),
            renderingServiceHelper.UnloadShader(_ghostShader));

        if (result.TryPickProblems(out var problems))
        {
            logger.Log(problems.Prepend("Failed to unload building rendering resources"));
        }

        return Result.Success();
    }

    public Result Register(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        var worldMatrix = ComputeWorldMatrix(blueprint.Footprint, building.Position);
        _instances[buildingId] = new Shaders.Building.Instance(worldMatrix);
        _dirty = true;

        return Result.Success();
    }

    public Result RegisterGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        var worldMatrix = ComputeWorldMatrix(footprint, position);
        _ghostInstances[ghostId] = new Shaders.Building.Instance(worldMatrix);
        _ghostDirty = true;

        return Result.Success();
    }

    public Result UpdateGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        if (!_ghostInstances.ContainsKey(ghostId))
        {
            return new ResultProblem("Could not find ghost instance for building '{0}'", ghostId);
        }

        var worldMatrix = ComputeWorldMatrix(footprint, position);
        _ghostInstances[ghostId] = new Shaders.Building.Instance(worldMatrix);
        _ghostDirty = true;

        return Result.Success();
    }

    public void SetGhostAppearance(float opacity, Vector3D<float>? colorOverride = null, float colorMix = 0f)
    {
        _ghostShader.UOpacity = opacity;
        _ghostShader.UColorOverride = colorOverride ?? new Vector3D<float>(0, 0, 0);
        _ghostShader.UColorMix = colorMix;
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (_instances.Remove(buildingId))
        {
            _dirty = true;
            return Result.Success();
        }

        if (_ghostInstances.Remove(buildingId))
        {
            _ghostDirty = true;
            return Result.Success();
        }

        return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        cameraSceneService.ApplyCameraPositionParameters(_ghostShader);
        sceneLightService.ApplyShaderParameters(_ghostShader);

        if (_dirty)
        {
            RebuildInstanceBuffer(_instances, _registration);
            _dirty = false;
        }

        if (_ghostDirty)
        {
            RebuildInstanceBuffer(_ghostInstances, _ghostRegistration);
            _ghostDirty = false;
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        if (_instances.Count > 0)
        {
            if (renderingManager3D.RenderInstanced(_shader, _registration, (uint)_instances.Count)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        if (_ghostInstances.Count > 0)
        {
            if (renderingManager3D.RenderInstanced(_ghostShader, _ghostRegistration, (uint)_ghostInstances.Count)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    private Matrix4X4<float> ComputeWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        const float inset = 0f;
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var sw = w - inset * 2;
        var sd = d - inset * 2;
        var sh = h - inset;

        var y = gridService.ToTileOrigin(position.BottomLeft).Y;

        var rotation = position.CardinalDirection.ToYRotation();

        return Matrix4X4.CreateScale(sw, sh, sd)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(
                   position.BottomLeft.X + inset,
                   y,
                   position.BottomLeft.Z + inset)
            ;
    }

    private void RebuildInstanceBuffer(
        Dictionary<Id<Building>, Shaders.Building.Instance> instances,
        OpenGLInstancedBufferManager.MeshInstancedRegistration registration)
    {
        var instanceCount = instances.Count;
        if (instanceCount == 0)
        {
            instancedBufferManager.UpdateMeshInstanceBuffer(
                registration,
                ReadOnlySpan<float>.Empty,
                0,
                BufferUsageARB.DynamicDraw);
            return;
        }

        var floats = new float[instanceCount * Shaders.Building.Instance.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var instance in instances.Values)
        {
            instance.WriteTo(span.Slice(offset, Shaders.Building.Instance.FloatCount));
            offset += Shaders.Building.Instance.FloatCount;
        }

        instancedBufferManager.UpdateMeshInstanceBuffer(
            registration,
            floats,
            (uint)instanceCount,
            BufferUsageARB.DynamicDraw);
    }
}
