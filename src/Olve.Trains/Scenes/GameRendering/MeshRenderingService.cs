using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.GameRendering;

public class MeshRenderingService(
    CameraSceneService cameraSceneService,
    SceneLightService sceneLightService,
    ShadowMapService shadowMapService,
    RenderingServiceHelper renderingServiceHelper,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    MeshManager meshManager,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    TextureManager textureManager,
    TerrainRenderingService terrainRenderingService,
    SharedRenderingService sharedRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService, shadowMapService]);

    public readonly record struct MeshGroupHandle(
        GroupId<Shaders.Default.Instance> GroupId,
        ShadowMapService.ShadowGroupHandle<Shaders.Default.Instance> ShadowGroupId);

    public readonly record struct MeshInstanceHandle(
        GroupId<Shaders.Default.Instance> GroupId,
        Id<Shaders.Default.Instance> InstanceId,
        ShadowMapService.ShadowGroupHandle<Shaders.Default.Instance> ShadowGroupId,
        Id<Shaders.Default.Instance> ShadowInstanceId);

    private readonly Shaders.Default _shader = new()
    {
        UColor = new Vector3D<float>(1f, 1f, 1f),
        UOpacity = 1.0f,
        UColorMix = 0f,
    };

    private readonly HashSet<UntypedTextureId> _registeredTextures = [];
    private TextureId<RGBA> _whitePixel = null!;

    public Result Load()
    {
        _whitePixel = textureManager.RegisterTexture(TextureData<RGBA>.Single(RGBA.White));

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(_whitePixel, new TextureUploadOptions())
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to register white pixel texture with OpenGL");
        }

        _registeredTextures.Add(_whitePixel);
        _shader.TextureSampler = _whitePixel;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load mesh rendering shader");
        }

        return Result.Success();
    }

    public Result Unload()
    {
        textureEntityManager.Unregister(_whitePixel);
        return Result.Success();
    }

    public Result<MeshGroupHandle> RegisterMeshGroup(
        Id<Mesh> meshId,
        AssetPath<TextureData<RGBA>>? texture = null,
        RenderState? renderState = null,
        Shaders.Default.EntityParameters? parameters = null)
    {
        if (!meshManager.TryGetMeshData(meshId, out var meshData))
        {
            return new ResultProblem("Mesh not found: '{0}'", meshId);
        }

        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        meshData.Populate(vertices);

        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        return RegisterMeshGroup(vertices, indices, texture, renderState, parameters);
    }

    public Result<MeshGroupHandle> RegisterMeshGroup(
        ReadOnlySpan<Shaders.Default.Vertex> vertices,
        ReadOnlySpan<uint> indices,
        AssetPath<TextureData<RGBA>>? texture = null,
        RenderState? renderState = null,
        Shaders.Default.EntityParameters? parameters = null)
    {
        if (geometryManager.Register(vertices, indices)
            .TryPickProblems(out var problems, out var geometryId))
        {
            return problems.Prepend("Failed to register mesh geometry");
        }

        var groupParameters = parameters;

        if (texture is { } texturePath)
        {
            if (LoadAndRegisterTexture(texturePath).TryPickProblems(out problems, out var textureId))
            {
                return problems.Prepend("Failed to load texture '{0}'", texturePath.Name);
            }

            groupParameters = (groupParameters ?? new Shaders.Default.EntityParameters()) with
            {
                TextureSampler = textureId,
            };
        }

        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDefaultFrameFormat>(
                geometryId, _shader, sharedRenderingService.MainPass, renderState ?? RenderState.Opaque, groupParameters: groupParameters)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register mesh rendering group");
        }

        // Register shadow group using the same geometry
        if (shadowMapService.RegisterDefaultShadowGroup(geometryId)
            .TryPickProblems(out problems, out var shadowGroup))
        {
            return problems.Prepend("Failed to register mesh shadow group");
        }

        return new MeshGroupHandle(groupId, shadowGroup);
    }

    public Result DeregisterMeshGroup(MeshGroupHandle group)
    {
        return renderingGroupManager.Deregister(group.GroupId);
    }

    public Result UpdateGroupParameters(MeshGroupHandle group, Shaders.Default.EntityParameters parameters)
    {
        return renderingGroupManager.SetGroupParameters(group.GroupId, parameters);
    }

    public Result<MeshInstanceHandle> AddInstance(MeshGroupHandle group, Matrix4X4<float> worldMatrix)
    {
        var instance = new Shaders.Default.Instance(worldMatrix);

        if (renderingInstanceManager.Add(group.GroupId, instance)
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add mesh instance");
        }

        if (shadowMapService.AddInstance(group.ShadowGroupId, instance)
            .TryPickProblems(out problems, out var shadowInstanceId))
        {
            return problems.Prepend("Failed to add mesh shadow instance");
        }

        return new MeshInstanceHandle(group.GroupId, instanceId, group.ShadowGroupId, shadowInstanceId);
    }

    public Result UpdateInstance(MeshInstanceHandle instance, Matrix4X4<float> worldMatrix)
    {
        var data = new Shaders.Default.Instance(worldMatrix);

        if (renderingInstanceManager.Update(instance.GroupId, instance.InstanceId, data)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return shadowMapService.UpdateInstance(instance.ShadowGroupId, instance.ShadowInstanceId, data);
    }

    public Result RemoveInstance(MeshInstanceHandle instance)
    {
        if (renderingInstanceManager.Remove(instance.GroupId, instance.InstanceId)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return shadowMapService.RemoveInstance(instance.ShadowGroupId, instance.ShadowInstanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);
        shadowMapService.ApplyShaderParameters(_shader);

        return Result.Success();
    }

    private Result<TextureId<RGBA>> LoadAndRegisterTexture(AssetPath<TextureData<RGBA>> texturePath)
    {
        if (textureLoadingManager.LoadTexture(texturePath)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems;
        }

        if (_registeredTextures.Add(textureId))
        {
            if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(textureId, new TextureUploadOptions())
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to register texture with OpenGL");
            }
        }

        return textureId;
    }
}
