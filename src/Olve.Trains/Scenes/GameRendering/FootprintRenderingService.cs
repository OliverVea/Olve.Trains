using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class FootprintRenderingService(
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    TextureManager textureManager,
    TextureEntityManager textureEntityManager,
    MeshRenderingService meshRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    public readonly record struct FootprintGroupHandle(GroupId<Shaders.WorldRectangle.Instance> GroupId);

    public readonly record struct FootprintInstanceHandle(
        GroupId<Shaders.WorldRectangle.Instance> GroupId,
        Id<Shaders.WorldRectangle.Instance> InstanceId);

    private readonly Shaders.WorldRectangle _shader = new();

    private TextureId<RGBA> _whitePixel = null!;
    private GeometryId<Shaders.WorldRectangle.Vertex> _quadGeometryId = null!;

    private static readonly Shaders.WorldRectangle.Vertex[] QuadVertices;
    private static readonly uint[] QuadIndices = [0, 1, 2, 0, 2, 3];

    static FootprintRenderingService()
    {
        var up = new Vector3D<float>(0, 1, 0);
        QuadVertices =
        [
            new(new(0, 0, 0), up, new(0, 0)),
            new(new(1, 0, 0), up, new(1, 0)),
            new(new(1, 0, 1), up, new(1, 1)),
            new(new(0, 0, 1), up, new(0, 1)),
        ];
    }

    public Result Load()
    {
        _whitePixel = textureManager.RegisterTexture(TextureData<RGBA>.Single(RGBA.White));

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(_whitePixel, new TextureUploadOptions())
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to register white pixel texture");
        }

        _shader.UTexture = _whitePixel;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load footprint shader");
        }

        if (geometryManager.Register<Shaders.WorldRectangle.Vertex>(QuadVertices, QuadIndices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register footprint quad geometry");
        }

        _quadGeometryId = geometryId;

        return Result.Success();
    }

    public Result Unload()
    {
        textureEntityManager.Unregister(_whitePixel);
        return Result.Success();
    }

    public Result<FootprintGroupHandle> RegisterGroup(
        Vector4D<float> tint,
        Vector4D<float> borderWidth,
        Vector4D<float> borderColor,
        Vector4D<float> borderRadius)
    {
        if (renderingGroupManager.Register<Shaders.WorldRectangle.Vertex, Shaders.WorldRectangle.Instance>(
                _quadGeometryId, _shader, RenderState.AlphaBlend)
            .TryPickProblems(out var problems, out var groupId))
        {
            return problems.Prepend("Failed to register footprint group");
        }

        return new FootprintGroupHandle(groupId);
    }

    public Result DeregisterGroup(FootprintGroupHandle group)
    {
        return renderingGroupManager.Deregister(group.GroupId);
    }

    public Result<FootprintInstanceHandle> AddInstance(
        FootprintGroupHandle group,
        Matrix4X4<float> worldMatrix,
        Vector2D<float> size,
        Vector4D<float> tint,
        Vector4D<float> borderWidth,
        Vector4D<float> borderColor,
        Vector4D<float> borderRadius)
    {
        var instance = new Shaders.WorldRectangle.Instance(
            iWorld: worldMatrix,
            iSize: size,
            iTint: tint,
            iBorderWidth: borderWidth,
            iBorderColor: borderColor,
            iBorderRadius: borderRadius);

        if (renderingInstanceManager.Add(group.GroupId, instance)
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add footprint instance");
        }

        return new FootprintInstanceHandle(group.GroupId, instanceId);
    }

    public Result UpdateInstance(
        FootprintInstanceHandle handle,
        Matrix4X4<float> worldMatrix,
        Vector2D<float> size,
        Vector4D<float> tint,
        Vector4D<float> borderWidth,
        Vector4D<float> borderColor,
        Vector4D<float> borderRadius)
    {
        var instance = new Shaders.WorldRectangle.Instance(
            iWorld: worldMatrix,
            iSize: size,
            iTint: tint,
            iBorderWidth: borderWidth,
            iBorderColor: borderColor,
            iBorderRadius: borderRadius);

        return renderingInstanceManager.Update(handle.GroupId, handle.InstanceId, instance);
    }

    public Result RemoveInstance(FootprintInstanceHandle handle)
    {
        return renderingInstanceManager.Remove(handle.GroupId, handle.InstanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        return Result.Success();
    }
}
